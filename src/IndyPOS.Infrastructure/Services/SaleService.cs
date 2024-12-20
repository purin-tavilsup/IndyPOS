﻿using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Events;
using IndyPOS.Application.Notifications;
using IndyPOS.Application.UseCases.InventoryProducts;
using IndyPOS.Application.UseCases.InventoryProducts.Get;
using IndyPOS.Application.UseCases.InventoryProducts.Update;
using IndyPOS.Application.UseCases.InvoicePayments.Create;
using IndyPOS.Application.UseCases.InvoiceProducts.Create;
using IndyPOS.Application.UseCases.Invoices;
using IndyPOS.Application.UseCases.Invoices.Create;
using IndyPOS.Application.UseCases.PayLaterPayments.Create;
using IndyPOS.Domain.Events;
using MediatR;
using Throw;

namespace IndyPOS.Infrastructure.Services;

public class SaleService : ISaleService
{
	private readonly IMediator _mediator;
	private readonly IEventAggregator _eventAggregator;
	private ILoggedInUser? _loggedInUser;

	public IList<Product> Products { get; private set; } = new List<Product>();

	public IList<Payment> Payments { get; private set; } = new List<Payment>();

	public SaleService(IMediator mediator,
					   IEventAggregator eventAggregator)
	{
		_mediator = mediator;
		_eventAggregator = eventAggregator;

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

	private void AddProductInternal(InventoryProductDto product, decimal unitPrice, int quantity, string note)
	{
		var productToAdd = ConvertToInvoiceProduct(product);

		productToAdd.Priority = GetNextProductPriority();
		productToAdd.UnitPrice = unitPrice;
		productToAdd.Quantity = quantity;
		productToAdd.Note = note;

		Products.Add(productToAdd);
	}

	private void AddProductInternal(InventoryProductDto product)
	{
		AddProductInternal(product, product.UnitPrice, 1, string.Empty);
	}

	private static Product ConvertToInvoiceProduct(InventoryProductDto product)
	{
		return new Product
		{
			InventoryProductId = product.InventoryProductId,
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
		return Products.Sum(p => !p.IsGroupProduct ? p.UnitPrice * p.Quantity : p.GroupPrice);
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

	public void AddProduct(InventoryProductDto product)
	{
		AddProductInternal(product);

		_eventAggregator.GetEvent<InvoiceProductAddedEvent>().Publish();
	}

	public void AddProduct(InventoryProductDto product, decimal unitPrice, int quantity, string note)
	{
		AddProductInternal(product, unitPrice, quantity, note);

		_eventAggregator.GetEvent<InvoiceProductAddedEvent>().Publish();
	}

	public Product GetSaleInvoiceProduct(string barcode, int priority)
	{
		var result = Products.FirstOrDefault(p => p.Barcode  == barcode && 
												  p.Priority == priority);

		result.ThrowIfNull(() =>
			throw new ProductNotFoundException($"Sale Invoice Product is not found. Barcode: {barcode}. Priority: {priority}."));

		return result;
	}

	public void RemoveProduct(Product product)
	{
		Products.Remove(product);

		_eventAggregator.GetEvent<InvoiceProductRemovedEvent>().Publish();
	}

	public async Task<InventoryProductDto> GetInventoryProductByBarcodeAsync(string barcode)
	{
		var result = await _mediator.Send(new GetInventoryProductByBarcodeQuery(barcode));

		return result;
	}

	private async Task<InventoryProductDto> GetInventoryProductByIdAsync(int id)
	{
		var result = await _mediator.Send(new GetInventoryProductByIdQuery(id));

		return result;
	}

	private async Task UpdateInventoryProductQuantityAsync(int id, int quantity)
	{
		var command = new UpdateInventoryProductQuantityCommand
		{
			Id = id,
			Quantity = quantity
		};

		await _mediator.Send(command);
	}

	public void AddPayment(PaymentType paymentType, decimal paymentAmount, string note)
	{
		AddPaymentInternal(paymentType, paymentAmount, note);

		_eventAggregator.GetEvent<InvoicePaymentAddedEvent>().Publish();
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
		{
			var message = $"Sale Invoice Product is not found. InventoryProductId: {inventoryProductId}. Priority: {priority}.";

			throw new ProductNotFoundException(message);
		}
		
		if (productToUpdate.UnitPrice == unitPrice) { return; }

		productToUpdate.UnitPrice = unitPrice;
		productToUpdate.Note = note;

		_eventAggregator.GetEvent<InvoiceProductUpdatedEvent>().Publish();
	}

	public async Task UpdateProductQuantityAsync(int inventoryProductId, int priority, int newQuantity)
	{
		var productToUpdate = Products.FirstOrDefault(p => p.InventoryProductId == inventoryProductId &&
														   p.Priority == priority);

		if (productToUpdate == null)
		{
			var message = $"Sale Invoice Product is not found. InventoryProductId: {inventoryProductId}. Priority: {priority}.";
			throw new ProductNotFoundException(message);
		}

		if (productToUpdate.Quantity == newQuantity)
		{
			return;
		}
		
		if (!productToUpdate.HasGroupPrice())
		{
			productToUpdate.Quantity = newQuantity;
			_eventAggregator.GetEvent<InvoiceProductUpdatedEvent>().Publish();
			return;
		}

		if (newQuantity > productToUpdate.Quantity)
		{
			await IncreaseQuantityOfGroupProductAsync(productToUpdate, newQuantity);
		}
		else
		{
			DecreaseQuantityOfGroupProduct(productToUpdate, newQuantity);
		}
	}

	private static bool IsEligibleForGroupPrice(Product product, int newQuantity)
	{
		if (product.GroupPriceQuantity is null)
		{
			return false;
		}

		return newQuantity >= product.GroupPriceQuantity;
	}
	
	private async Task IncreaseQuantityOfGroupProductAsync(Product product, int newQuantity)
	{
		var quantityToAdd = newQuantity;
		var groupPriceQuantity = product.GroupPriceQuantity.GetValueOrDefault();
		var inventoryProduct = await GetInventoryProductByIdAsync(product.InventoryProductId);

		while (IsEligibleForGroupPrice(product, quantityToAdd))
		{
			AddProductWithGroupPrice(inventoryProduct);
			
			quantityToAdd -= groupPriceQuantity;
		}
		
		if (quantityToAdd > 0)
		{
			product.Quantity = quantityToAdd;
		}
		else
		{
			Products.Remove(product);
		}
		
		_eventAggregator.GetEvent<InvoiceProductUpdatedEvent>().Publish();
	}

	private void AddProductWithGroupPrice(InventoryProductDto inventoryProduct)
	{
		var productToAdd = ConvertToInvoiceProduct(inventoryProduct);
		var groupPriceQuantity = inventoryProduct.GroupPriceQuantity.GetValueOrDefault();

		productToAdd.Priority = GetNextProductPriority();
		productToAdd.Quantity = groupPriceQuantity;
		productToAdd.IsGroupProduct = true;
		productToAdd.Note = "ราคากลุ่ม";

		Products.Add(productToAdd);
	}

	private void DecreaseQuantityOfGroupProduct(Product product, int newQuantity)
	{
		product.Quantity = newQuantity;
		product.IsGroupProduct = false;
		product.Note = string.Empty;

		_eventAggregator.GetEvent<InvoiceProductUpdatedEvent>().Publish();
	}

	public IList<string> ValidateSaleInvoice()
	{
		var message = new List<string>();

		if (_loggedInUser is null)
			message.Add("ไม่พบผู้ใช้งานในระบบ");

		if (!Products.Any()) 
			message.Add("ไม่มีรายการสินค้า");

		if (!Payments.Any()) 
			message.Add("ไม่มีรายการเงิน");

		if (IsPendingPayment())
			message.Add("รายการเงินยังไม่สมบูรณ์");

		return message;
	}

	public async Task<IInvoiceInfo> CompleteSaleAsync()
	{
		_loggedInUser.ThrowIfNull(() => throw new UserNotLoggedInException("User has not logged in."));

		var loggedInUserId = _loggedInUser.UserId;
		var invoiceTotal = CalculateInvoiceTotal();

		var invoiceId = await AddInvoiceToDatabaseAsync(invoiceTotal, loggedInUserId);

		var invoiceInfo = CreateInvoiceInfo(invoiceId, invoiceTotal);

		// Refactor: Moved these to event (notification) handlers 
		await AddInvoiceProductsToDatabaseAsync(invoiceInfo);
		await AddPaymentsToDatabaseAsync(invoiceInfo);
		await UpdateInventoryProductsSoldOnInvoice(invoiceInfo);
		//---------------------------------------------------------
		
		await PublishSalesCompletedEventAsync(invoiceId, invoiceInfo.HasPayLaterPayment);

		return invoiceInfo;
	}

	private IInvoiceInfo CreateInvoiceInfo(int invoiceId, decimal invoiceTotal)
	{
		var paymentTotal = CalculatePaymentTotal();
		var isRefundInvoice = invoiceTotal < 0;

		return new InvoiceInfo
		{
			Id = invoiceId,
			Products = Products,
			Payments = Payments,
			InvoiceTotal = invoiceTotal,
			PaymentTotal = paymentTotal,
			Changes = CalculateChanges(),
			IsRefundInvoice = isRefundInvoice,
			HasPayLaterPayment = Payments.HasPayLayerPayment()
		};
	}

	private async Task UpdateInventoryProductsSoldOnInvoice(IInvoiceInfo invoiceInfo)
	{
		var productGroups = invoiceInfo.Products
									   .Where(p => p.IsTrackable)
									   .GroupBy(p => p.InventoryProductId);

		foreach (var group in productGroups)
		{
			var id = group.Key;
			var quantity = group.Sum(p => p.Quantity);
			var product = await GetInventoryProductByIdAsync(id);
			var newQuantity = product.QuantityInStock - quantity;

			await UpdateInventoryProductQuantityAsync(id, newQuantity);
        }
    }

	private async Task<int> AddInvoiceToDatabaseAsync(decimal invoiceTotal, int userId)
	{
		var command = new CreateInvoiceCommand
		{
			Total = invoiceTotal,
			UserId = userId,
			CustomerId = null
		};

		var invoiceId = await _mediator.Send(command);

		return invoiceId;
	}

	private async Task AddInvoiceProductsToDatabaseAsync(IInvoiceInfo invoiceInfo)
	{
		foreach(var product in invoiceInfo.Products)
		{
			await AddInvoiceProductToDatabaseAsync(product, invoiceInfo.Id);
        }
    }

	private async Task AddInvoiceProductToDatabaseAsync(Product product, int invoiceId)
	{
		var command = new CreateInvoiceProductCommand
		{
			Priority = product.Priority,
			InvoiceId = invoiceId,
			InventoryProductId = product.InventoryProductId,
			Barcode = product.Barcode,
			Description = product.Description,
			Manufacturer = product.Manufacturer,
			Brand = product.Brand,
			Category = product.Category,
			UnitPrice = product.UnitPrice,
			Quantity = product.Quantity,
			Note = product.Note,
			GroupPrice = product.GroupPrice,
			IsGroupProduct = product.IsGroupProduct
		};

		await _mediator.Send(command);
	}

	private async Task AddPaymentsToDatabaseAsync(IInvoiceInfo invoiceInfo)
	{
		foreach(var payment in invoiceInfo.Payments)
		{
			var invoiceId = invoiceInfo.Id;
			var paymentId = await AddPaymentToDatabaseAsync(payment, invoiceId);

			if (payment.PaymentTypeId == (int) PaymentType.PayLater)
			{
				await AddPayLaterPaymentToDatabaseAsync(payment, paymentId, invoiceId);
            }
        }
    }

	private async Task<int> AddPaymentToDatabaseAsync(Payment payment, int invoiceId)
	{
		var command = new CreateInvoicePaymentCommand
		{
			PaymentTypeId = payment.PaymentTypeId,
			InvoiceId = invoiceId,
			Amount = payment.Amount,
			Note = payment.Note
		};

		return await _mediator.Send(command);
	}

	private async Task AddPayLaterPaymentToDatabaseAsync(Payment payment, int paymentId, int invoiceId)
	{
		var command = new CreatePayLaterPaymentCommand
		{
			InvoiceId = invoiceId,
			PaymentId = paymentId,
			Description = payment.Note,
			ReceivableAmount = payment.Amount
		};

		await _mediator.Send(command);
	}
	
	private async Task PublishSalesCompletedEventAsync(int invoiceId, bool hasPayLaterPayment)
	{
		await _mediator.Publish(new SalesCompletedEvent(invoiceId, hasPayLaterPayment));
	}
}