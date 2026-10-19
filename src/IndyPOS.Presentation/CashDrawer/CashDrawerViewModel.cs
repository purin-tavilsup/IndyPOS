using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Presentation.SalesHistory;

namespace IndyPOS.Presentation.CashDrawer;

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
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    private CashDrawerSummaryDto? _summary;

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

    public bool IsEditable => Summary?.IsEditable ?? false;

    public bool ShowProductTypeSplit { get; }

    public bool ShowPayLater { get; }

    public bool CanPickDay => _access.CanViewAnyDay;

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task RefreshAsync() => LoadAsync();

    // Another day only for reports.view; StoreHub would refuse it anyway.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ShowDayAsync(DateOnly day)
    {
        if (day != _clock.Today && !CanPickDay)
            return Task.CompletedTask;

        BusinessDate = day;
        return LoadAsync();
    }

    private async Task LoadAsync()
    {
        var token = Restart();
        var day = BusinessDate;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await LoadDayAsync(day, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A newer load replaced this one.
        }
        catch (StoreHubException ex) when (!token.IsCancellationRequested)
        {
            ErrorMessage = string.Format(LoadFailed, ex.Message);
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsBusy = false;
        }
    }

    private async Task LoadDayAsync(DateOnly day, CancellationToken token)
    {
        var summary = await _client.GetCashSummaryAsync(day, token);
        token.ThrowIfCancellationRequested();
        Summary = summary;
        SetEditable(summary.IsEditable);

        await Payouts.LoadAsync(day, token);
        await Floats.LoadAsync(day, token);
        if (ShowPayLater)
            await DebtRepayments.LoadAsync(day, token);
        await Count.LoadAsync(day, token);
    }

    private void SetEditable(bool isEditable) =>
        Payouts.IsEditable = Floats.IsEditable = DebtRepayments.IsEditable = Count.IsEditable = isEditable;

    private CancellationToken Restart()
    {
        _current.Cancel();
        _current.Dispose();
        _current = new CancellationTokenSource();
        return _current.Token;
    }
}
