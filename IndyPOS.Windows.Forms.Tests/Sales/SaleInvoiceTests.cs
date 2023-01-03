using FluentAssertions;
using IndyPOS.Common.Enums;
using IndyPOS.Mock.Attributes;
using System.Linq;
using Xunit;

namespace IndyPOS.Tests.Sales;

public class SaleInvoiceTests
{
	private const decimal ZeroMoneyValue = 0m;
		
	//[Theory]
	//[AutoMoqData]
	//      public void StartNewSale_InvoiceShouldBeReset(OldSaleInvoice sut)
	//      {
	//	sut.StartNewSale();

	//	sut.Id.Should().BeNull();
	//	sut.Products.Should().BeEmpty();
	//	sut.Payments.Should().BeEmpty();
	//	sut.InvoiceTotal.Should().Be(ZeroMoneyValue);
	//	sut.PaymentTotal.Should().Be(ZeroMoneyValue);
	//	sut.BalanceRemaining.Should().Be(ZeroMoneyValue);
	//	sut.Changes.Should().Be(ZeroMoneyValue);
	//	sut.IsRefundInvoice.Should().BeFalse();
	//}
		
	//[Theory]
	//[AutoMoqData]
	//public void AddProduct_IsRefundProduct_ShouldBeRefundInvoice(InventoryProductModel product, OldSaleInvoice sut)
	//{
	//	const int refundQuantity = -1;
	//	var refundAmount = product.UnitPrice * refundQuantity;

	//	var addedProduct = sut.AddProduct(product);

	//	addedProduct.Quantity = refundQuantity;

	//	addedProduct.InventoryProductId.Should().Be(product.InventoryProductId);
	//	addedProduct.UnitPrice.Should().Be(product.UnitPrice);
	//	sut.InvoiceTotal.Should().Be(refundAmount);
	//	sut.IsRefundInvoice.Should().BeTrue();
	//	sut.Changes.Should().Be(ZeroMoneyValue);
	//}

	//[Theory]
	//[AutoMoqData]
	//public void AddProduct_ProductShouldBeAdded(InventoryProductModel product, OldSaleInvoice sut)
	//{
	//	var addedProduct = sut.AddProduct(product);

	//	addedProduct.InventoryProductId.Should().Be(product.InventoryProductId);
	//	addedProduct.UnitPrice.Should().Be(product.UnitPrice);
	//	addedProduct.Quantity.Should().Be(1);

	//	sut.Products.Should().HaveCount(1);
	//	sut.InvoiceTotal.Should().Be(product.UnitPrice);
	//}

	//[Theory]
	//[InlineAutoMoqData(10, 1, 1, 10)]
	//[InlineAutoMoqData(20, 2, 2, 80)]
	//[InlineAutoMoqData(30, 3, 3, 270)]
	//public void AddProduct_WithUnitPriceAndQuantity_ProductShouldBeAddedWithSpecifiedValues(
	//	decimal unitPrice, 
	//	int quantity, 
	//	int numberOfProducts, 
	//	decimal expectedInvoiceTotal,
	//	InventoryProductModel product,
	//	OldSaleInvoice sut)
	//{
	//	for (var i = 0; i < numberOfProducts; i++)
	//	{
	//		var addedProduct = sut.AddProduct(product, unitPrice, quantity);

	//		addedProduct.Priority.Should().Be(i + 1);
	//		addedProduct.InventoryProductId.Should().Be(product.InventoryProductId);
	//		addedProduct.UnitPrice.Should().Be(unitPrice);
	//		addedProduct.Quantity.Should().Be(quantity);
	//	}

	//	sut.Products.Should().HaveCount(numberOfProducts);
	//	sut.InvoiceTotal.Should().Be(expectedInvoiceTotal);
	//}

	//[Theory]
	//[AutoMoqData]
	//public void AddProduct_WithUnitPriceAndQuantityAndNote_ProductShouldBeAddedWithSpecifiedValues(
	//	InventoryProductModel product, 
	//	decimal unitPrice,
	//	int quantity,
	//	string note,
	//	OldSaleInvoice sut)
	//{
	//	// Act
	//	var addedProduct = sut.AddProduct(product, unitPrice, quantity, note);

	//	// Assert
	//	addedProduct.InventoryProductId.Should().Be(product.InventoryProductId);
	//	addedProduct.UnitPrice.Should().Be(unitPrice);
	//	addedProduct.Quantity.Should().Be(quantity);
	//	addedProduct.Note.Should().Be(note);

	//	sut.Products.Should().HaveCount(1);
	//	sut.InvoiceTotal.Should().Be(unitPrice * quantity);
	//}

	//[Theory]
	//[AutoMoqData]
	//public void RemoveProduct_ProductShouldBeRemoved(InventoryProductModel product, OldSaleInvoice sut)
	//{
	//	// Arrange
	//	sut.AddProduct(product);
	//	var productToRemove = sut.Products.FirstOrDefault();

	//	// Act
	//	sut.RemoveProduct(productToRemove);

	//	// Assert
	//	sut.Products.Should().BeEmpty();
	//	sut.InvoiceTotal.Should().Be(ZeroMoneyValue);
	//}

	//[Theory]
	//[InlineAutoMoqData(1.25, 1, 1.25)]
	//[InlineAutoMoqData(10, 1, 10)]
	//[InlineAutoMoqData(20, 2, 40)]
	//[InlineAutoMoqData(30, 3, 90)]
	//[InlineAutoMoqData(100, 4, 400)]
	//[InlineAutoMoqData(1000, 5, 5000)]
	//public void AddPayment_PaymentShouldBeAdded(
	//	decimal paymentAmount, 
	//	int numberOfPayments, 
	//	decimal expectedPaymentTotal,
	//	PaymentType paymentType,
	//	string note,
	//	OldSaleInvoice sut)
	//{
	//	// Act
	//	for (var i = 0; i < numberOfPayments; i++)
	//	{
	//		var addedPayment = sut.AddPayment(paymentType, paymentAmount, note);

	//		addedPayment.Priority.Should().Be(i + 1);
	//		addedPayment.PaymentTypeId.Should().Be((int)paymentType);
	//		addedPayment.Amount.Should().Be(paymentAmount);
	//		addedPayment.Note.Should().Be(note);
	//	}

	//	// Assert
	//	sut.Payments.Should().HaveCount(numberOfPayments);
	//	sut.PaymentTotal.Should().Be(expectedPaymentTotal);
	//}

	//[Theory]
	//[AutoMoqData]
	//public void RemoveAllPayments_AllPaymentsShouldBeRemoved(
	//	OldSaleInvoice sut, 
	//	PaymentType paymentType,
	//	decimal paymentAmount,
	//	string note)
	//{
	//	// Arrange
	//	sut.AddPayment(paymentType, paymentAmount, note);

	//	// Act
	//	sut.RemoveAllPayments();

	//	// Assert
	//	sut.Payments.Should().BeEmpty();
	//	sut.PaymentTotal.Should().Be(ZeroMoneyValue);
	//}

	//[Theory]
	//[AutoMoqData]
	//public void SetSaleInvoiceId_IdShouldBeSet(int id, OldSaleInvoice sut)
	//{
	//	// Act
	//	sut.SetSaleInvoiceId(id);

	//	// Assert
	//	sut.Id.Should().Be(id);
	//}
}