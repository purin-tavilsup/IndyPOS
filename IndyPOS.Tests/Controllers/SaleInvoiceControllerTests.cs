using System;
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
using IndyPOS.Controllers;
using NUnit.Framework;
using AutoFixture;
using FakeItEasy;
using FluentAssertions;
using IndyPOS.Exceptions;
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
		private int _inventoryProductId;
		private int _productPriority;

		private const decimal ZeroMoneyValue = 0m;

        [SetUp]
        public void Setup()
		{
			InstantiateFakeObjects();
			ArrangeFakeEventObjects();

			_fixture = new Fixture();
			_fixture.Register<ISaleInvoiceProduct>(() => _fixture.Create<SaleInvoiceProduct>());

			_inventoryProductId = _fixture.Create<int>();
			_productPriority = _fixture.Create<int>();

            _saleInvoiceController = new SaleInvoiceController(_saleInvoice,
                                                               _eventAggregator,
                                                               _invoicesRepository,
                                                               _inventoryProductsRepository,
                                                               _receiptPrinter,
                                                               _userAccountHelper);
        }

		private void ArrangeFakeEventObjects()
		{
			A.CallTo(() => _eventAggregator.GetEvent<AllPaymentsRemovedEvent>()).Returns(A.Fake<AllPaymentsRemovedEvent>());
			A.CallTo(() => _eventAggregator.GetEvent<NewSaleStartedEvent>()).Returns(A.Fake<NewSaleStartedEvent>());
			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>()).Returns(A.Fake<SaleInvoiceProductAddedEvent>());
			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>()).Returns(A.Fake<SaleInvoiceProductUpdatedEvent>());
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

			A.CallTo(() => _eventAggregator.GetEvent<AllPaymentsRemovedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void AddProduct_ProductFound_ProductShouldBeAdded()
		{
			var product = _fixture.Create<InventoryProductModel>();

			A.CallTo(() => _inventoryProductsRepository.GetProductByBarcode(A<string>.Ignored)).Returns(product);

			_saleInvoiceController.AddProduct(_fixture.Create<string>());

			A.CallTo(() => _saleInvoice.AddProduct(product)).MustHaveHappenedOnceExactly();
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

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>()).Returns(A.Fake<SaleInvoiceProductRemovedEvent>());

            _saleInvoiceController.RemoveProduct(product);

			A.CallTo(() => _saleInvoice.RemoveProduct(product)).MustHaveHappenedOnceExactly();
			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>().Publish()).MustHaveHappenedOnceExactly();
        }

		[Test]
		public void AddPayment_PaymentShouldBeAdded()
		{
			var type = _fixture.Create<PaymentType>();
			var amount = _fixture.Create<decimal>();
            var note = _fixture.Create<string>();

			A.CallTo(() => _eventAggregator.GetEvent<PaymentAddedEvent>()).Returns(A.Fake<PaymentAddedEvent>());

            _saleInvoiceController.AddPayment(type, amount, note);

			A.CallTo(() => _saleInvoice.AddPayment(type, amount, note)).MustHaveHappenedOnceExactly();
			A.CallTo(() => _eventAggregator.GetEvent<PaymentAddedEvent>().Publish()).MustHaveHappenedOnceExactly();
        }

		[Test]
		public void UpdateProductUnitPrice_ProductFound_UnitPriceShouldBeUpdated()
		{
			var unitPrice = _fixture.Create<decimal>();
			var note = _fixture.Create<string>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductUnitPrice(product.InventoryProductId, product.Priority, unitPrice, note);

			product.UnitPrice.Should().Be(unitPrice);
			product.Note.Should().Be(note);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void UpdateProductUnitPrice_ProductNotFound_UnitPriceShouldNotBeUpdated()
		{
			var unitPrice = _fixture.Create<decimal>();
			var note = _fixture.Create<string>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductUnitPrice(_inventoryProductId, _productPriority, unitPrice, note);

			product.UnitPrice.Should().NotBe(unitPrice);
			product.Note.Should().NotBe(note);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustNotHaveHappened();
		}

		[Test]
		public void UpdateProductUnitPrice_SameUnitPrice_UnitPriceShouldNotBeUpdated()
		{
			var note = _fixture.Create<string>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductUnitPrice(product.InventoryProductId, product.Priority, product.UnitPrice, note);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustNotHaveHappened();
		}

		[Test]
		public void UpdateProductQuantity_WithoutGroupPrice_QuantityShouldBeUpdated()
        {
			var quantity = _fixture.Create<int>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.GroupPriceQuantity = null;
			product.GroupPrice = null;
			product.Quantity = _fixture.Create<int>();
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductQuantity(product.InventoryProductId, product.Priority, quantity);

			product.Quantity.Should().Be(quantity);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustHaveHappenedOnceExactly();
        }

		[Test]
		public void UpdateProductQuantity_ProductNotFound_QuantityShouldNotBeUpdated()
		{
			var quantity = _fixture.Create<int>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductQuantity(_inventoryProductId, _productPriority, quantity);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustNotHaveHappened();
		}

		[Test]
		public void UpdateProductQuantity_SameQuantity_QuantityShouldNotBeUpdated()
		{
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.InventoryProductId = _inventoryProductId;
			product.Priority = _productPriority;
			product.Quantity = _fixture.Create<int>();
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductQuantity(_inventoryProductId, _productPriority, product.Quantity);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustNotHaveHappened();
		}

		[Test]
		public void UpdateProductQuantity_IncreaseBelowGroupPriceSettings_UnitPriceShouldNotBeUpdated()
		{
			var groupPrice = _fixture.Create<decimal>();
			var originalQuantity = _fixture.Create<int>();
			var groupPriceQuantity = originalQuantity + 3;
			var newQuantity = groupPriceQuantity - 1;
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.InventoryProductId = _inventoryProductId;
			product.Priority = _productPriority;
			product.Quantity = originalQuantity;
			product.GroupPriceQuantity = groupPriceQuantity;
			product.GroupPrice = groupPrice;

			var originalUnitPrice = product.UnitPrice;
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductQuantity(_inventoryProductId, _productPriority, newQuantity);

			product.Quantity.Should().Be(newQuantity);
			product.UnitPrice.Should().Be(originalUnitPrice);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void UpdateProductQuantity_IncreaseToGroupPriceSettings_UnitPriceShouldBeUpdated()
		{
			var groupPrice = _fixture.Create<decimal>();
			var originalQuantity = _fixture.Create<int>();
			var groupPriceQuantity = originalQuantity + 3;
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.InventoryProductId = _inventoryProductId;
			product.Priority = _productPriority;
			product.Quantity = originalQuantity;
			product.GroupPriceQuantity = groupPriceQuantity;
			product.GroupPrice = groupPrice;
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductQuantity(_inventoryProductId, _productPriority, groupPriceQuantity);

			var newUnitPrice = groupPrice / groupPriceQuantity;

			product.Quantity.Should().Be(groupPriceQuantity);
			product.UnitPrice.Should().Be(newUnitPrice);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void UpdateProductQuantity_IncreaseToAboveGroupPriceSettings_NewProductsShouldBeAdded()
		{
			var groupPrice = _fixture.Create<decimal>();
			var originalQuantity = _fixture.Create<int>();
			var groupPriceQuantity = originalQuantity + 3;
			var newQuantity = groupPriceQuantity * 3;
			var inventoryProduct = _fixture.Create<InventoryProductModel>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.InventoryProductId = _inventoryProductId;
			product.Priority = _productPriority;
			product.Quantity = originalQuantity;
			product.GroupPriceQuantity = groupPriceQuantity;
			product.GroupPrice = groupPrice;

			A.CallTo(() => _saleInvoice.Products).Returns(productList);
			A.CallTo(() => _inventoryProductsRepository.GetProductById(_inventoryProductId)).Returns(inventoryProduct);

			_saleInvoiceController.UpdateProductQuantity(_inventoryProductId, _productPriority, newQuantity);

			var newUnitPrice = groupPrice / groupPriceQuantity;

			product.Quantity.Should().Be(groupPriceQuantity);
			product.UnitPrice.Should().Be(newUnitPrice);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustHaveHappenedOnceExactly();
			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish()).MustHaveHappenedTwiceExactly();
			A.CallTo(() => _saleInvoice.AddProduct(inventoryProduct, A<decimal>.Ignored, A<int>.Ignored))
			 .MustHaveHappenedTwiceExactly();
		}

		[Test]
		public void UpdateProductQuantity_IncreaseToAboveGroupPriceSettings_ProductNotFound_ShouldThrowException()
		{
			var groupPrice = _fixture.Create<decimal>();
			var originalQuantity = _fixture.Create<int>();
			var groupPriceQuantity = originalQuantity + 3;
			var newQuantity = groupPriceQuantity * 3;
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.InventoryProductId = _inventoryProductId;
			product.Priority = _productPriority;
			product.Quantity = originalQuantity;
			product.GroupPriceQuantity = groupPriceQuantity;

			A.CallTo(() => _saleInvoice.Products).Returns(productList);
			A.CallTo(() => _inventoryProductsRepository.GetProductById(_inventoryProductId)).Returns(null);

			Action act = () => _saleInvoiceController.UpdateProductQuantity(_inventoryProductId, _productPriority, newQuantity);

			act.Should().ThrowExactly<ProductNotFoundException>();
		}

		[Test]
		public void UpdateProductQuantity_DecreaseQuantityFromBelowGroupPriceSettings_UnitPriceShouldNotBeUpdated()
		{
			var groupPrice = _fixture.Create<decimal>();
			var originalQuantity = _fixture.Create<int>();
			var groupPriceQuantity = originalQuantity + 3;
			var newQuantity = originalQuantity - 1;
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.InventoryProductId = _inventoryProductId;
			product.Priority = _productPriority;
			product.Quantity = originalQuantity;
			product.GroupPriceQuantity = groupPriceQuantity;
			product.GroupPrice = groupPrice;

			var originalUnitPrice = product.UnitPrice;

			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductQuantity(_inventoryProductId, _productPriority, newQuantity);

			product.Quantity.Should().Be(newQuantity);
			product.UnitPrice.Should().Be(originalUnitPrice);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void UpdateProductQuantity_DecreaseQuantityFromGroupPriceSettings_UnitPriceShouldBeRestored()
		{
			var groupPrice = _fixture.Create<decimal>();
			var groupPriceQuantity = _fixture.Create<int>();
			var newQuantity = groupPriceQuantity - 1;
			var inventoryProduct = _fixture.Create<InventoryProductModel>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.InventoryProductId = _inventoryProductId;
			product.Priority = _productPriority;
			product.Quantity = groupPriceQuantity;
			product.GroupPriceQuantity = groupPriceQuantity;
			product.GroupPrice = groupPrice;
			product.UnitPrice = groupPrice / groupPriceQuantity;

			A.CallTo(() => _inventoryProductsRepository.GetProductById(_inventoryProductId)).Returns(inventoryProduct);
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			_saleInvoiceController.UpdateProductQuantity(_inventoryProductId, _productPriority, newQuantity);

			product.Quantity.Should().Be(newQuantity);
			product.UnitPrice.Should().Be(inventoryProduct.UnitPrice);

			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void UpdateProductQuantity_DecreaseQuantityFromGroupPriceSettings_ProductNotFound_ShouldThrowException()
		{
			var groupPriceQuantity = _fixture.Create<int>();
			var newQuantity = groupPriceQuantity - 1;
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			product.InventoryProductId = _inventoryProductId;
			product.Priority = _productPriority;
			product.Quantity = groupPriceQuantity;
			product.GroupPriceQuantity = groupPriceQuantity;

			A.CallTo(() => _inventoryProductsRepository.GetProductById(_inventoryProductId)).Returns(null);
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			Action act = () => _saleInvoiceController.UpdateProductQuantity(_inventoryProductId, _productPriority, newQuantity);

			act.Should().ThrowExactly<ProductNotFoundException>();
		}
	}
}
