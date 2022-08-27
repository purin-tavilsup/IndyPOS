using AutoFixture;
using FluentAssertions;
using IndyPOS.Common.Enums;
using IndyPOS.Sales;
using NUnit.Framework;
using System.Linq;
using InventoryProductModel = IndyPOS.DataAccess.Models.InventoryProduct;

namespace IndyPOS.Tests.Sales
{
	[TestFixture]
    public class SaleInvoiceTests
	{
		private SaleInvoice _saleInvoice;
		private IFixture _fixture;

		private const decimal ZeroMoneyValue = 0m;

		[SetUp]
        public void Setup()
        {
			_fixture = new Fixture();
			_saleInvoice = new SaleInvoice();
		}

		[Test]
        public void StartNewSale_InvoiceShouldBeReset()
        {
			_saleInvoice.StartNewSale();

			_saleInvoice.Id.Should().BeNull();
			_saleInvoice.Products.Should().BeEmpty();
			_saleInvoice.Payments.Should().BeEmpty();
			_saleInvoice.InvoiceTotal.Should().Be(ZeroMoneyValue);
			_saleInvoice.PaymentTotal.Should().Be(ZeroMoneyValue);
			_saleInvoice.BalanceRemaining.Should().Be(ZeroMoneyValue);
			_saleInvoice.Changes.Should().Be(ZeroMoneyValue);
			_saleInvoice.IsRefundInvoice.Should().BeFalse();
		}

		[Test]
		public void AddProduct_IsRefundProduct_ShouldBeRefundInvoice()
		{
			var product = _fixture.Create<InventoryProductModel>();
			const int refundQuantity = -1;
			var refundAmount = product.UnitPrice * refundQuantity;

			var addedProduct = _saleInvoice.AddProduct(product);

			addedProduct.Quantity = refundQuantity;

			addedProduct.InventoryProductId.Should().Be(product.InventoryProductId);
			addedProduct.UnitPrice.Should().Be(product.UnitPrice);
			_saleInvoice.InvoiceTotal.Should().Be(refundAmount);
			_saleInvoice.IsRefundInvoice.Should().BeTrue();
			_saleInvoice.Changes.Should().Be(ZeroMoneyValue);
		}

		[Test]
		public void AddProduct_ProductShouldBeAdded()
		{
			var product = _fixture.Create<InventoryProductModel>();

			var addedProduct = _saleInvoice.AddProduct(product);

			addedProduct.InventoryProductId.Should().Be(product.InventoryProductId);
			addedProduct.UnitPrice.Should().Be(product.UnitPrice);
			addedProduct.Quantity.Should().Be(1);

			_saleInvoice.Products.Should().HaveCount(1);
			_saleInvoice.InvoiceTotal.Should().Be(product.UnitPrice);
		}

		[Test]
		[TestCase(10, 1, 1, 10)]
		[TestCase(20, 2, 2, 80)]
		[TestCase(30, 3, 3, 270)]
		public void AddProduct_WithUnitPriceAndQuantity_ProductShouldBeAddedWithSpecifiedValues(decimal unitPrice, int quantity, int numberOfProducts, decimal expectedInvoiceTotal)
		{
			var product = _fixture.Create<InventoryProductModel>();

			for (var i = 0; i < numberOfProducts; i++)
            {
				var addedProduct = _saleInvoice.AddProduct(product, unitPrice, quantity);

				addedProduct.Priority.Should().Be(i + 1);
				addedProduct.InventoryProductId.Should().Be(product.InventoryProductId);
				addedProduct.UnitPrice.Should().Be(unitPrice);
				addedProduct.Quantity.Should().Be(quantity);
            }

			_saleInvoice.Products.Should().HaveCount(numberOfProducts);
			_saleInvoice.InvoiceTotal.Should().Be(expectedInvoiceTotal);
		}

		[Test]
		public void AddProduct_WithUnitPriceAndQuantityAndNote_ProductShouldBeAddedWithSpecifiedValues()
		{
			var product = _fixture.Create<InventoryProductModel>();
			var unitPrice = _fixture.Create<decimal>();
			var quantity = _fixture.Create<int>();
			var note = _fixture.Create<string>();

			var addedProduct = _saleInvoice.AddProduct(product, unitPrice, quantity, note);

			addedProduct.InventoryProductId.Should().Be(product.InventoryProductId);
			addedProduct.UnitPrice.Should().Be(unitPrice);
			addedProduct.Quantity.Should().Be(quantity);
			addedProduct.Note.Should().Be(note);

			_saleInvoice.Products.Should().HaveCount(1);
			_saleInvoice.InvoiceTotal.Should().Be(unitPrice * quantity);
		}

		[Test]
		public void RemoveProduct_ProductShouldBeRemoved()
        {
			_saleInvoice.AddProduct(_fixture.Create<InventoryProductModel>());

			var productToRemove = _saleInvoice.Products.FirstOrDefault();

			_saleInvoice.RemoveProduct(productToRemove);

			_saleInvoice.Products.Should().BeEmpty();
			_saleInvoice.InvoiceTotal.Should().Be(ZeroMoneyValue);
		}

		[Test]
		[TestCase(1.25, 1, 1.25)]
		[TestCase(10, 1, 10)]
		[TestCase(20, 2, 40)]
		[TestCase(30, 3, 90)]
		[TestCase(100, 4, 400)]
		[TestCase(1000, 5, 5000)]
		public void AddPayment_PaymentShouldBeAdded(decimal paymentAmount, int numberOfPayments, decimal expectedPaymentTotal)
		{
			for (var i = 0; i < numberOfPayments; i++)
            {
				var paymentType = _fixture.Create<PaymentType>();
				var note = _fixture.Create<string>();

				var addedPayment = _saleInvoice.AddPayment(paymentType, paymentAmount, note);

				addedPayment.Priority.Should().Be(i + 1);
				addedPayment.PaymentTypeId.Should().Be((int) paymentType);
				addedPayment.Amount.Should().Be(paymentAmount);
				addedPayment.Note.Should().Be(note);
            }

			_saleInvoice.Payments.Should().HaveCount(numberOfPayments);
			_saleInvoice.PaymentTotal.Should().Be(expectedPaymentTotal);
		}

		[Test]
		public void RemoveAllPayments_AllPaymentsShouldBeRemoved()
        {
			_saleInvoice.AddPayment(_fixture.Create<PaymentType>(), _fixture.Create<decimal>(), _fixture.Create<string>());

			_saleInvoice.RemoveAllPayments();

			_saleInvoice.Payments.Should().BeEmpty();
			_saleInvoice.PaymentTotal.Should().Be(ZeroMoneyValue);
		}

		[Test]
		public void SetSaleInvoiceId_IdShouldBeSet()
		{
			var id = _fixture.Create<int>();

			_saleInvoice.SetSaleInvoiceId(id);

			_saleInvoice.Id.Should().Be(id);
		}
	}
}
