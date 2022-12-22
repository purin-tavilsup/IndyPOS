using Dapper;
using IndyPOS.Common.Exceptions;
using IndyPOS.DataAccess.Extensions;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite;

public class InvoiceRepository : IInvoiceRepository
{
	private readonly IDbConnectionProvider _dbConnectionProvider;

	public InvoiceRepository(IDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public int AddInvoice(Invoice invoice)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
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
			Total = invoice.Total.ToMoneyString(),
			invoice.CustomerId,
			invoice.UserId
		};

		var invoiceId = connection.Query<int>(sqlCommand, sqlParameters)
								  .FirstOrDefault();

		if (invoiceId < 1) 
			throw new InvoiceNotAddedException("Failed to add a new invoice.");

		return invoiceId;
	}

	public int AddInvoiceProduct(InvoiceProduct product)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
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
			UnitPrice = product.UnitPrice.ToMoneyString(),
			product.Quantity,
			product.Note
		};

		var invoiceProductId = connection.Query<int>(sqlCommand, sqlParameters)
										 .FirstOrDefault();

		if (invoiceProductId < 1) 
			throw new ProductNotAddedException($"Failed to add an invoice product. Product barcode: {product.Barcode}.");

		return invoiceProductId;
	}

	public int AddPayment(Payment payment)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
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
			Amount = payment.Amount.ToMoneyString(),
			payment.Note
		};

		var paymentId = connection.Query<int>(sqlCommand, sqlParameters)
								  .FirstOrDefault();

		if (paymentId < 1) 
			throw new PaymentNotAddedException($"Failed to add a new payment. InvoiceId: {payment.InvoiceId}.");

		return paymentId;
	}

	public Invoice GetInvoiceByInvoiceId(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
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

		var result = connection.Query(sqlCommand, sqlParameters)
							   .FirstOrDefault();
		if (result is null)
			throw new InvoiceNotFoundException($"Invoice is not found. InvoiceId: {id}.");

		return MapInvoice(result);
	}

	public IList<Invoice> GetInvoicesByDateRange(DateTime start, DateTime end)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
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
			startDate = start.ToStartDateString(),
			endDate = end.ToEndDateString()
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? new List<Invoice>() : MapInvoices(results);
	}

	public IList<Invoice> GetInvoicesByDate(DateTime date)
	{
		return GetInvoicesByDateRange(date, date);
	}

	public IList<InvoiceProduct> GetInvoiceProductsByInvoiceId(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
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

		return results is null ? new List<InvoiceProduct>() : MapInvoiceProducts(results);
	}

	public IList<InvoiceProduct> GetInvoiceProductsByDateRange(DateTime start, DateTime end)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
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
			startDate = start.ToStartDateString(),
			endDate = end.ToEndDateString()
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? new List<InvoiceProduct>() : MapInvoiceProducts(results);
	}

	public IList<InvoiceProduct> GetInvoiceProductsByDate(DateTime date)
	{
		return GetInvoiceProductsByDateRange(date, date);
	}

	public IList<Payment> GetPaymentsByInvoiceId(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
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

		return results is null ? new List<Payment>() : MapPayments(results);
	}

	public IList<Payment> GetPaymentsByDate(DateTime date)
	{
		return GetPaymentsByDateRange(date, date);
	}

	public IList<Payment> GetPaymentsByDateRange(DateTime start, DateTime end)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
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
			startDate = start.ToStartDateString(),
			endDate = end.ToEndDateString()
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? new List<Payment>() : MapPayments(results);
	}

	public IList<Payment> GetPaymentsByPaymentTypeId(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
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

		return results is null ? new List<Payment>() : MapPayments(results);
	}

	private static Invoice MapInvoice(dynamic result)
	{
		var invoice = new Invoice
		{
			InvoiceId = (int)result.InvoiceId,
			Total = ((string)result.Total).ToMoney(),
			CustomerId = (int?)result.CustomerId,
			UserId = (int)result.UserId,
			DateCreated = result.DateCreated
		};

		return invoice;
	}

	private static IList<Invoice> MapInvoices(IEnumerable<dynamic> results)
	{
		var invoices = results.Select(MapInvoice);

		return invoices.ToList();
	}

	private static IList<InvoiceProduct> MapInvoiceProducts(IEnumerable<dynamic> results)
	{
		var products = results.Select(x => new InvoiceProduct
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
			UnitPrice = ((string)x.UnitPrice).ToMoney(),
			Quantity = (int)x.Quantity,
			DateCreated = x.DateCreated,
			Note = x.Note
		});

		return products.ToList();
	}

	private static IList<Payment> MapPayments(IEnumerable<dynamic> results)
	{
		var payments = results.Select(x => new Payment
		{
			PaymentId = (int)x.PaymentId,
			InvoiceId = (int)x.InvoiceId,
			PaymentTypeId = (int)x.PaymentTypeId,
			Amount = ((string)x.Amount).ToMoney(),
			DateCreated = x.DateCreated,
			Note = x.Note
		});

		return payments.ToList();
	}
}