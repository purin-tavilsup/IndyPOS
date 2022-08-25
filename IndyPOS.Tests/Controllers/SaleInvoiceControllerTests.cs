using AutoFixture;
using FakeItEasy;
using FluentAssertions;
using IndyPOS.Controllers;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Devices;
using IndyPOS.Enums;
using IndyPOS.Events;
using IndyPOS.Exceptions;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Mappers;
using IndyPOS.Sales;
using IndyPOS.Users;
using NUnit.Framework;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccountsReceivableModel = IndyPOS.DataAccess.Models.AccountsReceivable;
using InventoryProductModel = IndyPOS.DataAccess.Models.InventoryProduct;
using InvoiceProductModel = IndyPOS.DataAccess.Models.InvoiceProduct;
using Payment = IndyPOS.Sales.Payment;
using PaymentModel = IndyPOS.DataAccess.Models.Payment;

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
		private IAccountsReceivableRepository _accountsReceivableRepository;
		private IReportHelper _reportHelper;
		private IDataFeedApiHelper _dataFeedApiHelper;
		private ISaleInvoiceMapper _invoiceMapper;
        private IFixture _fixture;
		private int _inventoryProductId;
		private int _productPriority;

		private const decimal ZeroMoneyValue = 0m;

        [SetUp]
        public void Setup()
		{
			SetupFixture();
			InstantiateFakeObjects();
			SetupFakeEventObjects();
			
			_inventoryProductId = _fixture.Create<int>();
			_productPriority = _fixture.Create<int>();

            _saleInvoiceController = new SaleInvoiceController(_saleInvoice,
                                                               _eventAggregator,
                                                               _invoicesRepository,
                                                               _inventoryProductsRepository,
                                                               _receiptPrinter,
                                                               _userAccountHelper,
															   _accountsReceivableRepository,
															   _reportHelper,
															   _dataFeedApiHelper,
															   _invoiceMapper);
        }

		private void SetupFakeEventObjects()
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
			_accountsReceivableRepository = A.Fake<IAccountsReceivableRepository>();
			_reportHelper = A.Fake<IReportHelper>();
			_dataFeedApiHelper = A.Fake<IDataFeedApiHelper>();
			_invoiceMapper = A.Fake<ISaleInvoiceMapper>();
		}

		private void SetupFixture()
        {
			_fixture = new Fixture();
			_fixture.Register<ISaleInvoiceProduct>(() => _fixture.Create<SaleInvoiceProduct>());
			_fixture.Register<IPayment>(() => _fixture.Create<Payment>());
			_fixture.Register<IUserAccount>(() => _fixture.Create<UserAccount>());
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
		public void AddProduct_WithDefaultSettings_ProductShouldBeAdded()
		{
			var product = _fixture.Create<InventoryProductModel>();

			_saleInvoiceController.AddProduct(product);

			A.CallTo(() => _saleInvoice.AddProduct(product)).MustHaveHappenedOnceExactly();
			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void AddProduct_WithSpecification_ProductShouldBeAddedWithSpecifiedValues()
		{
			var product = _fixture.Create<InventoryProductModel>();
			var unitPrice = _fixture.Create<decimal>();
			var quantity = _fixture.Create<int>();
			var note = _fixture.Create<string>();

			_saleInvoiceController.AddProduct(product, unitPrice, quantity, note);

			A.CallTo(() => _saleInvoice.AddProduct(product, unitPrice, quantity, note)).MustHaveHappenedOnceExactly();
			A.CallTo(() => _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish()).MustHaveHappenedOnceExactly();
		}

		[Test]
		public void GetInventoryProductByBarcode_ProductFound_ShouldReturnProduct()
		{
			var product = _fixture.Create<InventoryProductModel>();

			A.CallTo(() => _inventoryProductsRepository.GetProductByBarcode(product.Barcode)).Returns(product);

            var result = _saleInvoiceController.GetInventoryProductByBarcode(product.Barcode);

            result.Should().Be(product);
        }

		[Test]
		public void GetInventoryProductByBarcode_ProductNotFound_ShouldNotReturnProduct()
		{
			var barcode = _fixture.Create<string>();

			A.CallTo(() => _inventoryProductsRepository.GetProductByBarcode(barcode)).Returns(null);

			var result = _saleInvoiceController.GetInventoryProductByBarcode(barcode);

			result.Should().BeNull();
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
		public void UpdateProductUnitPrice_ProductNotFound_ShouldThrowProductNotFoundException()
		{
			var unitPrice = _fixture.Create<decimal>();
			var note = _fixture.Create<string>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			Action act = () => _saleInvoiceController.UpdateProductUnitPrice(_inventoryProductId, _productPriority, unitPrice, note);

			act.Should().ThrowExactly<ProductNotFoundException>();
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
		public void UpdateProductQuantity_ProductNotFound_ShouldThrowProductNotFoundException()
		{
			var quantity = _fixture.Create<int>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			Action act = () => _saleInvoiceController.UpdateProductQuantity(_inventoryProductId, _productPriority, quantity);

			act.Should().ThrowExactly<ProductNotFoundException>();
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

		[Test]
		public void ValidateSaleInvoice_UserHasNotLoggedIn_ShouldReturnErrorMessage()
		{
			var payment = _fixture.Create<IPayment>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			var paymentList = new List<IPayment> { payment };

			A.CallTo(() => _saleInvoice.Products).Returns(productList);
			A.CallTo(() => _saleInvoice.Payments).Returns(paymentList);
			A.CallTo(() => _userAccountHelper.LoggedInUser).Returns(null);

			var errorMessages = _saleInvoiceController.ValidateSaleInvoice();

			errorMessages.Should().NotBeEmpty();
		}

		[Test]
		public void ValidateSaleInvoice_SaleInvoiceHasNoProduct_ShouldReturnErrorMessage()
		{
			var payment = _fixture.Create<IPayment>();
			var paymentList = new List<IPayment> { payment };

			A.CallTo(() => _saleInvoice.Products).Returns(new List<ISaleInvoiceProduct>());
			A.CallTo(() => _saleInvoice.Payments).Returns(paymentList);
			A.CallTo(() => _userAccountHelper.LoggedInUser).Returns(_fixture.Create<IUserAccount>());

			var errorMessages = _saleInvoiceController.ValidateSaleInvoice();

			errorMessages.Should().NotBeEmpty();
		}

		[Test]
		public void ValidateSaleInvoice_SaleInvoiceHasNoPayment_ShouldReturnErrorMessage()
		{
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };

			A.CallTo(() => _saleInvoice.Products).Returns(productList);
			A.CallTo(() => _saleInvoice.Payments).Returns(new List<IPayment>());
			A.CallTo(() => _userAccountHelper.LoggedInUser).Returns(_fixture.Create<IUserAccount>());

			var errorMessages = _saleInvoiceController.ValidateSaleInvoice();

			errorMessages.Should().NotBeEmpty();
		}

		[Test]
		public void ValidateSaleInvoice_SaleInvoiceIsPendingPayment_ShouldReturnErrorMessage()
		{
			var payment = _fixture.Create<IPayment>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			var paymentList = new List<IPayment> { payment };
			var paymentTotal = _fixture.Create<decimal>();
			var invoiceTotal = paymentTotal + _fixture.Create<decimal>();

			A.CallTo(() => _saleInvoice.Products).Returns(productList);
			A.CallTo(() => _saleInvoice.Payments).Returns(paymentList);
			A.CallTo(() => _saleInvoice.IsRefundInvoice).Returns(false);
			A.CallTo(() => _saleInvoice.InvoiceTotal).Returns(invoiceTotal);
			A.CallTo(() => _saleInvoice.PaymentTotal).Returns(paymentTotal);
			A.CallTo(() => _userAccountHelper.LoggedInUser).Returns(_fixture.Create<IUserAccount>());

			var errorMessages = _saleInvoiceController.ValidateSaleInvoice();

			errorMessages.Should().NotBeEmpty();
		}

		[Test]
		public void ValidateSaleInvoice_RefundInvoiceIsPendingPayment_ShouldReturnErrorMessage()
		{
			var payment = _fixture.Create<IPayment>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			var paymentList = new List<IPayment> { payment };
			var invoiceTotal = _fixture.Create<decimal>() * -1;

			A.CallTo(() => _saleInvoice.Products).Returns(productList);
			A.CallTo(() => _saleInvoice.Payments).Returns(paymentList);
			A.CallTo(() => _saleInvoice.IsRefundInvoice).Returns(true);
			A.CallTo(() => _saleInvoice.InvoiceTotal).Returns(invoiceTotal);
			A.CallTo(() => _saleInvoice.PaymentTotal).Returns(0m);
			A.CallTo(() => _userAccountHelper.LoggedInUser).Returns(_fixture.Create<IUserAccount>());

			var errorMessages = _saleInvoiceController.ValidateSaleInvoice();

			errorMessages.Should().NotBeEmpty();
		}

		[Test]
		public void ValidateSaleInvoice_SaleInvoiceIsValid_ShouldNotReturnErrorMessage()
		{
			var payment = _fixture.Create<IPayment>();
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			var paymentList = new List<IPayment> { payment };
			var invoiceTotal = _fixture.Create<decimal>();

			A.CallTo(() => _saleInvoice.Products).Returns(productList);
			A.CallTo(() => _saleInvoice.Payments).Returns(paymentList);
			A.CallTo(() => _saleInvoice.IsRefundInvoice).Returns(false);
			A.CallTo(() => _saleInvoice.InvoiceTotal).Returns(invoiceTotal);
			A.CallTo(() => _saleInvoice.PaymentTotal).Returns(invoiceTotal);
			A.CallTo(() => _userAccountHelper.LoggedInUser).Returns(_fixture.Create<IUserAccount>());

			var errorMessages = _saleInvoiceController.ValidateSaleInvoice();

			errorMessages.Should().BeEmpty();
		}

		[Test]
		public async Task CompleteSale_SaleInvoiceShouldBeSavedToDatabase()
        {
			await _saleInvoiceController.CompleteSale();

			A.CallTo(() => _invoicesRepository.AddInvoice(A<DataAccess.Models.Invoice>.Ignored))
			 .MustHaveHappenedOnceExactly();
		}

		[Test]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		public async Task CompleteSale_AllPaymentsShouldBeSavedToDatabase(int numberOfPayments)
		{
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			var paymentList = new List<IPayment>();

			for (var i = 0; i < numberOfPayments; i++)
			{
				paymentList.Add(_fixture.Create<IPayment>());
			}

			A.CallTo(() => _saleInvoice.Products).Returns(productList);
			A.CallTo(() => _saleInvoice.Payments).Returns(paymentList);
			A.CallTo(() => _saleInvoice.Id).Returns(_fixture.Create<int>());

			await _saleInvoiceController.CompleteSale();

			A.CallTo(() => _invoicesRepository.AddPayment(A<PaymentModel>.Ignored))
			 .MustHaveHappened(paymentList.Count, Times.Exactly);
		}

		[Test]
		public async Task CompleteSale_PaymentTypeIsAccountsReceivable_AccountsReceivableShouldBeSavedToDatabase()
		{
			var product = _fixture.Create<ISaleInvoiceProduct>();
			var productList = new List<ISaleInvoiceProduct> { product };
			var payment = A.Fake<IPayment>();

			A.CallTo(() => _saleInvoice.Products).Returns(productList);
			A.CallTo(() => payment.PaymentTypeId).Returns((int) PaymentType.AccountReceivable);
			A.CallTo(() => _saleInvoice.Payments).Returns(new List<IPayment> { payment });

			await _saleInvoiceController.CompleteSale();

			A.CallTo(() => _accountsReceivableRepository.AddAccountsReceivable(A<AccountsReceivableModel>.Ignored))
			 .MustHaveHappenedOnceExactly();
		}

		[Test]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		public async Task CompleteSale_AllProductsShouldBeSavedToDatabase(int numberOfProducts)
		{
			var productList = new List<ISaleInvoiceProduct>();

			for (var i = 0; i < numberOfProducts; i++)
            {
				productList.Add(_fixture.Create<ISaleInvoiceProduct>());
            }

			A.CallTo(() => _saleInvoice.Products).Returns(productList);

			await _saleInvoiceController.CompleteSale();

			A.CallTo(() => _invoicesRepository.AddInvoiceProduct(A<InvoiceProductModel>.Ignored))
			 .MustHaveHappened(productList.Count, Times.Exactly);
		}

		[Test]
		public async Task CompleteSale_ProductQuantityInStockShouldBeUpdated()
		{
			const int quantityInStock = 100;
			const int quantity = 5;
			const int numberOfProducts = 3;
			const int expectedNewQuantityInStock = quantityInStock - quantity * numberOfProducts;

			var productList = new List<ISaleInvoiceProduct>();
			var product = A.Fake<ISaleInvoiceProduct>();

			A.CallTo(() => product.IsTrackable).Returns(true);
			A.CallTo(() => product.InventoryProductId).Returns(_inventoryProductId);
			A.CallTo(() => product.Quantity).Returns(quantity);

			for (var i = 0; i < numberOfProducts; i++)
            {
				productList.Add(product);
            }

			var inventoryProduct = _fixture.Create<InventoryProductModel>();
			inventoryProduct.InventoryProductId = _inventoryProductId;
			inventoryProduct.QuantityInStock = quantityInStock;

            A.CallTo(() => _inventoryProductsRepository.GetProductById(_inventoryProductId)).Returns(inventoryProduct);
			A.CallTo(() => _saleInvoice.Products).Returns(productList);

            await _saleInvoiceController.CompleteSale();

            A.CallTo(() => _inventoryProductsRepository.UpdateProductQuantityById(_inventoryProductId, expectedNewQuantityInStock))
			 .MustHaveHappenedOnceExactly();
        }

		[Test]
		public void PrintReceipt_ShouldPrintReceipt()
		{
			var loggedUser = _fixture.Create<IUserAccount>();

			A.CallTo(() => _userAccountHelper.LoggedInUser).Returns(loggedUser);

			_saleInvoiceController.PrintReceipt();

			A.CallTo(() => _receiptPrinter.PrintReceipt(A<ISaleInvoice>.Ignored, loggedUser))
			 .MustHaveHappenedOnceExactly();
		}
	}
}
