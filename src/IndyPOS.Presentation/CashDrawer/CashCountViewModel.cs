using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Domain.ValueObjects;
using IndyPOS.Presentation.SalesHistory;

namespace IndyPOS.Presentation.CashDrawer;

/// <summary>
/// A saved count in the day's history, newest first. Who counted is kept: the history exists so the
/// owner can trace on whose count the drawer went wrong (spec §3).
/// </summary>
public sealed record CashCountRow(DateTime StoreTime, decimal CountedTotal, Guid CountedByUserId, string? CountedByName);

/// <summary>
/// The cash count (spec §3): nine denominations typed by the cashier, saved as a new count every time.
/// Saved counts are never edited; Reset clears only what is typed. The live total while typing uses
/// the same <see cref="CashDenominations"/> as a saved count, so the two cannot disagree.
/// </summary>
public sealed partial class CashCountViewModel : ObservableObject
{
    public const string SaveFailed = "บันทึกการนับเงินไม่สำเร็จ: {0}";

    private readonly IStoreHubClient _client;
    private readonly StoreClock _clock;
    private readonly Func<Task> _reloadScreen;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(TypedTotal))] private int _bankNote1000Count;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TypedTotal))] private int _bankNote500Count;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TypedTotal))] private int _bankNote100Count;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TypedTotal))] private int _bankNote50Count;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TypedTotal))] private int _bankNote20Count;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TypedTotal))] private int _coin10Count;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TypedTotal))] private int _coin5Count;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TypedTotal))] private int _coin2Count;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TypedTotal))] private int _coin1Count;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isEditable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public CashCountViewModel(IStoreHubClient client, StoreClock clock, Func<Task> reloadScreen)
    {
        _client = client;
        _clock = clock;
        _reloadScreen = reloadScreen;
    }

    public ObservableCollection<CashCountRow> History { get; } = [];

    public decimal TypedTotal => CashDenominations.Total(Counts());

    /// <summary>Replaces the history with the day's counts.</summary>
    public async Task LoadAsync(DateOnly day, CancellationToken cancellationToken) =>
        Show(await FetchAsync(day, cancellationToken));

    /// <summary>The day's counts, newest first, not shown yet: the screen shows a whole day at once or not at all.</summary>
    public async Task<IReadOnlyList<CashCountRow>> FetchAsync(DateOnly day, CancellationToken cancellationToken) =>
        (await _client.GetCashCountsAsync(day, cancellationToken))
        .OrderByDescending(c => c.CreatedUtc)
        .Select(c => new CashCountRow(_clock.ToStoreTime(c.CreatedUtc), c.CountedTotal, c.CreatedByUserId, c.CountedByName))
        .ToList();

    public void Show(IReadOnlyList<CashCountRow> history)
    {
        History.Clear();
        foreach (var row in history)
            History.Add(row);
    }

    private bool CanSave() => IsEditable && !IsBusy;

    // The typed counts stay after a save, so the cashier sees what was saved; Reset clears them.
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _client.AddCashCountAsync(Request());
        }
        catch (StoreHubException ex)
        {
            ErrorMessage = string.Format(SaveFailed, ex.Message);
            return;
        }
        finally
        {
            IsBusy = false;
        }

        await _reloadScreen();
    }

    [RelayCommand]
    private void Reset()
    {
        BankNote1000Count = BankNote500Count = BankNote100Count = BankNote50Count = BankNote20Count = 0;
        Coin10Count = Coin5Count = Coin2Count = Coin1Count = 0;
    }

    private AddCashCountRequest Request() => new(
        BankNote1000Count, BankNote500Count, BankNote100Count, BankNote50Count, BankNote20Count,
        Coin10Count, Coin5Count, Coin2Count, Coin1Count);

    private int[] Counts() =>
    [
        BankNote1000Count, BankNote500Count, BankNote100Count, BankNote50Count, BankNote20Count,
        Coin10Count, Coin5Count, Coin2Count, Coin1Count
    ];
}
