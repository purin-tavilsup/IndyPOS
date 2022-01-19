using Dapper;
using IndyPOS.DataAccess.Models;
using IndyPOS.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.DataAccess.SQLite.Repositories
{
    public class AccountsReceivableRepository : IAccountsReceivableRepository
    {
		private readonly IDbConnectionProvider _dbConnectionProvider;

		public AccountsReceivableRepository(IDbConnectionProvider dbConnectionProvider)
		{
			_dbConnectionProvider = dbConnectionProvider;
		}

		public int AddAccountsReceivable(AccountsReceivable accountsReceivable)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
				connection.Open();

				const string sqlCommand = @"INSERT INTO AccountsReceivables
                (
                    PaymentId,
                    Description,
                    InvoiceId,
                    ReceivableAmount,
					DateCreated
                )
                VALUES
                (
                    @PaymentId,
                    @Description,
                    @InvoiceId,
					@ReceivableAmount,
                    datetime('now','localtime')
                );
                SELECT last_insert_rowid()";

				var sqlParameters = new
									{
										accountsReceivable.PaymentId,
										accountsReceivable.Description,
										accountsReceivable.InvoiceId,
										ReceivableAmount = MapMoneyToString(accountsReceivable.ReceivableAmount)
									};

				var rowId = connection.Query<int>(sqlCommand, sqlParameters).FirstOrDefault();

				if (rowId < 1) throw new Exception("Failed to get the last insert Row ID after adding a new account receivable.");

				return rowId;
			}
		}

		public void UpdateAccountsReceivable(AccountsReceivable accountsReceivable)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
				connection.Open();

				const string sqlCommand = @"UPDATE AccountsReceivables
                SET
                    PaidAmount = @PaidAmount,
                    IsCompleted = @IsCompleted,
                    DateUpdated = datetime('now','localtime')
                WHERE PaymentId = @PaymentId";

				var sqlParameters = new
									{
										accountsReceivable.PaymentId,
										PaidAmount = MapMoneyToString(accountsReceivable.PaidAmount),
										IsCompleted = accountsReceivable.IsCompleted ? 1 : 0
									};

				var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

				if (affectedRowsCount != 1)
					throw new Exception("Failed to update the account receivable.");
			}
		}

		public IEnumerable<AccountsReceivable> GetAccountsReceivables()
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
				connection.Open();

				const string sqlCommand = @"SELECT
				PaymentId,
				Description,
				InvoiceId,
				ReceivableAmount,
				PaidAmount,
				IsCompleted,
				DateCreated,
				DateUpdated
                FROM AccountsReceivables";

				var results = connection.Query(sqlCommand);

				return MapAccountsReceivables(results);
			}
		}

		public AccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
				connection.Open();

				const string sqlCommand = @"SELECT
				PaymentId,
				Description,
				InvoiceId,
				ReceivableAmount,
				PaidAmount,
				IsCompleted,
				DateCreated,
				DateUpdated
                FROM AccountsReceivables
				WHERE InvoiceId = @InvoiceId";

				var sqlParameters = new
									{
										InvoiceId = invoiceId
									};

				var results = connection.Query(sqlCommand, sqlParameters);

				return MapAccountsReceivables(results).FirstOrDefault();
			}
		}

		private static IList<AccountsReceivable> MapAccountsReceivables(IEnumerable<dynamic> results)
		{
			var accountsReceivables = results?.Select(x => new AccountsReceivable
														   {
															   PaymentId = (int)x.PaymentId,
															   Description = x.Description,
															   InvoiceId = (int)x.InvoiceId,
															   ReceivableAmount = MapMoneyToDecimal(x.ReceivableAmount),
															   PaidAmount = MapMoneyToDecimal(x.PaidAmount),
															   IsCompleted = x.IsCompleted == 1,
															   DateCreated = x.DateCreated,
															   DateUpdated = x.DateUpdated
														   }) ?? Enumerable.Empty<AccountsReceivable>();

			return accountsReceivables.ToList();
        }

        private static decimal MapMoneyToDecimal(string value)
		{
			if (decimal.TryParse(value.Trim(), out var result))
				return result / 100m;

			return 0m;
		}

		private static string MapMoneyToString(decimal? value)
		{
			if (!value.HasValue)
				return null;

			var result = Math.Round(value.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero) * 100m;

			return $"{result}";
		}
    }
}
