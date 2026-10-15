using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Presentation.SalesHistory;

/// <summary>
/// One bill (spec §7): its lines and payments in store time, the Hardware / General split, and
/// reprint. The void feature adds its command here later.
/// </summary>
public sealed partial class InvoiceDetailViewModel(
    IStoreHubClient client,
    IReceiptPrinterService printer,
    ReceiptDocumentFactory receipts,
    StoreClock clock,
    SalesHistoryAccess access) : ObservableObject
{
    public const string BillNotFound = "ไม่พบบิล";
    public const string LoadFailed = "โหลดบิลไม่สำเร็จ: {0}";
    public const string ReprintFailed = "พิมพ์ซ้ำไม่สำเร็จ: {0}";
    public const string PrintFailed = "บันทึกการพิมพ์ซ้ำแล้ว แต่เครื่องพิมพ์ไม่ทำงาน: {0}";

    // Bumped by every load; a response for an older load is dropped.
    private int _loadVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IssuedAt), nameof(HardwareTotal), nameof(GeneralTotal))]
    [NotifyCanExecuteChangedFor(nameof(ReprintCommand))]
    private InvoiceDetailDto? _bill;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public DateTime? IssuedAt => Bill is null ? null : clock.ToStoreTime(Bill.CreatedUtc);

    public decimal HardwareTotal =>
        Bill?.Lines.Where(l => l.CategoryKind == ProductCategoryKind.Hardware).Sum(l => l.LineTotal) ?? 0m;

    // Everything that is not hardware, including a line whose product has no category any more.
    public decimal GeneralTotal => (Bill?.Lines.Sum(l => l.LineTotal) ?? 0m) - HardwareTotal;

    /// <summary>Shows a bill the caller already has, e.g. from a bill-number search.</summary>
    public void Show(InvoiceDetailDto bill)
    {
        _loadVersion++;
        ErrorMessage = null;
        Bill = bill;
    }

    public async Task LoadAsync(Guid saleId)
    {
        var version = ++_loadVersion;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var bill = await client.GetSaleAsync(saleId);
            if (version != _loadVersion)
                return;

            Bill = bill;
            ErrorMessage = bill is null ? BillNotFound : null;
        }
        catch (StoreHubException ex) when (version == _loadVersion)
        {
            Bill = null;
            ErrorMessage = string.Format(LoadFailed, ex.Message);
        }
        finally
        {
            if (version == _loadVersion)
                IsBusy = false;
        }
    }

    private bool CanReprint() =>
        Bill is not null
        && access.CanReprint
        && TodayOnlyRule.Allows(clock.BusinessDateOf(Bill.CreatedUtc), clock.Today, access.CanViewAnyDay);

    [RelayCommand(CanExecute = nameof(CanReprint))]
    private async Task ReprintAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await client.CreateReprintAsync(Bill!.Id, cancellationToken);
            Print(receipts.ForReprint(result, access.UserDisplayName));
        }
        catch (StoreHubException ex)
        {
            ErrorMessage = string.Format(ReprintFailed, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // The reprint is already recorded; say so, so a retry is known to write a second record.
    private void Print(ReceiptDocument copy)
    {
        try
        {
            printer.PrintReceipt(copy);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = string.Format(PrintFailed, ex.Message);
        }
    }
}
