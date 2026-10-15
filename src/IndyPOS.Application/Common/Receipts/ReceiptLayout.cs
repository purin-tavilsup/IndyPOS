using System.Globalization;

namespace IndyPOS.Application.Common.Receipts;

public enum ReceiptRowStyle
{
    Banner,
    StoreName,
    Text,
    Indented,
    Rule
}

/// <summary>One printed row: text on the left, and an amount in the price column when there is one.</summary>
public sealed record ReceiptRow(ReceiptRowStyle Style, string Text = "", string? Amount = null);

/// <summary>
/// Every word a receipt prints, in order. Pure, so the content is tested without a printer; the
/// renderer only draws these rows. Dates and amounts follow the given culture, as v3 receipts did.
/// </summary>
public static class ReceiptLayout
{
    public const string CopyBanner = "สำเนา / COPY";

    private static readonly ReceiptRow RuleRow = new(ReceiptRowStyle.Rule);

    public static IReadOnlyList<ReceiptRow> Rows(ReceiptHeader header, ReceiptDocument document, CultureInfo culture)
    {
        var rows = new List<ReceiptRow>();

        AddCopyBanner(rows, document.Reprint);
        AddHeader(rows, header);
        AddBillInfo(rows, document, culture);
        AddItems(rows, document, culture);
        AddPayments(rows, document, culture);

        return rows;
    }

    private static void AddCopyBanner(List<ReceiptRow> rows, ReceiptReprint? reprint)
    {
        if (reprint is not null)
            rows.Add(new ReceiptRow(ReceiptRowStyle.Banner, CopyBanner));
    }

    // Two rows, not one: "พิมพ์ซ้ำ <date> โดย <name>" overruns a 58 mm roll for any real name.
    private static void AddReprintNote(List<ReceiptRow> rows, ReceiptReprint? reprint, CultureInfo culture)
    {
        if (reprint is null)
            return;

        rows.Add(Text($"พิมพ์ซ้ำ {reprint.ReprintedAt.ToString("dd/MM/yyyy HH:mm", culture)}"));
        rows.Add(Text($"โดย {reprint.ReprintedBy}"));
    }

    private static void AddHeader(List<ReceiptRow> rows, ReceiptHeader header)
    {
        rows.Add(new ReceiptRow(ReceiptRowStyle.StoreName, header.StoreName));
        rows.Add(new ReceiptRow(ReceiptRowStyle.Text, header.AddressLine1));
        rows.Add(new ReceiptRow(ReceiptRowStyle.Text, header.AddressLine2));
        rows.Add(new ReceiptRow(ReceiptRowStyle.Text, $"Tel.: {header.PhoneNumber}"));
        rows.Add(RuleRow);
    }

    private static void AddBillInfo(List<ReceiptRow> rows, ReceiptDocument document, CultureInfo culture)
    {
        // The bill number is digits only, so it must not take the culture's digit grouping.
        rows.Add(Text($"Invoice No.: {document.InvoiceNumber.ToString("0000000000", CultureInfo.InvariantCulture)}"));
        rows.Add(Text($"Date: {document.IssuedAt.ToString("dd MMMM yyyy", culture)}"));
        rows.Add(Text($"Time: {document.IssuedAt.ToString("hh:mm tt", culture)}"));
        rows.Add(Text($"Cashier: {document.CashierName}"));
        AddReprintNote(rows, document.Reprint, culture);
        rows.Add(RuleRow);
    }

    private static void AddItems(List<ReceiptRow> rows, ReceiptDocument document, CultureInfo culture)
    {
        rows.Add(Text("รายการสินค้า"));

        foreach (var item in document.Items)
        {
            rows.Add(Text(ItemDescription(item, document.IsRefund)));
            rows.Add(new ReceiptRow(ReceiptRowStyle.Indented,
                                    $"{item.Quantity} x {item.UnitPrice.ToString("N", culture)}",
                                    item.LineTotal.ToString("N", culture)));
        }

        rows.Add(RuleRow);
        rows.Add(Text("ราคารวม", document.Total.ToString("N", culture)));
        rows.Add(RuleRow);
    }

    private static void AddPayments(List<ReceiptRow> rows, ReceiptDocument document, CultureInfo culture)
    {
        rows.Add(Text("รายการเงิน"));

        foreach (var payment in document.Payments)
            rows.Add(Text(PaymentDescription(payment, document.IsRefund), payment.Amount.ToString("N", culture)));

        rows.Add(RuleRow);
        rows.Add(Text("เงินรวม", document.AmountReceived.ToString("N", culture)));
        rows.Add(Text("เงินทอน", document.Change.ToString("N", culture)));
        rows.Add(RuleRow);
    }

    private static string ItemDescription(ReceiptItem item, bool isRefund) => (isRefund, string.IsNullOrEmpty(item.Note)) switch
    {
        (true, false) => $"{item.Description} : {item.Note} (คืนสินค้า)",
        (true, true) => $"{item.Description} : (คืนสินค้า)",
        (false, false) => $"{item.Description} : {item.Note}",
        _ => item.Description
    };

    private static string PaymentDescription(ReceiptPayment payment, bool isRefund)
    {
        if (isRefund)
            return $"{payment.DisplayName} : คืนเงิน";

        return string.IsNullOrEmpty(payment.Note) ? payment.DisplayName : $"{payment.DisplayName} : {payment.Note}";
    }

    private static ReceiptRow Text(string text, string? amount = null) => new(ReceiptRowStyle.Text, text, amount);
}
