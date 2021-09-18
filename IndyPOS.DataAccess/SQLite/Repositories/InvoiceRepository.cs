﻿using Dapper;
using IndyPOS.DataAccess.Extensions;
using IndyPOS.DataAccess.Models;
using IndyPOS.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.DataAccess.SQLite.Repositories
{
	public class InvoiceRepository : IInvoiceRepository
    {
        private readonly IDbConnectionProvider _dbConnectionProvider;

        public InvoiceRepository(IDbConnectionProvider dbConnectionProvider)
        {
            _dbConnectionProvider = dbConnectionProvider;
        }

        public int AddInvoice(Invoice invoice)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"INSERT INTO Invoices
                (
                    Total,
                    CustomerId,
                    UserId,
                    DateCreated
                )
                VALUES
                (
                    @Total,
                    @CustomerId,
                    @UserId,
                    datetime('now','localtime')
                );
                SELECT last_insert_rowid()";

                var sqlParameters = new
                {
                    Total = MapMoneyToString(invoice.Total),
                    invoice.CustomerId,
                    invoice.UserId
                };

                var invoiceId = connection.Query<int>(sqlCommand, sqlParameters).FirstOrDefault();

                if (invoiceId < 1) throw new Exception("Failed to get the last insert Row ID after adding a new invoice.");

                return invoiceId;
            }
        }

        public int AddInvoiceProduct(InvoiceProduct product)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"INSERT INTO InvoiceProducts
                (
                    InvoiceId,
                    InventoryProductId,
                    Barcode,
                    Description,
                    Manufacturer,
                    Brand,
                    Category,
                    UnitCost,
                    UnitPrice,
                    Quantity,
                    DateCreated
                )
                VALUES
                (
                    @InvoiceId,
                    @InventoryProductId,
                    @Barcode,
					@Description,
                    @Manufacturer,
                    @Brand,
                    @Category,
                    @UnitCost,
                    @UnitPrice,
                    @Quantity,
                    datetime('now','localtime')
                );
                SELECT last_insert_rowid()";

				var sqlParameters = new
				{
                    product.InvoiceId,
                    product.InventoryProductId,
                    product.Barcode,
                    product.Description,
                    product.Manufacturer,
                    product.Brand,
                    product.Category,
                    product.UnitCost,
                    product.UnitPrice,
                    product.Quantity
                };

				var invoiceProductId = connection.Query<int>(sqlCommand, sqlParameters).FirstOrDefault();

                if (invoiceProductId < 1) throw new Exception("Failed to get the last insert Row ID after adding an invoice product.");

                return invoiceProductId;
            }
        }

        public int AddPayment(Payment payment)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"INSERT INTO Payments
                (
                    InvoiceId,
                    PaymentTypeId,
                    Amount,
                    DateCreated
                )
                VALUES
                (
                    @InvoiceId,
                    @PaymentTypeId,
                    @Amount,
                    datetime('now','localtime')
                );
                SELECT last_insert_rowid()";

                var sqlParameters = new
                {
                    payment.InvoiceId,
                    payment.PaymentTypeId,
                    Amount = MapMoneyToString(payment.Amount)
                };

                var paymentId = connection.Query<int>(sqlCommand, sqlParameters).FirstOrDefault();

                if (paymentId < 1) throw new Exception("Failed to get the last insert Row ID after adding a new payment.");

                return paymentId;
            }
        }

        public Invoice GetInvoiceByInvoiceId(int id)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"SELECT
                InvoiceId,
                Total,
                CustomerId,
                UserId,
                DateCreated
                FROM Invoices 
                WHERE InvoiceId = @invoiceId";

                var sqlParameters = new
                {
                    invoiceId = id
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapInvoices(results).FirstOrDefault();
            }
        }

        public IList<Invoice> GetInvoicesByDateRange(DateTime start, DateTime end)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"SELECT
                InvoiceId,
                Total,
                CustomerId,
                UserId,
                DateCreated
                FROM Invoices 
                WHERE DateCreated BETWEEN @startDate AND @endDate";

                var sqlParameters = new
                {
                    startDate = MapStartDateToString(start),
                    endDate = MapEndDateToString(end)
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapInvoices(results);
            }
        }

        public IList<Invoice> GetInvoicesByDate(DateTime date)
        {
            return GetInvoicesByDateRange(date, date);
        }

		public IList<InvoiceProduct> GetInvoiceProductsByInvoiceId(int id)
		{
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"SELECT
                InvoiceProductId,
                InvoiceId,
                InventoryProductId,
                Barcode,
                Description,
                Manufacturer,
                Brand,
                Category,
                UnitCost,
                UnitPrice,
                Quantity,
                DateCreated
                FROM InvoiceProducts 
                WHERE InvoiceId = @invoiceId";

                var sqlParameters = new
                {
                    invoiceId = id
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapInvoiceProducts(results);
            }
        }

        public IList<InvoiceProduct> GetInvoiceProductsByDateRange(DateTime start, DateTime end)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"SELECT
                InvoiceProductId,
                InvoiceId,
                InventoryProductId,
                Barcode,
                Description,
                Manufacturer,
                Brand,
                Category,
                UnitCost,
                UnitPrice,
                Quantity,
                DateCreated
                FROM InvoiceProducts 
                WHERE DateCreated BETWEEN @startDate AND @endDate";

                var sqlParameters = new
                {
                    startDate = MapStartDateToString(start),
                    endDate = MapEndDateToString(end)
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapInvoiceProducts(results);
            }
        }

        public IList<InvoiceProduct> GetInvoiceProductsByDate(DateTime date)
        {
            return GetInvoiceProductsByDateRange(date, date);
        }

        private string MapStartDateToString(DateTime date)
		{
            var dateString = date.ToString("yyyy-MM-dd");

            return $"{dateString} 00:00";
		}

        private string MapEndDateToString(DateTime date)
        {
            var dateString = date.ToString("yyyy-MM-dd");

            return $"{dateString} 24:00";
        }

        private IList<Invoice> MapInvoices(IEnumerable<dynamic> results)
        {
            var invoices = results?.Select(x => new Invoice
            {
                InvoiceId = (int)x.InvoiceId,

                Total = MapMoneyToDecimal(x.Total),

                CustomerId = (int?)x.CustomerId,

                UserId = (int)x.UserId,

                DateCreated = x.DateCreated
            }) ?? Enumerable.Empty<Invoice>();

            return invoices.ToList();
        }

        private IList<InvoiceProduct> MapInvoiceProducts(IEnumerable<dynamic> results)
        {
            var products = results?.Select(x => new InvoiceProduct
            {
                InvoiceProductId = (int)x.InvoiceProductId,

                InvoiceId = (int)x.InvoiceId,

                InventoryProductId = (int)x.InventoryProductId,

                Barcode = x.Barcode,

                Description = x.Description,

                Manufacturer = x.Manufacturer,

                Brand = x.Brand,

                Category = (int)x.Category,

                UnitCost = MapMoneyToDecimal(x.UnitCost),

                UnitPrice = MapMoneyToDecimal(x.UnitPrice),

                Quantity = (int)x.Quantity,

                DateCreated = x.DateCreated,
            }) ?? Enumerable.Empty<InvoiceProduct>();

            return products.ToList();
        }

        private decimal? MapMoneyToNullableDecimal(string value)
        {
            if (!value.HasValue())
                return null;

            if (decimal.TryParse(value.Trim(), out var result))
                return result / 100m;

            return null;
        }

        private decimal MapMoneyToDecimal(string value)
        {
            if (decimal.TryParse(value.Trim(), out var result))
                return result / 100m;

            return 0m;
        }

        private string MapMoneyToString(decimal? value)
        {
            if (!value.HasValue)
                return null;

            var result = Math.Round(value.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero) * 100m;

            return result.ToString();
        }
    }
}
