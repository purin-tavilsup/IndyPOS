using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;

namespace IndyPOS.Presentation.SalesHistory;

/// <summary>A bill in the list, its time already store-local and its payment method by name.</summary>
public sealed record InvoiceRow(Guid Id, long InvoiceNumber, DateTime CreatedAt, decimal TotalAmount,
                                string PaymentMethodName, int LineCount);

public sealed record DateRange(DateOnly From, DateOnly To);

/// <summary>
/// The bill list (spec §7). A manager picks a period; a cashier sees today only. Any newer request
/// cancels the one in flight, so an older answer never replaces a newer list. Nothing changes until
/// StoreHub answers.
/// </summary>
public sealed partial class InvoiceHistoryViewModel : ObservableObject
{
    public const int PageSize = 50;
    public const string NotANumber = "กรุณาใส่เลขที่บิลเป็นตัวเลข";
    public const string NumberNotFound = "ไม่พบบิลเลขที่ {0}";
    public const string RangeReversed = "วันที่เริ่มต้นต้องไม่เกินวันที่สิ้นสุด";
    public const string LoadFailed = "โหลดรายการบิลไม่สำเร็จ: {0}";
    public const string SearchFailed = "ค้นหาบิลไม่สำเร็จ: {0}";

    private const int FirstPage = 1;

    private readonly IStoreHubClient _client;
    private readonly StoreClock _clock;
    private readonly SalesHistoryAccess _access;
    private CancellationTokenSource _current = new();
    private int _page;

    // Bumped whenever a first page replaces the list; a later page for an earlier list is dropped.
    private int _listVersion;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool _hasMore;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public InvoiceHistoryViewModel(IStoreHubClient client, StoreClock clock, SalesHistoryAccess access,
                                   InvoiceDetailViewModel detail)
    {
        _client = client;
        _clock = clock;
        _access = access;
        Detail = detail;
    }

    public InvoiceDetailViewModel Detail { get; }

    public bool IsTodayOnly => _access.IsTodayOnly;

    public ObservableCollection<InvoiceRow> Bills { get; } = [];

    public DateOnly From { get; private set; }

    public DateOnly To { get; private set; }

    private bool CanPickPeriod() => _access.CanViewAnyDay;

    [RelayCommand]
    private Task ShowTodayAsync() => ShowAsync(_clock.Today, _clock.Today);

    [RelayCommand(CanExecute = nameof(CanPickPeriod))]
    private Task ShowThisMonthAsync()
    {
        var today = _clock.Today;

        return ShowAsync(new DateOnly(today.Year, today.Month, 1), today);
    }

    [RelayCommand(CanExecute = nameof(CanPickPeriod))]
    private Task ShowThisYearAsync()
    {
        var today = _clock.Today;

        return ShowAsync(new DateOnly(today.Year, 1, 1), today);
    }

    [RelayCommand(CanExecute = nameof(CanShowRange))]
    private Task ShowRangeAsync(DateRange range)
    {
        if (range.From > range.To)
        {
            ErrorMessage = RangeReversed;
            return Task.CompletedTask;
        }

        return ShowAsync(range.From, range.To);
    }

    private bool CanShowRange(DateRange? range) => CanPickPeriod();

    [RelayCommand(CanExecute = nameof(HasMore))]
    private Task LoadMoreAsync() => LoadPageAsync(new DateRange(From, To), _page + 1, _current.Token);

    // Concurrent: a click on another bill while one loads must load it too, or the list would highlight a
    // bill the detail does not show. The detail drops the earlier bill's late answer itself.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task OpenAsync(InvoiceRow row) => Detail.LoadAsync(row.Id);

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (!long.TryParse(SearchText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            ErrorMessage = NotANumber;
            return;
        }

        var token = Restart();
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var bill = await _client.GetSaleByNumberAsync(number, token);
            if (token.IsCancellationRequested)
                return;

            if (bill is null)
                ErrorMessage = string.Format(NumberNotFound, number);
            else
                Detail.Show(bill);
        }
        catch (StoreHubException ex) when (!token.IsCancellationRequested)
        {
            ErrorMessage = string.Format(SearchFailed, ex.Message);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsBusy = false;
        }
    }

    private Task ShowAsync(DateOnly from, DateOnly to) => LoadPageAsync(new DateRange(from, to), FirstPage, Restart());

    // From and To move only when the new period's first page arrives: until then the list, its
    // period and "load more" all still belong to the period on screen.
    private async Task LoadPageAsync(DateRange range, int page, CancellationToken token)
    {
        var listVersion = _listVersion;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _client.ListSalesAsync(range.From, range.To, page, PageSize, token);
            if (token.IsCancellationRequested)
                return;

            if (page == FirstPage)
                ReplaceList(range);
            else if (listVersion != _listVersion)
                return;

            foreach (var bill in result.Items)
                Bills.Add(ToRow(bill));

            _page = page;
            HasMore = result.HasMore;
        }
        catch (StoreHubException ex) when (!token.IsCancellationRequested)
        {
            ErrorMessage = string.Format(LoadFailed, ex.Message);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsBusy = false;
        }
    }

    private void ReplaceList(DateRange range)
    {
        _listVersion++;
        From = range.From;
        To = range.To;
        Bills.Clear();
    }

    private CancellationToken Restart()
    {
        _current.Cancel();
        _current.Dispose();
        _current = new CancellationTokenSource();

        return _current.Token;
    }

    // StoreHub names the method from the store's whole catalogue, so an ended campaign keeps its name; an
    // older StoreHub sends no name, and the code is shown rather than a blank.
    private InvoiceRow ToRow(InvoiceSummaryDto bill) =>
        new(bill.Id, bill.InvoiceNumber, _clock.ToStoreTime(bill.CreatedUtc), bill.TotalAmount,
            bill.PrimaryPaymentMethodName ?? bill.PrimaryPaymentMethod, bill.LineCount);
}
