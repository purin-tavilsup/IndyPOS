using IndyPOS.Common.Enums;
using IndyPOS.Common.Exceptions;
using IndyPOS.Common.Extensions;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.Facade.Adapters;
using IndyPOS.Facade.Events;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models;
using Prism.Events;
using AccountsReceivableModel = IndyPOS.DataAccess.Models.AccountsReceivable;
using InvoiceModel = IndyPOS.DataAccess.Models.Invoice;
using InvoiceProductModel = IndyPOS.DataAccess.Models.InvoiceProduct;
using PaymentModel = IndyPOS.DataAccess.Models.Payment;
using PaymentType = IndyPOS.Common.Enums.PaymentType;

namespace IndyPOS.Facade.Helpers;

public class SaleInvoiceHelper : ISaleInvoiceHelper
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IInvoiceRepository _invoicesRepository;
	private readonly IInventoryHelper _inventoryHelper;
	private readonly IInventoryProductRepository _inventoryProductsRepository;
	private readonly IReceiptPrinterHelper _receiptPrinter;
	private readonly IUserHelper _userHelper;
	private readonly IAccountsReceivableRepository _accountsReceivableRepository;

	public IList<ISaleInvoiceProduct> Products { get; private set; } = new List<ISaleInvoiceProduct>();

	public IList<IPayment> Payments { get; private set; } = new List<IPayment>();

	public SaleInvoiceHelper(IInventoryHelper inventoryHelper,
							 IEventAggregator eventAggregator, 
							 IInvoiceRepository invoicesRepository, 
							 IInventoryProductRepository inventoryProductsRepository,
							 IReceiptPrinterHelper receiptPrinter,
							 IUserHelper userHelper,
							 IAccountsReceivableRepository accountsReceivableRepository)
	{
		_inventoryHelper = inventoryHelper;
		_eventAggregator = eventAggregator;
		_invoicesRepository = invoicesRepository;
		_inventoryProductsRepository = inventoryProductsRepository;
		_receiptPrinter = receiptPrinter;
		_userHelper = userHelper;
		_accountsReceivableRepository = accountsReceivableRepository;
	}
		
	public void StartNewSale()
	{
		Products = new List<ISaleInvoiceProduct>();
		Payments = new List<IPayment>();

		_eventAggregator.GetEvent<NewSaleStartedEvent>().Publish();
	}

	public void RemoveAllPayments()
	{
		Payments.Clear();

		_eventAggregator.GetEvent<AllPaymentsRemovedEvent>().Publish();
	}

	private void AddProductInternal(IInventoryProduct product, decimal unitPrice, int quantity, string note)
	{
		var productToAdd = ConvertToSaleInvoiceProduct(product);

		productToAdd.Priority = GetNextProductPriority();
		productToAdd.UnitPrice = unitPrice;
		productToAdd.Quantity = quantity;
		productToAdd.Note = note;

		Products.Add(productToAdd);
	}

	private void AddProductInternal(IInventoryProduct product, decimal unitPrice, int quantity)
	{
		AddProductInternal(product, unitPrice, quantity, string.Empty);
	}

	private void AddProductInternal(IInventoryProduct product)
	{
		AddProductInternal(product, product.UnitPrice, 1, string.Empty);
	}

	private static ISaleInvoiceProduct ConvertToSaleInvoiceProduct(IInventoryProduct product)
	{
		return new SaleInvoiceProduct
		{
			InventoryProductId = product.InventoryProductId,
			Barcode = product.Barcode,
			Description = product.Description,
			Manufacturer = product.Manufacturer,
			Brand = product.Brand,
			Category = product.Category,
			UnitPrice = product.UnitPrice,
			Quantity = 1,
			GroupPrice = product.GroupPrice,
			GroupPriceQuantity = product.GroupPriceQuantity,
			IsTrackable = product.IsTrackable
		};
	}

	public bool IsRefundInvoice()
	{
		var invoiceTotal = CalculateInvoiceTotal();

		return invoiceTotal < 0;
	}

	public bool IsPendingPayment()
	{
		var invoiceTotal = CalculateInvoiceTotal();
		var paymentTotal = CalculatePaymentTotal();

		if (invoiceTotal < 0)
			return invoiceTotal != paymentTotal;

		return invoiceTotal > paymentTotal;
	}

	public decimal CalculateInvoiceTotal()
	{
		return Products.Sum(p => p.Quantity * p.UnitPrice);
	}

	public decimal CalculatePaymentTotal()
	{
		return Payments.Sum(p => p.Amount);
	}

	public decimal CalculateBalanceRemaining()
	{
		var invoiceTotal = CalculateInvoiceTotal();
		var paymentTotal = CalculatePaymentTotal();

		return invoiceTotal - paymentTotal;
	}

	public decimal CalculateChanges()
	{
		var invoiceTotal = CalculateInvoiceTotal();
		var paymentTotal = CalculatePaymentTotal();

		if (invoiceTotal < 0)
			return 0m;

		var amount = paymentTotal - invoiceTotal;

		return amount >= 0 ? amount : 0m;
	}

	private int GetNextProductPriority()
	{
		return Products.Count > 0 ? Products.Max(p => p.Priority) + 1 : 1;
	}

	private int GetNextPaymentPriority()
	{
		return Payments.Count > 0 ? Payments.Max(p => p.Priority) + 1 : 1;
	}

	public void AddProduct(IInventoryProduct product)
	{
		AddProductInternal(product);

		_eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish();
	}

	public void AddProduct(IInventoryProduct product, decimal unitPrice, int quantity, string note)
	{
		AddProductInternal(product, unitPrice, quantity, note);

		_eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish();
	}

	public ISaleInvoiceProduct GetSaleInvoiceProduct(string barcode, int priority)
	{
		var result = Products.FirstOrDefault(p => p.Barcode  == barcode && 
												  p.Priority == priority);

		if (result is null)
			throw new ProductNotFoundException($"Sale Invoice Product is not found. Barcode: {barcode}. Priority: {priority}.");

		return result;
	}

	public void RemoveProduct(ISaleInvoiceProduct product)
	{
		Products.Remove(product);

		_eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>().Publish();
	}

	public IInventoryProduct GetInventoryProductByBarcode(string barcode)
	{
		return _inventoryHelper.GetInventoryProductByBarcode(barcode);
	}

	private IInventoryProduct GetInventoryProductById(int id)
	{
		return _inventoryHelper.GetProductById(id);
	}

	public void AddPayment(PaymentType paymentType, decimal paymentAmount, string note)
	{
		AddPaymentInternal(paymentType, paymentAmount, note);

		_eventAggregator.GetEvent<PaymentAddedEvent>().Publish();
	}

	private void AddPaymentInternal(PaymentType paymentType, decimal paymentAmount, string note)
	{
		var payment = new Payment
		{
			PaymentTypeId = (int) paymentType,
			Priority = GetNextPaymentPriority(),
			Amount = paymentAmount,
			Note = note
		};

		Payments.Add(payment);
	}

	public void UpdateProductUnitPrice(int inventoryProductId, int priority, decimal unitPrice, string note)
	{
		var productToUpdate = Products.FirstOrDefault(p => p.InventoryProductId == inventoryProductId && 
														   p.Priority           == priority);

		if (productToUpdate is null)
			throw new ProductNotFoundException($"Sale Invoice Product is not found. InventoryProductId: {inventoryProductId}. Priority: {priority}.");

		if (productToUpdate.UnitPrice == unitPrice)
			return;

		productToUpdate.UnitPrice = unitPrice;
		productToUpdate.Note = note;

		_eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish();
	}

	public void UpdateProductQuantity(int inventoryProductId, int priority, int newQuantity)
	{
		var productToUpdate = Products.FirstOrDefault(p => p.InventoryProductId == inventoryProductId &&
														   p.Priority           == priority);

		if (productToUpdate == null)
			throw new ProductNotFoundException($"Sale Invoice Product is not found. InventoryProductId: {inventoryProductId}. Priority: {priority}.");

		if (productToUpdate.Quantity == newQuantity)
			return;

		if ((productToUpdate.GroupPriceQuantity ?? 0) == 0)
		{
			productToUpdate.Quantity = newQuantity;

			_eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish();

			return;
		}

		if (productToUpdate.Quantity < newQuantity)
		{
			IncreaseQuantityWithGroupPriceSettings(productToUpdate, newQuantity);
		}
		else
		{
			DecreaseQuantityWithGroupPriceSettings(productToUpdate, newQuantity);
		}
	}

	private void IncreaseQuantityWithGroupPriceSettings(ISaleInvoiceProduct product, int newQuantity)
	{
		var groupPrice = product.GroupPrice.GetValueOrDefault();
		var groupPriceQuantity = product.GroupPriceQuantity.GetValueOrDefault();

		if (newQuantity < groupPriceQuantity)
		{
			product.Quantity = newQuantity;

			_eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish();

			return;
		}

		product.Quantity = groupPriceQuantity;
		product.UnitPrice = groupPrice / groupPriceQuantity;

		_eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish();

		var remainingQuantity = newQuantity - groupPriceQuantity;

		if (remainingQuantity == 0)
			return;

		AutoAddProductsWithGroupPriceSettings(product.InventoryProductId, remainingQuantity, groupPriceQuantity);
	}

	private void AutoAddProductsWithGroupPriceSettings(int inventoryProductId, int quantity, int groupPriceQuantity)
	{
		var remainingQuantity = quantity;
			
		while (remainingQuantity > 0)
		{
			var quantityToAdd = remainingQuantity > groupPriceQuantity ? groupPriceQuantity : remainingQuantity;

			AutoAddProductWithGroupPriceSettings(inventoryProductId, quantityToAdd);

			remainingQuantity -= groupPriceQuantity;
		}
	}

	private void AutoAddProductWithGroupPriceSettings(int inventoryProductId, int quantity)
	{ 
		var product = GetInventoryProductById(inventoryProductId);

		if (product == null)
			throw new ProductNotFoundException($"Product with InventoryProductId {inventoryProductId} could not be found.");

		var groupPrice = product.GroupPrice.GetValueOrDefault();
		var groupPriceQuantity = product.GroupPriceQuantity.GetValueOrDefault();
		var unitPrice = quantity == groupPriceQuantity ? groupPrice / groupPriceQuantity : product.UnitPrice;

		AddProductInternal(product, unitPrice, quantity);

		_eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish();
	}

	private void DecreaseQuantityWithGroupPriceSettings(ISaleInvoiceProduct product, int newQuantity)
	{
		if (product.Quantity == product.GroupPriceQuantity)
		{
			var inventoryProduct = GetInventoryProductById(product.InventoryProductId);

			if (inventoryProduct is null)
				throw new ProductNotFoundException($"Sale Invoice Product is not found. InventoryProductId: {product.InventoryProductId}.");

			// Restore original unit price
			product.UnitPrice = inventoryProduct.UnitPrice;
		}
            
		product.Quantity = newQuantity;

		_eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish();
	}

	public IList<string> ValidateSaleInvoice()
	{
		var message = new List<string>();

		if (_userHelper.LoggedInUser == null)
			message.Add("ไม่พบผู้ใช้งานในระบบ");

		if (!Products.Any()) 
			message.Add("ไม่มีรายการสินค้า");

		if (!Payments.Any()) 
			message.Add("ไม่มีรายการเงิน");

		if (IsPendingPayment())
			message.Add("รายการเงินยังไม่สมบูรณ์");

		return message;
	}

	public IInvoiceInfo CompleteSale()
	{
		var loggedInUserId = _userHelper.LoggedInUser.UserId;
		var invoiceInfo = GetInvoiceInfo();

		AddInvoiceToDatabase(invoiceInfo, loggedInUserId);
		AddInvoiceProductsToDatabase(invoiceInfo);
		AddPaymentsToDatabase(invoiceInfo);
		UpdateInventoryProductsSoldOnInvoice(invoiceInfo);

		return invoiceInfo;
	}

	public IInvoiceInfo GetInvoiceInfo()
	{
		var invoiceTotal = CalculateInvoiceTotal();
		var paymentTotal = CalculatePaymentTotal();
		var isRefundInvoice = invoiceTotal < 0;

		return new InvoiceInfo
		{
			Id = 0,
			Products = Products,
			Payments = Payments,
			InvoiceTotal = invoiceTotal,
			PaymentTotal = paymentTotal,
			Changes = CalculateChanges(),
			IsRefundInvoice = isRefundInvoice
		};
	}

	public void PrintReceipt()
	{
		var loggedInUser = _userHelper.LoggedInUser;
		var invoiceInfo = GetInvoiceInfo();
			
		_receiptPrinter.PrintReceipt(invoiceInfo, loggedInUser);
	}

	private void UpdateInventoryProductsSoldOnInvoice(IInvoiceInfo invoiceInfo)
	{
		var productGroups = invoiceInfo.Products
									   .Where(p => p.IsTrackable)
									   .GroupBy(p => p.InventoryProductId);

		foreach (var group in productGroups)
		{
			var id = group.Key;
			var quantity = group.Sum(p => p.Quantity);
			var product = GetInventoryProductById(id);
			var newQuantity = product.QuantityInStock - quantity;

			_inventoryProductsRepository.UpdateProductQuantityById(id, newQuantity);
		}
	}

	private void AddInvoiceToDatabase(IInvoiceInfo invoiceInfo, int userId)
	{
		var invoiceId = _invoicesRepository.AddInvoice(new InvoiceModel
		{ 
			Total = invoiceInfo.InvoiceTotal,
			UserId = userId,
			CustomerId = null
		});

		invoiceInfo.Id = invoiceId;
	}

	private void AddInvoiceProductsToDatabase(IInvoiceInfo invoiceInfo)
	{
		foreach(var product in invoiceInfo.Products)
		{
			AddProductToDatabase(product, invoiceInfo.Id);
		}
	}

	private void AddProductToDatabase(ISaleInvoiceProduct product, int invoiceId)
	{
		_invoicesRepository.AddInvoiceProduct(new InvoiceProductModel
		{
			InvoiceId = invoiceId,
			InventoryProductId = product.InventoryProductId,
			Priority = product.Priority,
			Barcode = product.Barcode,
			Description = product.Description,
			Manufacturer = product.Manufacturer,
			Brand = product.Brand,
			Category = product.Category,
			UnitPrice = product.UnitPrice,
			Quantity = product.Quantity,
			Note = product.Note
		});
	}

	private void AddPaymentsToDatabase(IInvoiceInfo invoiceInfo)
	{
		foreach(var payment in invoiceInfo.Payments)
		{
			var invoiceId = invoiceInfo.Id;
			var paymentId = AddPaymentToDatabase(payment, invoiceId);

			if (payment.PaymentTypeId == (int) PaymentType.AccountReceivable)
			{
				AddAccountsReceivableToDatabase(payment, paymentId, invoiceId);
			}
		}
	}

	private int AddPaymentToDatabase(IPayment payment, int invoiceId)
	{
		return _invoicesRepository.AddPayment(new PaymentModel
		{
			InvoiceId = invoiceId,
			PaymentTypeId = payment.PaymentTypeId,
			Amount = payment.Amount,
			Note = payment.Note
		});
	}

	private void AddAccountsReceivableToDatabase(IPayment payment, int paymentId, int invoiceId)
	{
		_accountsReceivableRepository.AddAccountsReceivable(new AccountsReceivableModel
		{
			PaymentId = paymentId,
			Description = payment.Note,
			InvoiceId = invoiceId,
			ReceivableAmount = payment.Amount
		});
	}

	public IEnumerable<IFinalInvoice> GetInvoicesByPeriod(TimePeriod period)
	{
		DateTime startDate;
		DateTime endDate; 

		switch (period)
		{
			case TimePeriod.Today:
				startDate = DateTime.Today;
				endDate = DateTime.Today;
				break;
			case TimePeriod.ThisMonth:
				startDate = DateTime.Today.FirstDayOfMonth();
				endDate = DateTime.Today.LastDayOfMonth();
				break;
			case TimePeriod.ThisYear:
				startDate = DateTime.Today.FirstDayOfYear();
				endDate = DateTime.Today.LastDayOfYear();
				break;
			default:
				startDate = DateTime.Today;
				endDate = DateTime.Today;
				break;
		}

		return GetInvoicesByDateRange(startDate, endDate);
	}

	public IEnumerable<IFinalInvoice> GetInvoicesByDateRange(DateTime startDate, DateTime endDate)
	{
		var results = _invoicesRepository.GetInvoicesByDateRange(startDate, endDate);

		return results.Select(x => new FinalInvoiceAdapter(x) as IFinalInvoice);
	}

	public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date)
	{
		var results = _invoicesRepository.GetInvoiceProductsByDate(date);

		return results.Select(x => new FinalInvoiceProductAdapter(x) as IFinalInvoiceProduct);
	}

	public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDateRange(DateTime startDate, DateTime endDate)
	{
		var results = _invoicesRepository.GetInvoiceProductsByDateRange(startDate, endDate);

		return results.Select(x => new FinalInvoiceProductAdapter(x) as IFinalInvoiceProduct);
	}

	public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId)
	{
		var results = _invoicesRepository.GetInvoiceProductsByInvoiceId(invoiceId);

		return results.Select(x => new FinalInvoiceProductAdapter(x) as IFinalInvoiceProduct);
	}

	public IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId)
	{
		var results = _invoicesRepository.GetPaymentsByInvoiceId(invoiceId);

		return results.Select(x => new FinalInvoicePaymentAdapter(x) as IFinalInvoicePayment);
	}
}