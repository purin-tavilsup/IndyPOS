using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Devices;
using IndyPOS.Enums;
using IndyPOS.Events;
using IndyPOS.Extensions;
using IndyPOS.Inventory;
using IndyPOS.Sales;
using IndyPOS.Users;
using System.Collections.Generic;
using System.Linq;
using IndyPOS.Controllers;
using NUnit.Framework;
using AutoFixture;
using FakeItEasy;
using FluentAssertions;
using Moq;
using Prism.Events;
using InventoryProductModel = IndyPOS.DataAccess.Models.InventoryProduct;

namespace IndyPOS.Tests.Controllers
{
	[TestFixture]
    public class SaleInvoiceControllerTests
    {
        private SaleInvoiceController _saleInvoiceController;
		private ISaleInvoice _saleInvoice;
		private IEventAggregator _eventAggregator;
        private IInvoiceRepository _invoicesRepository;
        private IInventoryProductRepository _inventoryProductsRepository;
        private IReceiptPrinter _receiptPrinter;
        private IUserAccountHelper _userAccountHelper;
        private IFixture _fixture;

		private const decimal ZeroMoneyValue = 0m;

        [SetUp]
        public void Setup()
		{
			InstantiateFakeObjects();

			_fixture = new Fixture();

			_fixture.Register<ISaleInvoiceProduct>(() => _fixture.Create<SaleInvoiceProduct>());

            _saleInvoiceController = new SaleInvoiceController(_saleInvoice,
                                                               _eventAggregator,
                                                               _invoicesRepository,
                                                               _inventoryProductsRepository,
                                                               _receiptPrinter,
                                                               _userAccountHelper);
        }

		private void InstantiateFakeObjects()
		{
			_saleInvoice = A.Fake<ISaleInvoice>();
			_eventAggregator = A.Fake<IEventAggregator>();
			_invoicesRepository = A.Fake<IInvoiceRepository>();
			_inventoryProductsRepository = A.Fake<IInventoryProductRepository>();
			_receiptPrinter = A.Fake<IReceiptPrinter>();
			_userAccountHelper = A.Fake<IUserAccountHelper>();
		}

        [Test]
        public void StartNewSale_NewInvoiceShouldBeCreated()
		{
			A.CallTo(() => _saleInvoice.Products).Returns(new List<ISaleInvoiceProduct>());
			A.CallTo(() => _saleInvoice.Payments).Returns(new List<IPayment>());

			_saleInvoiceController.StartNewSale();

			A.CallTo(() => _saleInvoice.StartNewSale()).MustHaveHappenedOnceExactly();

			_saleInvoiceController.Products.Should().BeEmpty();
            _saleInvoiceController.Payments.Should().BeEmpty();
            _saleInvoiceController.InvoiceTotal.Should().Be(ZeroMoneyValue);
            _saleInvoiceController.PaymentTotal.Should().Be(ZeroMoneyValue);
            _saleInvoiceController.BalanceRemaining.Should().Be(ZeroMoneyValue);
            _saleInvoiceController.IsRefundInvoice.Should().BeFalse();
            _saleInvoiceController.IsPendingPayment.Should().BeFalse();
            _saleInvoiceController.Changes.Should().Be(ZeroMoneyValue);
        }

		[Test]
		public void StartNewSale_NewSaleStartedEventShouldBePublished()
		{
			A.CallTo(() => _eventAggregator.GetEvent<NewSaleStartedEvent>()).Returns(A.Fake<NewSaleStartedEvent>());

			_saleInvoiceController.StartNewSale();

			A.CallTo(() => _eventAggregator.GetEvent<NewSaleStartedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void RemoveAllPayments_PaymentsShouldBeCleared()
        {
			A.CallTo(() => _saleInvoice.Products).Returns(new List<ISaleInvoiceProduct>());
			A.CallTo(() => _saleInvoice.Payments).Returns(new List<IPayment>());

			_saleInvoiceController.RemoveAllPayments();

			A.CallTo(() => _saleInvoice.RemoveAllPayments()).MustHaveHappenedOnceExactly();

			_saleInvoiceController.Payments.Should().BeEmpty();
			_saleInvoiceController.PaymentTotal.Should().Be(ZeroMoneyValue);
			_saleInvoiceController.Changes.Should().Be(ZeroMoneyValue);
		}

		[Test]
		public void RemoveAllPayments_AllPaymentsRemovedEventShouldBePublished()
		{
			A.CallTo(() => _eventAggregator.GetEvent<AllPaymentsRemovedEvent>()).Returns(A.Fake<AllPaymentsRemovedEvent>());

			_saleInvoiceController.RemoveAllPayments();

			A.CallTo(() => _eventAggregator.GetEvent<AllPaymentsRemovedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void AddProduct_ProductFound_ProductShouldBeAdded()
		{
			var product = A.Fake<InventoryProductModel>();

			A.CallTo(() => _inventoryProductsRepository.GetProductByBarcode(A<string>.Ignored)).Returns(product);

			_saleInvoiceController.AddProduct(_fixture.Create<string>());

			A.CallTo(() => _saleInvoice.AddProduct(product)).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void AddProduct_ProductFound_SaleInvoiceProductAddedEventShouldBePublished()
		{
			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>()).Returns(A.Fake<SaleInvoiceProductAddedEvent>());

			_saleInvoiceController.AddProduct(_fixture.Create<string>());

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		[TestCase(true, true)]
		[TestCase(false, false)]
		public void AddProduct_ShouldReturnExpectedResult(bool isProductFound, bool expectedResult)
		{
			var product = isProductFound ? _fixture.Create<InventoryProductModel>() : null;

			A.CallTo(() => _inventoryProductsRepository.GetProductByBarcode(A<string>.Ignored)).Returns(product);

            var result = _saleInvoiceController.AddProduct(_fixture.Create<string>());

            result.Should().Be(expectedResult);
        }

		[Test]
		public void RemoveProduct_ProductShouldBeRemoved()
		{
            var product = A.Fake<ISaleInvoiceProduct>();

            _saleInvoiceController.RemoveProduct(product);

			A.CallTo(() => _saleInvoice.RemoveProduct(product)).MustHaveHappenedOnceExactly();
        }

		[Test]
		public void RemoveProduct_SaleInvoiceProductRemovedEventShouldBePublished()
		{
			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>()).Returns(A.Fake<SaleInvoiceProductRemovedEvent>());
			
			_saleInvoiceController.RemoveProduct(_fixture.Create<ISaleInvoiceProduct>());

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void AddPayment_PaymentShouldBeAdded()
		{
			var type = _fixture.Create<PaymentType>();
			var amount = _fixture.Create<decimal>();
            var note = _fixture.Create<string>();

            _saleInvoiceController.AddPayment(type, amount, note);

			A.CallTo(() => _saleInvoice.AddPayment(type, amount, note)).MustHaveHappenedOnceExactly();
        }

		[Test]
		public void AddPayment_PaymentAddedEventShouldBePublished()
        {
			A.CallTo(() => _eventAggregator.GetEvent<PaymentAddedEvent>()).Returns(A.Fake<PaymentAddedEvent>());

			var type = _fixture.Create<PaymentType>();
			var amount = _fixture.Create<decimal>();
			var note = _fixture.Create<string>();

			_saleInvoiceController.AddPayment(type, amount, note);

			A.CallTo(() => _eventAggregator.GetEvent<PaymentAddedEvent>().Publish()).MustHaveHappenedOnceExactly();
        }

		[Test]
		public void UpdateProductUnitPrice_ProductFound_UnitPriceShouldBeUpdated()
		{
			var productId = _fixture.Create<int>();
			var priority = _fixture.Create<int>();
			var unitPrice = _fixture.Create<decimal>();
			var note = _fixture.Create<string>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.InventoryProductId = productId;
			product.Priority = priority;
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductUnitPrice(productId, priority, unitPrice, note);

			product.UnitPrice.Should().Be(unitPrice);
			product.Note.Should().Be(note);
		}

		[Test]
		public void UpdateProductUnitPrice_ProductNotFound_SaleInvoiceProductUpdatedEventShouldNotBePublished()
		{
			var productId = _fixture.Create<int>();
			var priority = _fixture.Create<int>();
			var unitPrice = _fixture.Create<decimal>();
			var note = _fixture.Create<string>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductUnitPrice(productId, priority, unitPrice, note);

			product.UnitPrice.Should().NotBe(unitPrice);
			product.Note.Should().NotBe(note);
		}

		[Test]
		public void UpdateProductUnitPrice_SameUnitPrice_UnitPriceShouldNotBeUpdated()
		{
			var productId = _fixture.Create<int>();
			var priority = _fixture.Create<int>();
			var unitPrice = _fixture.Create<decimal>();
			var note = _fixture.Create<string>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			
			product.InventoryProductId = productId;
			product.Priority = priority;
			product.UnitPrice = unitPrice;

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>()).Returns(A.Fake<SaleInvoiceProductUpdatedEvent>());
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductUnitPrice(productId, priority, unitPrice, note);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustNotHaveHappened();
		}

		[Test]
		public void UpdateProductUnitPrice_ProductFound_SaleInvoiceProductUpdatedEventShouldBePublished()
		{
			var productId = _fixture.Create<int>();
			var priority = _fixture.Create<int>();
			var unitPrice = _fixture.Create<decimal>();
			var note = _fixture.Create<string>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.InventoryProductId = productId;
			product.Priority = priority;
			
			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>()).Returns(A.Fake<SaleInvoiceProductUpdatedEvent>());
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductUnitPrice(productId, priority, unitPrice, note);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}
    }
}
