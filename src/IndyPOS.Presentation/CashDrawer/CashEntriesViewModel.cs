using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Presentation.CashDrawer;

/// <summary>
/// One cash-entry list on the ลิ้นชักเก็บเงิน screen: payouts, floats or debt repayments (spec §5/§8).
/// Nothing changes on screen until StoreHub confirms; then the whole screen reloads. A refused save
/// leaves the list as it was and keeps what the cashier typed, so nothing is typed twice.
/// </summary>
public sealed partial class CashEntriesViewModel : ObservableObject
{
    public const string InvalidAmount = "จำนวนเงินไม่ถูกต้อง";
    public const string SaveFailed = "บันทึกไม่สำเร็จ: {0}";
    public const string DeleteFailed = "ลบไม่สำเร็จ: {0}";

    private readonly ICashEntryApi _api;
    private readonly Func<Task> _reloadScreen;

    [ObservableProperty]
    private string _amountText = string.Empty;

    [ObservableProperty]
    private string? _text;

    [ObservableProperty]
    private PayoutCategory _category = PayoutCategory.General;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand), nameof(DeleteCommand))]
    private CashEntryRow? _selectedRow;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand), nameof(EditCommand), nameof(DeleteCommand))]
    private bool _isEditable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand), nameof(EditCommand), nameof(DeleteCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public CashEntriesViewModel(ICashEntryApi api, Func<Task> reloadScreen)
    {
        _api = api;
        _reloadScreen = reloadScreen;
    }

    public ObservableCollection<CashEntryRow> Rows { get; } = [];

    public bool HasCategory => _api.HasCategory;

    /// <summary>Replaces the rows with the day's list.</summary>
    public async Task LoadAsync(DateOnly day, CancellationToken cancellationToken) =>
        Show(await FetchAsync(day, cancellationToken));

    /// <summary>The day's list, not shown yet: the screen shows a whole day at once or not at all.</summary>
    public Task<IReadOnlyList<CashEntryRow>> FetchAsync(DateOnly day, CancellationToken cancellationToken) =>
        _api.ListAsync(day, cancellationToken);

    public void Show(IReadOnlyList<CashEntryRow> rows)
    {
        Rows.Clear();
        foreach (var row in rows)
            Rows.Add(row);
    }

    // Selecting a row puts its values in the inputs, ready to edit; clearing it starts a new entry from
    // empty boxes rather than a copy of the row last looked at.
    partial void OnSelectedRowChanged(CashEntryRow? value)
    {
        if (value is null)
        {
            AmountText = string.Empty;
            Text = null;
            Category = PayoutCategory.General;
            return;
        }

        AmountText = value.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        Text = value.Text;
        Category = value.Category ?? PayoutCategory.General;
    }

    private bool CanAdd() => IsEditable && !IsBusy;

    private bool CanChangeSelected() => CanAdd() && SelectedRow is not null;

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private Task AddAsync() => SaveAsync(input => _api.AddAsync(input, CancellationToken.None));

    [RelayCommand(CanExecute = nameof(CanChangeSelected))]
    private Task EditAsync() => SaveAsync(input => _api.EditAsync(SelectedRow!.Id, input, CancellationToken.None));

    [RelayCommand(CanExecute = nameof(CanChangeSelected))]
    private Task DeleteAsync() =>
        CallAsync(() => _api.DeleteAsync(SelectedRow!.Id, CancellationToken.None), DeleteFailed);

    private Task SaveAsync(Func<CashEntryInput, Task> save)
    {
        if (!TryReadAmount(out var amount))
        {
            ErrorMessage = InvalidAmount;
            return Task.CompletedTask;
        }

        return CallAsync(() => save(new CashEntryInput(amount, Text, Category)), SaveFailed);
    }

    // The only check here is the input's shape; StoreHub owns every other rule and says why in Thai.
    private bool TryReadAmount(out decimal amount) =>
        decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

    private async Task CallAsync(Func<Task> call, string failure)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await call();
        }
        catch (StoreHubException ex)
        {
            ErrorMessage = string.Format(failure, ex.Message);
            return;
        }
        finally
        {
            IsBusy = false;
        }

        ClearInputs();
        await _reloadScreen();
    }

    private void ClearInputs()
    {
        SelectedRow = null;
        AmountText = string.Empty;
        Text = null;
        Category = PayoutCategory.General;
    }
}
