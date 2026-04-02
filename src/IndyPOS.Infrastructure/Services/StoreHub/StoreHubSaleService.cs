using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Events;
using IndyPOS.Application.UseCases.InventoryProducts;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Domain.Events;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Sale service implementation that uses StoreHub API instead of direct SQLite access.
/// Maintains in-memory cart state and calls StoreHub API to complete sales.
/// </summary>
public class StoreHubSaleService : ISaleService
{
    private readonly IStoreHubClient _storeHubClient;
    private readonly IProductCacheService _productCache;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StoreHubSaleService> _logger;
    private ILoggedInUser? _loggedInUser;

    public IList<Product> Products { get; private set; } = new List<Product>();
    public IList<Payment> Payments { get; private set; } = new List<Payment>();

    public StoreHubSaleService(
        IStoreHubClient storeHubClient,
        IProductCacheService productCache,
        IEventAggregator eventAggregator,
        ILogger<StoreHubSaleService> logger)
    {
        _storeHubClient = storeHubClient;
        _productCache = productCache;
        _eventAggregator = eventAggregator;
        _logger = logger;

        SubscribeEvents();
    }

    private void SubscribeEvents()
    {
        _eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);
        _eventAggregator.GetEvent<UserLoggedOutEvent>().Subscribe(OnUserLoggedOut);
    }

    private void OnUserLoggedIn(ILoggedInUser loggedInUser)
    {
        _loggedInUser = loggedInUser;
    }

    private void OnUserLoggedOut()
    {
        _loggedInUser = null;
    }

    public void StartNewSale()
    {
        Products = new List<Product>();
        Payments = new List<Payment>();

        _eventAggregator.GetEvent<NewSaleStartedEvent>().Publish();
    }

    public void RemoveAllPayments()
    {
        Payments.Clear();
        _eventAggregator.GetEvent<AllPaymentsRemovedEvent>().Publish();
    }

    #region Product Management

    public void AddProduct(InventoryProductDto product)
    {
        AddProductInternal(product, product.UnitPrice, 1, string.Empty);
        _eventAggregator.GetEvent<InvoiceProductAddedEvent>().Publish();
    }

    public void AddProduct(InventoryProductDto product, decimal unitPrice, int quantity, string note)
    {
        AddProductInternal(product, unitPrice, quantity, note);
        _eventAggregator.GetEvent<InvoiceProductAddedEvent>().Publish();
    }

    private void AddProductInternal(InventoryProductDto product, decimal unitPrice, int quantity, string note)
    {
        var productToAdd = ConvertToInvoiceProduct(product);

        productToAdd.Priority = GetNextProductPriority();
        productToAdd.UnitPrice = unitPrice;
        productToAdd.Quantity = quantity;
        productToAdd.Note = note;

        Products.Add(productToAdd);
    }

    private static Product ConvertToInvoiceProduct(InventoryProductDto product)
    {
        return new Product
        {
            Id = product.Id,
            Barcode = product.Barcode,
            Description = product.Description,
            Manufacturer = product.Manufacturer,
            Brand = product.Brand,
            Category = product.Category,
            UnitPrice = product.UnitPrice,
            OriginalUnitPrice = product.UnitPrice,
            Quantity = 1,
            GroupPrice = product.GroupPrice,
            GroupPriceQuantity = product.GroupPriceQuantity,
            IsGroupProduct = false,
            IsTrackable = product.IsTrackable
        };
    }

    public Task<InventoryProductDto> GetInventoryProductByBarcodeAsync(string barcode)
    {
        // Use product cache instead of SQLite
        var cachedProduct = _productCache.GetByBarcode(barcode);

        if (cachedProduct is null)
        {
            throw new ProductNotFoundException($"Product not found: {barcode}");
        }

        var dto = ConvertToInventoryDto(cachedProduct);

        return Task.FromResult(dto);
    }

    private static InventoryProductDto ConvertToInventoryDto(ProductDto product)
    {
        return new InventoryProductDto
        {
            Id = product.Id,
            Barcode = product.Barcode,
            Description = product.Name,
            Manufacturer = product.Manufacturer ?? string.Empty,
            Brand = product.Brand ?? string.Empty,
            Category = 0, // Category is string in StoreHub, int in legacy
            UnitPrice = product.UnitPrice,
            GroupPrice = product.GroupPrice ?? 0,
            GroupPriceQuantity = product.GroupPriceQuantity,
            IsTrackable = true, // All StoreHub products are trackable
            QuantityInStock = 0 // Stock is managed by StoreHub
        };
    }

    public Product GetSaleInvoiceProduct(string barcode, int priority)
    {
        var result = Products.FirstOrDefault(p => p.Barcode == barcode && p.Priority == priority);

        if (result is null)
        {
            throw new ProductNotFoundException($"Sale Invoice Product not found. Barcode: {barcode}, Priority: {priority}");
        }

        return result;
    }

    public void RemoveProduct(Product product)
    {
        Products.Remove(product);
        _eventAggregator.GetEvent<InvoiceProductRemovedEvent>().Publish();
    }

    public void UpdateProductUnitPrice(Guid productId, int priority, decimal unitPrice, string note)
    {
        var productToUpdate = Products.FirstOrDefault(p => p.Id == productId && p.Priority == priority);

        if (productToUpdate is null)
        {
            throw new ProductNotFoundException($"Sale Invoice Product not found. ProductId: {productId}, Priority: {priority}");
        }

        if (productToUpdate.UnitPrice == unitPrice)
            return;

        productToUpdate.UnitPrice = unitPrice;
        productToUpdate.Note = note;

        _eventAggregator.GetEvent<InvoiceProductUpdatedEvent>().Publish();
    }

    public Task UpdateProductQuantityAsync(Guid productId, int priority, int newQuantity)
    {
        var productToUpdate = Products.FirstOrDefault(p => p.Id == productId && p.Priority == priority);

        if (productToUpdate is null)
        {
            throw new ProductNotFoundException($"Sale Invoice Product not found. ProductId: {productId}, Priority: {priority}");
        }

        if (productToUpdate.Quantity == newQuantity)
            return Task.CompletedTask;

        // Simplified quantity update (group price logic can be added later if needed)
        productToUpdate.Quantity = newQuantity;
        _eventAggregator.GetEvent<InvoiceProductUpdatedEvent>().Publish();

        return Task.CompletedTask;
    }

    #endregion

    #region Payment Management

    public void AddPayment(PaymentType paymentType, decimal paymentAmount, string note)
    {
        var payment = new Payment
        {
            PaymentTypeId = (int)paymentType,
            Priority = GetNextPaymentPriority(),
            Amount = paymentAmount,
            Note = note
        };

        Payments.Add(payment);
        _eventAggregator.GetEvent<InvoicePaymentAddedEvent>().Publish();
    }

    #endregion

    #region Calculations

    public bool IsRefundInvoice() => CalculateInvoiceTotal() < 0;

    public bool IsPendingPayment()
    {
        var invoiceTotal = CalculateInvoiceTotal();
        var paymentTotal = CalculatePaymentTotal();

        if (invoiceTotal < 0)
            return invoiceTotal != paymentTotal;

        return invoiceTotal > paymentTotal;
    }

    public decimal CalculateInvoiceTotal() => Products.Sum(p => p.GetTotal());

    public decimal CalculatePaymentTotal() => Payments.Sum(p => p.Amount);

    public decimal CalculateBalanceRemaining() => CalculateInvoiceTotal() - CalculatePaymentTotal();

    public decimal CalculateChanges()
    {
        var invoiceTotal = CalculateInvoiceTotal();
        var paymentTotal = CalculatePaymentTotal();

        if (invoiceTotal < 0)
            return 0m;

        var amount = paymentTotal - invoiceTotal;
        return amount >= 0 ? amount : 0m;
    }

    private int GetNextProductPriority() =>
        Products.Count > 0 ? Products.Max(p => p.Priority) + 1 : 1;

    private int GetNextPaymentPriority() =>
        Payments.Count > 0 ? Payments.Max(p => p.Priority) + 1 : 1;

    #endregion

    #region Sale Completion

    public IList<string> ValidateSaleInvoice()
    {
        var messages = new List<string>();

        if (_loggedInUser is null)
            messages.Add("ไม่พบผู้ใช้งานในระบบ");

        if (!Products.Any())
            messages.Add("ไม่มีรายการสินค้า");

        if (!Payments.Any())
            messages.Add("ไม่มีรายการเงิน");

        if (IsPendingPayment())
            messages.Add("รายการเงินยังไม่สมบูรณ์");

        if (!_storeHubClient.IsAuthenticated)
            messages.Add("ไม่ได้เชื่อมต่อกับ StoreHub");

        return messages;
    }

    public async Task<IInvoiceInfo> CompleteSaleAsync()
    {
        if (_loggedInUser is null)
            throw new UserNotLoggedInException("User has not logged in.");

        if (!_storeHubClient.IsAuthenticated)
            throw new StoreHubClientException("Not connected to StoreHub.");

        var invoiceTotal = CalculateInvoiceTotal();

        _logger.LogInformation("Completing sale via StoreHub API. Total: {Total}", invoiceTotal);

        // Build StoreHub request
        var request = BuildCompleteSaleRequest();

        // Call StoreHub API
        var response = await _storeHubClient.CompleteSaleAsync(request);

        _logger.LogInformation("Sale completed. InvoiceId: {InvoiceId}", response.InvoiceId);

        // Create invoice info for receipt printing
        var invoiceInfo = CreateInvoiceInfo(response);

        return invoiceInfo;
    }

    private CompleteSaleRequest BuildCompleteSaleRequest()
    {
        var lines = Products.Select(p => new SaleLineRequest(
            ProductId: p.Id,
            Quantity: p.Quantity,
            UnitPrice: p.UnitPrice
        )).ToList();

        var payments = Payments.Select(p => new SalePaymentRequest(
            Method: MapPaymentType((PaymentType)p.PaymentTypeId),
            Amount: p.Amount,
            Note: string.IsNullOrEmpty(p.Note) ? null : p.Note
        )).ToList();

        return new CompleteSaleRequest(
            UserId: _loggedInUser!.UserId,
            Lines: lines,
            Payments: payments);
    }

    private static string MapPaymentType(PaymentType paymentType) => paymentType switch
    {
        PaymentType.Cash => "Cash",
        PaymentType.PayLater => "PayLater",
        PaymentType.WelfareCard => "WelfareCard",
        PaymentType.M33WeLove => "M33WeLove",
        PaymentType.MoneyTransfer => "Transfer",
        PaymentType.FiftyFifty => "FiftyFifty",
        PaymentType.WeWin => "WeWin",
        _ => paymentType.ToString()
    };

    private IInvoiceInfo CreateInvoiceInfo(CompleteSaleResponse response)
    {
        var paymentTotal = CalculatePaymentTotal();
        var isRefundInvoice = response.TotalAmount < 0;

        return new InvoiceInfo
        {
            Id = 0, // Legacy ID not used, but kept for compatibility
            StoreHubInvoiceId = response.InvoiceId,
            Products = Products,
            Payments = Payments,
            InvoiceTotal = response.TotalAmount,
            PaymentTotal = paymentTotal,
            Changes = CalculateChanges(),
            IsRefundInvoice = isRefundInvoice,
            HasPayLaterPayment = Payments.HasPayLaterPayment()
        };
    }

    #endregion
}
