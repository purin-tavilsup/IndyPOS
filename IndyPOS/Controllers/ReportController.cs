﻿using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Enums;
using IndyPOS.Extensions;
using IndyPOS.Sales;
using IndyPOS.Users;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.Controllers
{
    internal class ReportController : IReportController
	{
		private readonly IUserAccountHelper _accountHelper;
		private readonly IInvoiceRepository _invoicesRepository;
		private readonly IInventoryProductRepository _inventoryProductsRepository;

		private IEnumerable<IFinalInvoice> _invoices;
		private IEnumerable<IFinalInvoiceProduct> _invoiceProducts;

		public IEnumerable<IFinalInvoice> Invoices => _invoices ?? new List<IFinalInvoice>();

		public IEnumerable<IFinalInvoiceProduct> InvoiceProducts => _invoiceProducts ?? new List<IFinalInvoiceProduct>();

		public IEnumerable<IFinalInvoiceProduct> GeneralGoodsProducts => GetGeneralGoodsProducts();

		public IEnumerable<IFinalInvoiceProduct> HardwareProducts => GetHardwareProducts();

		public decimal InvoicesTotal => GetInvoicesTotal();

		public decimal GeneralGoodsProductsTotal => GetGeneralGoodsProductsTotal();

		public decimal HardwareProductsTotal => GetHardwareProductsTotal();

        public ReportController(IUserAccountHelper accountHelper,
								IInvoiceRepository invoicesRepository,
								IInventoryProductRepository inventoryProductsRepository)
		{
			_accountHelper = accountHelper;
			_invoicesRepository = invoicesRepository;
			_inventoryProductsRepository = inventoryProductsRepository;
        }

		public void LoadInvoicesByPeriod(ReportPeriod period)
		{
			DateTime startDate;
			DateTime endDate;

			switch (period)
			{
				case ReportPeriod.Today:
					startDate = DateTime.Today;
					endDate = DateTime.Today;
					break;

				case ReportPeriod.ThisWeek:
					startDate = DateTime.Today.FirstDayOfWeek();
					endDate = DateTime.Today.LastDayOfWeek();
					break;

				case ReportPeriod.ThisMonth:
					startDate = DateTime.Today.FirstDayOfMonth();
					endDate = DateTime.Today.LastDayOfMonth();
					break;

				default:
					startDate = DateTime.Today;
					endDate = DateTime.Today;
					break;
			}

			_invoices = GetInvoicesByDateRange(startDate, endDate);
			_invoiceProducts = GetInvoiceProductsByDateRange(startDate, endDate);
		}

		public void LoadInvoicesByDateRange(DateTime startDate, DateTime endDate)
		{
			_invoices = GetInvoicesByDateRange(startDate, endDate);
			_invoiceProducts = GetInvoiceProductsByDateRange(startDate, endDate);
		}

		private decimal GetInvoicesTotal()
		{
			return Invoices.Sum(x => x.Total);
		}

		private decimal GetGeneralGoodsProductsTotal()
        {
			return GeneralGoodsProducts.Sum(x => (x.UnitPrice * x.Quantity));
        }

		private decimal GetHardwareProductsTotal()
		{
			return HardwareProducts.Sum(x => (x.UnitPrice * x.Quantity));
		}

		private IEnumerable<IFinalInvoiceProduct> GetGeneralGoodsProducts()
		{
			return InvoiceProducts.Where(p => p.Category < (int) ProductCategory.Hardware);
		}

		private IEnumerable<IFinalInvoiceProduct> GetHardwareProducts()
		{
			return InvoiceProducts.Where(p => p.Category >= (int) ProductCategory.Hardware);
		}

		private IEnumerable<IFinalInvoice> GetInvoicesByDateRange(DateTime startDate, DateTime endDate)
		{
			var results = _invoicesRepository.GetInvoicesByDateRange(startDate, endDate);

			return results.Select(x => new FinalInvoiceAdapter(x) as IFinalInvoice);
		}

		private IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDateRange(DateTime startDate, DateTime endDate)
		{
			var results = _invoicesRepository.GetInvoiceProductsByDateRange(startDate, endDate);

			return results.Select(x => new FinalInvoiceProductAdapter(x) as IFinalInvoiceProduct);
		}

		public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date)
		{
			var results = _invoicesRepository.GetInvoiceProductsByDate(date);

			return results.Select(x => new FinalInvoiceProductAdapter(x) as IFinalInvoiceProduct);
		}
    }
}
