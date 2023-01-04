using Dapper;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite;

public class StoreConstantRepository : IStoreConstantRepository
{
	private readonly IDbConnectionProvider _dbConnectionProvider;

	public StoreConstantRepository(IDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public IEnumerable<PaymentType> GetPaymentTypes()
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT * FROM [PaymentTypes]";

		var results = connection.Query<PaymentType>(sqlCommand, new DynamicParameters());

		return results ?? Enumerable.Empty<PaymentType>();
	}

	public IEnumerable<UserRole> GetUserRoles()
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT * FROM [UserRoles]";

		var results = connection.Query<UserRole>(sqlCommand, new DynamicParameters());

		return results ?? Enumerable.Empty<UserRole>();
	}

	public IEnumerable<ProductCategory> GetProductCategories()
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT * FROM [ProductCategories]";

		var results = connection.Query<ProductCategory>(sqlCommand, new DynamicParameters());

		return results ?? Enumerable.Empty<ProductCategory>();
	}
}