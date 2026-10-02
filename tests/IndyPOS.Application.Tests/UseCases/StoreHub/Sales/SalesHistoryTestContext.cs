using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.QueryHandlers.Sales;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

/// <summary>What one seeded bill looks like. Defaults: ฿350 paid with ฿500 cash, one line.</summary>
internal sealed record InvoiceSeed(long Number, DateTime CreatedUtc)
{
    public decimal Total { get; init; } = 350m;
    public string? StoreId { get; init; }
    public Guid UserId { get; init; } = SalesHistoryTestContext.CashierId;
    public string? ProductCategory { get; init; }
    public IReadOnlyList<SalePayment> Payments { get; init; } = [new(PaymentMethodCodes.Cash, 500m)];
}

/// <summary>
/// One InMemory StoreHub database, a fake clock at 10:00 Bangkok on <see cref="Today"/>, and a
/// store in the Bangkok timezone.
/// </summary>
internal sealed class SalesHistoryTestContext : IAsyncDisposable
{
    public static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    public static readonly DateOnly Today = new(2026, 9, 27);
    public static readonly DateOnly Yesterday = Today.AddDays(-1);

    /// <summary>10:00 Bangkok on <see cref="Today"/>.</summary>
    public static readonly DateTimeOffset TenAmBangkok = new(2026, 9, 27, 3, 0, 0, TimeSpan.Zero);

    /// <summary>09:00 Bangkok on <see cref="Today"/>.</summary>
    public static readonly DateTime NineAmTodayUtc = new(2026, 9, 27, 2, 0, 0, DateTimeKind.Utc);

    /// <summary>09:30 Bangkok on <see cref="Today"/>.</summary>
    public static readonly DateTime HalfPastNineTodayUtc = new(2026, 9, 27, 2, 30, 0, DateTimeKind.Utc);

    /// <summary>09:00 Bangkok on <see cref="Yesterday"/>.</summary>
    public static readonly DateTime NineAmYesterdayUtc = new(2026, 9, 26, 2, 0, 0, DateTimeKind.Utc);

    /// <summary>00:05 Bangkok on <see cref="Today"/> — still 26 Sep in UTC.</summary>
    public static readonly DateTime JustAfterMidnightTodayUtc = new(2026, 9, 26, 17, 5, 0, DateTimeKind.Utc);

    public static readonly Guid CashierId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string OtherStoreId = "other-store";

    public SalesHistoryTestContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new StoreHubDbContext(options);
        StoreIdentity = MockStoreIdentityService.GeneralHardware();
        StoreIdentity.TimeZone = Bangkok;
        Clock = new CashDrawerClock(new FakeTimeProvider(TenAmBangkok), StoreIdentity);
    }

    public StoreHubDbContext Db { get; }
    public MockStoreIdentityService StoreIdentity { get; }
    public ICashDrawerClock Clock { get; }

    public ListSalesQueryHandler ListHandler() => new(Db, StoreIdentity, Clock);

    public GetSaleQueryHandler DetailHandler() => new(Db, StoreIdentity, Clock);

    public async Task<Invoice> SeedInvoiceAsync(InvoiceSeed seed)
    {
        var storeId = seed.StoreId ?? StoreIdentity.StoreId;
        var product = new Product
        {
            Id = Guid.NewGuid(), StoreId = storeId, Barcode = $"885{seed.Number:D10}", Name = "Cement 50kg",
            Category = seed.ProductCategory, UnitPrice = seed.Total, CreatedUtc = seed.CreatedUtc, LastModifiedUtc = seed.CreatedUtc
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), StoreId = storeId, UserId = seed.UserId, InvoiceNumber = seed.Number,
            TotalAmount = seed.Total, CreatedUtc = seed.CreatedUtc, LastModifiedUtc = seed.CreatedUtc
        };
        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(), InvoiceId = invoice.Id, ProductId = product.Id, ProductName = product.Name,
            Quantity = 1, UnitPrice = seed.Total, Note = "ถุงใหญ่", CreatedUtc = seed.CreatedUtc, Product = product
        });
        foreach (var payment in seed.Payments)
        {
            invoice.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(), InvoiceId = invoice.Id, Method = payment.Method, Amount = payment.Amount, CreatedUtc = seed.CreatedUtc
            });
        }

        Db.Invoices.Add(invoice);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return invoice;
    }

    public async Task SeedCashierAsync(string firstName, string lastName)
    {
        Db.StoreUsers.Add(new StoreUser
        {
            Id = CashierId, StoreId = StoreIdentity.StoreId, Username = "somchai", FirstName = firstName, LastName = lastName,
            RoleId = 1, CreatedAtUtc = NineAmTodayUtc, LastModifiedAtUtc = NineAmTodayUtc
        });
        await Db.SaveChangesAsync();
    }

    public async Task SeedCategoryAsync(string code, ProductCategoryKind kind)
    {
        Db.ProductCategories.Add(new ProductCategory
        {
            StoreId = StoreIdentity.StoreId, Code = code, DisplayName = code, Kind = kind, IsEnabled = true,
            DisplayOrder = 1, CreatedUtc = NineAmTodayUtc, LastModifiedUtc = NineAmTodayUtc
        });
        await Db.SaveChangesAsync();
    }

    public async Task SeedPaymentMethodAsync(string code, string displayName)
    {
        Db.PaymentMethods.Add(new PaymentMethod
        {
            StoreId = StoreIdentity.StoreId, Code = code, DisplayName = displayName, IsEnabled = true,
            DisplayOrder = 1, CreatedUtc = NineAmTodayUtc, LastModifiedUtc = NineAmTodayUtc
        });
        await Db.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => Db.DisposeAsync();
}
