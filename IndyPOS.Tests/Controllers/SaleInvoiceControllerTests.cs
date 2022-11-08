using AutoFixture.Xunit2;
using FluentAssertions;
using IndyPOS.Common.Enums;
using IndyPOS.Controllers;
using IndyPOS.Facade.Exceptions;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Mock.Attributes;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace IndyPOS.Tests.Controllers
{
	public class SaleInvoiceControllerTests
    {
		private const decimal ZeroMoneyValue = 0m;

		[Theory]
		[AutoMoqData]
		public void StartNewSale_NewInvoiceShouldBeCreated(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper, 
			SaleInvoiceController sut)
		{
			// Arrange
			saleInvoiceHelper.Setup(s => s.Products)
							 .Returns(new List<ISaleInvoiceProduct>());

			saleInvoiceHelper.Setup(s => s.Payments)
							 .Returns(new List<IPayment>());

			// Act
			sut.StartNewSale();

			// Assert
			sut.CalculateInvoiceTotal().Should().Be(ZeroMoneyValue);
			sut.CalculatePaymentTotal().Should().Be(ZeroMoneyValue);
			sut.CalculateBalanceRemaining().Should().Be(ZeroMoneyValue);
			sut.IsRefundInvoice().Should().BeFalse();
			sut.IsPendingPayment().Should().BeFalse();
			sut.CalculateChanges().Should().Be(ZeroMoneyValue);

			saleInvoiceHelper.Verify(s => s.StartNewSale(), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void RemoveAllPayments_PaymentsShouldBeCleared(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper, 
			SaleInvoiceController sut)
		{
			// Act
			sut.RemoveAllPayments();

			// Assert
			sut.CalculatePaymentTotal().Should().Be(ZeroMoneyValue);
			sut.CalculateChanges().Should().Be(ZeroMoneyValue);

			saleInvoiceHelper.Verify(s => s.RemoveAllPayments(), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void AddProduct_WithDefaultSettings_ProductShouldBeAdded(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper, 
			SaleInvoiceController sut,
			IInventoryProduct product)
		{
			// Act
			sut.AddProduct(product);

			// Assert
			saleInvoiceHelper.Verify(s => s.AddProduct(product), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void AddProduct_WithSpecification_ProductShouldBeAddedWithSpecifiedValues(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut,
			IInventoryProduct product,
			decimal unitPrice,
			int quantity,
			string note)
		{
			// Act
			sut.AddProduct(product, unitPrice, quantity, note);

			// Assert
			saleInvoiceHelper.Verify(s => s.AddProduct(product, unitPrice, quantity, note), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void GetInventoryProductByBarcode_ProductFound_ShouldReturnProduct(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut,
			IInventoryProduct product)
		{
			// Arrange
			saleInvoiceHelper.Setup(s => s.GetInventoryProductByBarcode(product.Barcode))
							 .Returns(product);

			// Act
			var result = sut.GetInventoryProductByBarcode(product.Barcode);

			// Assert
			result.Should().Be(product);
		}

		[Theory]
		[AutoMoqData]
		public void GetInventoryProductByBarcode_ProductNotFound_ShouldNotReturnProduct(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut,
			string barcode)
		{
			// Arrange
			saleInvoiceHelper.Setup(s => s.GetInventoryProductByBarcode(barcode))
							 .Returns((IInventoryProduct) null);

			// Act
			var result = sut.GetInventoryProductByBarcode(barcode);

			// Assert
			result.Should().BeNull();
		}

		[Theory]
		[AutoMoqData]
		public void RemoveProduct_ProductShouldBeRemoved(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut,
			ISaleInvoiceProduct product)
		{
			// Act
			sut.RemoveProduct(product);

			// Assert
			saleInvoiceHelper.Verify(s => s.RemoveProduct(product), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void AddPayment_PaymentShouldBeAdded(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut,
			PaymentType type,
			decimal amount,
			string note)
		{
			// Act
			sut.AddPayment(type, amount, note);

			// Assert
			saleInvoiceHelper.Verify(s => s.AddPayment(type, amount, note), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void UpdateProductUnitPrice_ProductFound_UnitPriceShouldBeUpdated(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut,
			int inventoryProductId,
			int priority,
			decimal unitPrice,
			string note)
		{
			// Act
			sut.UpdateProductUnitPrice(inventoryProductId, priority, unitPrice, note);

			// Assert
			saleInvoiceHelper.Verify(s => s.UpdateProductUnitPrice(inventoryProductId, priority, unitPrice, note), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void UpdateProductUnitPrice_ProductNotFound_ShouldThrowProductNotFoundException(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut,
			int inventoryProductId,
			int priority,
			decimal unitPrice,
			string note)
		{
			// Arrange
			saleInvoiceHelper.Setup(s => s.UpdateProductUnitPrice(inventoryProductId, priority, unitPrice, note))
							 .Throws(new ProductNotFoundException(""));

			// Act
			Action act = () => sut.UpdateProductUnitPrice(inventoryProductId, priority, unitPrice, note);

			// Assert
			act.Should().ThrowExactly<ProductNotFoundException>();
		}

		[Theory]
		[AutoMoqData]
		public void UpdateProductQuantity_ProductFound_ProductQuantityShouldBeUpdated(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut,
			int inventoryProductId,
			int priority,
			int quantity)
		{
			// Act
			sut.UpdateProductQuantity(inventoryProductId, priority, quantity);

			// Assert
			saleInvoiceHelper.Verify(s => s.UpdateProductQuantity(inventoryProductId, priority, quantity), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void UpdateProductQuantity_ProductNotFound_ShouldThrowProductNotFoundException(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut,
			int inventoryProductId,
			int priority,
			int quantity)
		{
			// Arrange
			saleInvoiceHelper.Setup(s => s.UpdateProductQuantity(inventoryProductId, priority, quantity))
							 .Throws(new ProductNotFoundException(""));

			// Act
			Action act = () => sut.UpdateProductQuantity(inventoryProductId, priority, quantity);

			// Assert 
			act.Should().ThrowExactly<ProductNotFoundException>();
		}

		[Theory]
		[AutoMoqData]
		public void ValidateSaleInvoice_SaleInvoiceIsInvalid_ShouldReturnErrorMessage(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut,
			string errorMessage)
		{
			// Arrange
			saleInvoiceHelper.Setup(s => s.ValidateSaleInvoice())
							 .Returns(new List<string> {errorMessage});

			// Act
			var errorMessages = sut.ValidateSaleInvoice();

			// Assert
			errorMessages.Should().NotBeEmpty();
		}

		[Theory]
		[AutoMoqData]
		public void ValidateSaleInvoice_SaleInvoiceIsValid_ShouldNotReturnErrorMessage(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut)
		{
			// Arrange
			saleInvoiceHelper.Setup(s => s.ValidateSaleInvoice())
							 .Returns(new List<string>());

			// Act
			var errorMessages = sut.ValidateSaleInvoice();

			// Assert
			errorMessages.Should().BeEmpty();
		}

		[Theory]
		[AutoMoqData]
		public async Task CompleteSale_SaleInvoiceShouldBeSavedToDatabase(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut)
		{
			// Act
			await sut.CompleteSale();

			// Assert
			saleInvoiceHelper.Verify(s => s.CompleteSale(), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void PrintReceipt_ShouldPrintReceipt(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut)
		{
			// Act
			sut.PrintReceipt();

			// Assert
			saleInvoiceHelper.Verify(s => s.PrintReceipt(), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void GetInvoiceInfo_ShouldInvokeGetInvoiceInfo(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			SaleInvoiceController sut)
		{
			// Act
			sut.GetInvoiceInfo();

			// Assert
			saleInvoiceHelper.Verify(s => s.GetInvoiceInfo(), Times.Once);
		}

		[Theory]
		[AutoMoqData]
		public void GetInvoiceInfo_ShouldReturnInvoiceInfo(SaleInvoiceController sut)
		{
			// Act
			var result = sut.GetInvoiceInfo();

			// Assert
			result.Should().NotBeNull();
		}
		
		[Theory]
		[AutoMoqData]
		public void GetSaleInvoiceProduct_ShouldReturnSaleInvoiceProduct(
			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
			string barcode,
			int priority,
			ISaleInvoiceProduct product,
			SaleInvoiceController sut)
		{
			//Arrange
			saleInvoiceHelper.Setup(s => s.GetSaleInvoiceProduct(barcode, priority))
							 .Returns(product);

			// Act
			var result = sut.GetSaleInvoiceProduct(barcode, priority);

			// Assert
			result.Should().NotBeNull();
		}
	}
}
