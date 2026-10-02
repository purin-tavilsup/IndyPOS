using System.Linq.Expressions;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Sales;

/// <summary>
/// One bill of this store, by id or by bill number — same body either way. A bill the caller may
/// not see (another day, no reports.view) comes back as null, exactly like an unknown one.
/// </summary>
/// <remarks>
/// Cashier, category kinds and payment-method names are looked up separately and may be missing:
/// migrated history can reference a deleted user, a placeholder product with no category, or a
/// retired campaign code. None of those may hide the bill.
/// </remarks>
public class GetSaleQueryHandler(
    StoreHubDbContext db,
    IStoreIdentityService storeIdentity,
    ICashDrawerClock clock)
    : IQueryHandler<GetSaleByIdQuery, InvoiceDetailDto?>,
      IQueryHandler<GetSaleByNumberQuery, InvoiceDetailDto?>
{
    public Task<InvoiceDetailDto?> HandleAsync(GetSaleByIdQuery query, CancellationToken cancellationToken = default) =>
        LoadVisibleAsync(i => i.Id == query.InvoiceId, query.CanViewAnyDay, cancellationToken);

    public Task<InvoiceDetailDto?> HandleAsync(GetSaleByNumberQuery query, CancellationToken cancellationToken = default)
    {
        SalesQueryRules.EnsureValidNumber(query.InvoiceNumber);
        return LoadVisibleAsync(i => i.InvoiceNumber == query.InvoiceNumber, query.CanViewAnyDay, cancellationToken);
    }

    private async Task<InvoiceDetailDto?> LoadVisibleAsync(
        Expression<Func<Invoice, bool>> match, bool canViewAnyDay, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
                              .AsNoTracking()
                              .Where(i => i.StoreId == storeIdentity.StoreId)
                              .Where(match)
                              .Include(i => i.Lines)
                              .ThenInclude(l => l.Product)
                              .Include(i => i.Payments)
                              .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null || !IsVisible(invoice, canViewAnyDay))
            return null;

        return await ToDetailAsync(invoice, cancellationToken);
    }

    private bool IsVisible(Invoice invoice, bool canViewAnyDay) =>
        TodayOnlyRule.Allows(
            TodayOnlyRule.BusinessDateOf(invoice.CreatedUtc, storeIdentity.TimeZone),
            clock.Now().BusinessDate,
            canViewAnyDay);

    private async Task<InvoiceDetailDto> ToDetailAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var cashierName = await FindCashierNameAsync(invoice.UserId, cancellationToken);
        var kinds = await FindCategoryKindsAsync(invoice, cancellationToken);
        var methodNames = await FindMethodNamesAsync(invoice, cancellationToken);
        var figures = SaleFigures.From(invoice.TotalAmount, invoice.Payments.Select(p => new SalePayment(p.Method, p.Amount)));

        return new InvoiceDetailDto(
            Id: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber,
            StoreId: invoice.StoreId,
            UserId: invoice.UserId,
            CashierName: cashierName,
            TotalAmount: invoice.TotalAmount,
            AmountReceived: figures.AmountReceived,
            ChangeGiven: figures.ChangeGiven,
            IsRefund: figures.IsRefund,
            HasPayLater: figures.HasPayLater,
            PayLaterAmount: figures.PayLaterAmount,
            CreatedUtc: invoice.CreatedUtc,
            Lines: invoice.Lines.OrderBy(l => l.Priority ?? int.MaxValue)
                                .ThenBy(l => l.Id)
                                .Select(l => ToLineDto(l, kinds))
                                .ToList(),
            Payments: invoice.Payments.OrderBy(p => p.Id)
                                      .Select(p => ToPaymentDto(p, methodNames))
                                      .ToList());
    }

    private async Task<string?> FindCashierNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.StoreUsers
                           .AsNoTracking()
                           .Where(u => u.Id == userId)
                           .Select(u => new { u.FirstName, u.LastName })
                           .FirstOrDefaultAsync(cancellationToken);

        return user is null ? null : $"{user.FirstName} {user.LastName}".Trim();
    }

    private Task<Dictionary<string, ProductCategoryKind>> FindCategoryKindsAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var codes = invoice.Lines.Select(l => l.Product?.Category).OfType<string>().Distinct().ToList();

        return db.ProductCategories
                 .AsNoTracking()
                 .Where(c => c.StoreId == invoice.StoreId && codes.Contains(c.Code))
                 .ToDictionaryAsync(c => c.Code, c => c.Kind, cancellationToken);
    }

    /// <remarks>
    /// Matched ignoring case, as CompleteSaleCommandHandler validates codes: it stores the caller's
    /// spelling, so "cash" is a valid stored code against the catalogue's "Cash". The whole store
    /// catalogue is loaded (about ten rows) because a SQL IN would compare case-sensitively.
    /// </remarks>
    private async Task<Dictionary<string, string>> FindMethodNamesAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var catalogue = await db.PaymentMethods
                                .AsNoTracking()
                                .Where(m => m.StoreId == invoice.StoreId)
                                .Select(m => new { m.Code, m.DisplayName })
                                .ToListAsync(cancellationToken);

        return catalogue.ToDictionary(m => m.Code, m => m.DisplayName, StringComparer.OrdinalIgnoreCase);
    }

    private static InvoiceLineDto ToLineDto(InvoiceLine line, IReadOnlyDictionary<string, ProductCategoryKind> kinds) =>
        new(Id: line.Id,
            ProductId: line.ProductId,
            ProductName: line.ProductName,
            Barcode: line.Product?.Barcode,
            Note: line.Note,
            CategoryKind: line.Product?.Category is { } code && kinds.TryGetValue(code, out var kind) ? kind : null,
            Quantity: line.Quantity,
            UnitPrice: line.UnitPrice,
            LineTotal: line.LineTotal);

    private static PaymentDto ToPaymentDto(Payment payment, IReadOnlyDictionary<string, string> methodNames) =>
        new(Id: payment.Id,
            Method: payment.Method,
            MethodDisplayName: methodNames.GetValueOrDefault(payment.Method, payment.Method),
            Amount: payment.Amount,
            Note: payment.Note);
}
