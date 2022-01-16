using AutoFixture;
using FluentAssertions;
using IndyPOS.Enums;
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
		public void AddProduct_ProductShouldBeAdded()
		{
			var product = _fixture.Create<InventoryProductModel>();

			_saleInvoice.AddProduct(product);

			_saleInvoice.Products.Should().HaveCount(1);
			_saleInvoice.InvoiceTotal.Should().Be(product.UnitPrice);
		}

		[Test]
		public void AddProduct_WithUnitPriceAndQuantity_ProductShouldBeAdded()
		{
			var product = _fixture.Create<InventoryProductModel>();
			var unitPrice = _fixture.Create<decimal>();
			var quantity = _fixture.Create<int>();

			_saleInvoice.AddProduct(product, unitPrice, quantity);

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
		public void AddPayment_PaymentShouldBeAdded()
		{
			var paymentAmount = _fixture.Create<decimal>();

			_saleInvoice.AddPayment(_fixture.Create<PaymentType>(), paymentAmount, _fixture.Create<string>());

			_saleInvoice.Payments.Should().HaveCount(1);
			_saleInvoice.PaymentTotal.Should().Be(paymentAmount);
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
