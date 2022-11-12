using Dapper;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite
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
                    Priority,
                    InventoryProductId,
                    Barcode,
                    Description,
                    Manufacturer,
                    Brand,
                    Category,
                    UnitPrice,
                    Quantity,
                    DateCreated,
					Note
                )
                VALUES
                (
                    @InvoiceId,
                    @Priority,
                    @InventoryProductId,
                    @Barcode,
					@Description,
                    @Manufacturer,
                    @Brand,
                    @Category,
                    @UnitPrice,
                    @Quantity,
                    datetime('now','localtime'),
					@Note
                );
                SELECT last_insert_rowid()";

				var sqlParameters = new
				{
                    product.InvoiceId,
                    product.Priority,
                    product.InventoryProductId,
                    product.Barcode,
                    product.Description,
                    product.Manufacturer,
                    product.Brand,
                    product.Category,
					UnitPrice = MapMoneyToString(product.UnitPrice),
                    product.Quantity,
                    product.Note
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
                    DateCreated,
					Note
                )
                VALUES
                (
                    @InvoiceId,
                    @PaymentTypeId,
                    @Amount,
                    datetime('now','localtime'),
					@Note
                );
                SELECT last_insert_rowid()";

                var sqlParameters = new
                {
                    payment.InvoiceId,
                    payment.PaymentTypeId,
                    Amount = MapMoneyToString(payment.Amount),
                    payment.Note
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
                Priority,
                InvoiceId,
                InventoryProductId,
                Barcode,
                Description,
                Manufacturer,
                Brand,
                Category,
                UnitPrice,
                Quantity,
                DateCreated,
				Note
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
                Priority,
                InvoiceId,
                InventoryProductId,
                Barcode,
                Description,
                Manufacturer,
                Brand,
                Category,
                UnitPrice,
                Quantity,
                DateCreated,
				Note
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

        public IList<Payment> GetPaymentsByInvoiceId(int id)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"SELECT
                PaymentId,
                InvoiceId,
                PaymentTypeId,
                Amount,
                DateCreated,
				Note
                FROM Payments 
                WHERE InvoiceId = @invoiceId";

                var sqlParameters = new
                {
                    invoiceId = id
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapPayments(results);
            }
        }

		public IList<Payment> GetPaymentsByDate(DateTime date)
		{
			return GetPaymentsByDateRange(date, date);
		}

		public IList<Payment> GetPaymentsByDateRange(DateTime start, DateTime end)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
				connection.Open();

				const string sqlCommand = @"SELECT
                PaymentId,
                InvoiceId,
                PaymentTypeId,
                Amount,
                DateCreated,
				Note
                FROM Payments 
                WHERE DateCreated BETWEEN @startDate AND @endDate";

				var sqlParameters = new
									{
										startDate = MapStartDateToString(start),
										endDate = MapEndDateToString(end)
									};

				var results = connection.Query(sqlCommand, sqlParameters);

				return MapPayments(results);
			}
		}

		public IList<Payment> GetPaymentsByPaymentTypeId(int id)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
				connection.Open();

				const string sqlCommand = @"SELECT
                PaymentId,
                InvoiceId,
                PaymentTypeId,
                Amount,
                DateCreated,
				Note
                FROM Payments 
                WHERE PaymentTypeId = @PaymentTypeId";

				var sqlParameters = new
									{
										PaymentTypeId = id
									};

				var results = connection.Query(sqlCommand, sqlParameters);

				return MapPayments(results);
			}
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

                Priority = (int)x.Priority,

                InvoiceId = (int)x.InvoiceId,

                InventoryProductId = (int)x.InventoryProductId,

                Barcode = x.Barcode,

                Description = x.Description,

                Manufacturer = x.Manufacturer,

                Brand = x.Brand,

                Category = (int)x.Category,

                UnitPrice = MapMoneyToDecimal(x.UnitPrice),

                Quantity = (int)x.Quantity,

                DateCreated = x.DateCreated,

                Note = x.Note
            }) ?? Enumerable.Empty<InvoiceProduct>();

            return products.ToList();
        }

        private IList<Payment> MapPayments(IEnumerable<dynamic> results)
        {
            var payments = results?.Select(x => new Payment
            {
                PaymentId = (int)x.PaymentId,

                InvoiceId = (int)x.InvoiceId,

                PaymentTypeId = (int)x.PaymentTypeId,

                Amount = MapMoneyToDecimal(x.Amount),

                DateCreated = x.DateCreated,

                Note = x.Note
            }) ?? Enumerable.Empty<Payment>();

            return payments.ToList();
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

            return $"{result}";
        }
    }
}
