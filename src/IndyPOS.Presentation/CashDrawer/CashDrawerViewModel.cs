using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Presentation.SalesHistory;

namespace IndyPOS.Presentation.CashDrawer;

/// <summary>A ลงบัญชี (PayLater) taken on the day shown, in store time.</summary>
public sealed record PayLaterRow(DateTime StoreTime, string Description, decimal Amount);

/// <summary>
/// The ลิ้นชักเก็บเงิน screen (cash spec §8; per-store spec §2/§4.3). StoreHub computes every total;
/// this shows them, says what may be edited, and reloads the whole day after any confirmed save. A
/// newer load cancels the one in flight, so an older answer never replaces a newer day.
/// </summary>
public sealed partial class CashDrawerViewModel : ObservableObject
{
    public const string LoadFailed = "โหลดลิ้นชักเก็บเงินไม่สำเร็จ: {0}";

    private readonly IStoreHubClient _client;
    private readonly StoreClock _clock;
    private readonly CashDrawerAccess _access;
    private CancellationTokenSource _current = new();

    [ObservableProperty]
    private DateOnly _businessDate;

    [ObservableProperty]
    private CashDrawerSummaryDto? _summary;

    // The summary's IsEditable, but turned off by a failed load too, so the screen and its sections agree.
    [ObservableProperty]
    private bool _isEditable;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public CashDrawerViewModel(IStoreHubClient client, StoreClock clock, CashDrawerAccess access, StoreFeaturesDto? features)
    {
        _client = client;
        _clock = clock;
        _access = access;
        _businessDate = clock.Today;

        // Features that could not be read show everything, as the rest of the till does.
        ShowProductTypeSplit = features?.MultipleProductTypesEnabled ?? true;
        ShowPayLater = features?.PayLaterEnabled ?? true;

        Payouts = new CashEntriesViewModel(CashEntryApis.Payouts(client, clock), LoadAsync);
        Floats = new CashEntriesViewModel(CashEntryApis.Floats(client, clock), LoadAsync);
        DebtRepayments = new CashEntriesViewModel(CashEntryApis.DebtRepayments(client, clock), LoadAsync);
        Count = new CashCountViewModel(client, clock, LoadAsync);
    }

    public CashEntriesViewModel Payouts { get; }

    public CashEntriesViewModel Floats { get; }

    public CashEntriesViewModel DebtRepayments { get; }

    public CashCountViewModel Count { get; }

    /// <summary>The day's new ลงบัญชี, oldest first; empty for a store without PayLater.</summary>
    public ObservableCollection<PayLaterRow> TodaysPayLater { get; } = [];

    /// <summary>The payout category (เบ็ดเตล็ด / ฮาร์ดแวร์) only means something where hardware is sold.</summary>
    public bool ShowPayoutCategory => ShowProductTypeSplit;

    public bool ShowProductTypeSplit { get; }

    public bool ShowPayLater { get; }

    public bool CanPickDay => _access.CanViewAnyDay;

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task RefreshAsync() => LoadAsync();

    // Another day only for reports.view; StoreHub would refuse it anyway.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ShowDayAsync(DateOnly day) =>
        day != _clock.Today && !CanPickDay ? Task.CompletedTask : LoadAsync(day);

    // A cashier always loads the real today: after midnight the day the screen opened on is a day
    // StoreHub refuses them, and the screen would never recover.
    private Task LoadAsync() => LoadAsync(CanPickDay ? BusinessDate : _clock.Today);

    private async Task LoadAsync(DateOnly day)
    {
        var token = Restart();
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var loaded = await FetchDayAsync(day, token);
            token.ThrowIfCancellationRequested();
            Show(day, loaded);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A newer load replaced this one.
        }
        catch (StoreHubException ex)
        {
            // A failure that arrives after a newer load took over is that load's business, not this one's.
            if (token.IsCancellationRequested)
                return;

            // What is on screen may no longer be the day to edit, so nothing may be saved over it.
            ErrorMessage = string.Format(LoadFailed, ex.Message);
            SetEditable(false);
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsBusy = false;
        }
    }

    private sealed record LoadedDay(
        CashDrawerSummaryDto Summary, IReadOnlyList<CashEntryRow> Payouts, IReadOnlyList<CashEntryRow> Floats,
        IReadOnlyList<CashEntryRow> DebtRepayments, IReadOnlyList<CashCountRow> Counts, IReadOnlyList<PayLaterRow> PayLater);

    // All five at once; nothing is shown until every one has arrived, so a money screen never mixes two days.
    private async Task<LoadedDay> FetchDayAsync(DateOnly day, CancellationToken token)
    {
        var summary = _client.GetCashSummaryAsync(day, token);
        var payouts = Payouts.FetchAsync(day, token);
        var floats = Floats.FetchAsync(day, token);
        var repayments = ShowPayLater ? DebtRepayments.FetchAsync(day, token) : Task.FromResult<IReadOnlyList<CashEntryRow>>([]);
        var counts = Count.FetchAsync(day, token);
        var payLater = ShowPayLater ? PayLaterOfAsync(day, token) : Task.FromResult<IReadOnlyList<PayLaterRow>>([]);
        await Task.WhenAll(summary, payouts, floats, repayments, counts, payLater);

        return new LoadedDay(await summary, await payouts, await floats, await repayments, await counts, await payLater);
    }

    // No route lists one day's ลงบัญชี for a cashier (/reports/pay-later is managers' and per customer), so the
    // till keeps the day's own from the full list. Completed ones too: paid off later, they were still taken that day.
    private async Task<IReadOnlyList<PayLaterRow>> PayLaterOfAsync(DateOnly day, CancellationToken token) =>
        (await _client.GetPayLaterAsync(includeCompleted: true, searchTerm: null, token)).Items
        .Where(p => _clock.BusinessDateOf(p.CreatedUtc) == day)
        .OrderBy(p => p.CreatedUtc)
        .Select(p => new PayLaterRow(_clock.ToStoreTime(p.CreatedUtc), p.Description, p.PayLaterAmount))
        .ToList();

    private void Show(DateOnly day, LoadedDay loaded)
    {
        BusinessDate = day;
        Summary = loaded.Summary;
        Payouts.Show(loaded.Payouts);
        Floats.Show(loaded.Floats);
        DebtRepayments.Show(loaded.DebtRepayments);
        Count.Show(loaded.Counts);
        TodaysPayLater.Clear();
        foreach (var row in loaded.PayLater)
            TodaysPayLater.Add(row);
        SetEditable(loaded.Summary.IsEditable);
    }

    // Debt repayments settle PayLater, so a store without it has nothing to repay.
    private void SetEditable(bool isEditable)
    {
        IsEditable = Payouts.IsEditable = Floats.IsEditable = Count.IsEditable = isEditable;
        DebtRepayments.IsEditable = isEditable && ShowPayLater;
    }

    private CancellationToken Restart()
    {
        _current.Cancel();
        _current.Dispose();
        _current = new CancellationTokenSource();
        return _current.Token;
    }
}
