//using AutoFixture;
//using AutoFixture.AutoMoq;
//using AutoFixture.Xunit2;
//using IndyPOS.Application.Common.Enums;
//using IndyPOS.Application.Common.Interfaces;
//using IndyPOS.Application.Helpers;
//using IndyPOS.Application.Tests.Mocks.Attributes;
//using Moq;
//using System;
//using Xunit;

//namespace IndyPOS.Application.Tests
//{
//    public class ReportHelperTests
//    {
//		public class ArrangeAttribute : AutoDataAttribute
//		{
//			public ArrangeAttribute() 
//				: base(() => new Fixture()
//						   .Customize(new CompositeCustomization(new AutoMoqCustomization())))
//			{
//			}
//		}

//        [Theory]
//        [InlineAutoMoqData(TimePeriod.Today)]
//		[InlineAutoMoqData(TimePeriod.ThisMonth)]
//		[InlineAutoMoqData(TimePeriod.ThisYear)]
//        public void GetInvoicesByPeriod_ShouldCallSaleInvoiceHelper(
//			TimePeriod period,
//			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
//			ReportHelper sut)
//		{
//            // Act
//			sut.GetInvoicesByPeriod(period);

//            // Assert
//            saleInvoiceHelper.Verify(s => s.GetInvoicesByPeriod(period), Times.Once);
//		}

//		[Theory]
//		[AutoMoqData]
//		public void GetInvoicesByDateRange_ShouldCallSaleInvoiceHelper(
//			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
//			DateTime startDate,
//			DateTime endDate,
//			ReportHelper sut)
//		{
//            // AutoFixture currently does not support DateOnly
//			// Have to work around with conversion from DateTime 
//			var start = DateOnly.FromDateTime(startDate);
//			var end = DateOnly.FromDateTime(endDate);

//			// Act
//			sut.GetInvoicesByDateRange(start, end);

//			// Assert
//			saleInvoiceHelper.Verify(s => s.GetInvoicesByDateRange(start, end), Times.Once);
//		}

//		[Theory]
//		[AutoMoqData]
//		public void GetInvoiceProductsByDate_ShouldCallSaleInvoiceHelper(
//			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
//			DateTime date,
//			ReportHelper sut)
//		{
//			// AutoFixture currently does not support DateOnly
//			// Have to work around with conversion from DateTime 
//			var dateOnly = DateOnly.FromDateTime(date);

//			// Act
//			sut.GetInvoiceProductsByDate(dateOnly);

//			// Assert
//			saleInvoiceHelper.Verify(s => s.GetInvoiceProductsByDate(dateOnly), Times.Once);
//		}

//		[Theory]
//		[AutoMoqData]
//		public void GetInvoiceProductsByDateRange_ShouldCallSaleInvoiceHelper(
//			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
//			DateTime startDate,
//			DateTime endDate,
//			ReportHelper sut)
//		{
//			// AutoFixture currently does not support DateOnly
//			// Have to work around with conversion from DateTime 
//			var start = DateOnly.FromDateTime(startDate);
//			var end = DateOnly.FromDateTime(endDate);

//			// Act
//			sut.GetInvoiceProductsByDateRange(start, end);

//			// Assert
//			saleInvoiceHelper.Verify(s => s.GetInvoiceProductsByDateRange(start, end), Times.Once);
//		}

//		[Theory]
//		[AutoMoqData]
//		public void GetInvoiceProductsByInvoiceId_ShouldCallSaleInvoiceHelper(
//			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
//			int invoiceId,
//			ReportHelper sut)
//		{
//			// Act
//			sut.GetInvoiceProductsByInvoiceId(invoiceId);

//			// Assert
//			saleInvoiceHelper.Verify(s => s.GetInvoiceProductsByInvoiceId(invoiceId), Times.Once);
//		}

//		[Theory]
//		[AutoMoqData]
//		public void GetPaymentsByInvoiceId_ShouldCallSaleInvoiceHelper(
//			[Frozen] Mock<ISaleInvoiceHelper> saleInvoiceHelper,
//			int invoiceId,
//			ReportHelper sut)
//		{
//			// Act
//			sut.GetPaymentsByInvoiceId(invoiceId);

//			// Assert
//			saleInvoiceHelper.Verify(s => s.GetPaymentsByInvoiceId(invoiceId), Times.Once);
//		}
//    }
//}