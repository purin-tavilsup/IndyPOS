using Dapper;
using IndyPOS.Common.Exceptions;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite;

public class UserRepository : IUserRepository
{
	private readonly IDbConnectionProvider _dbConnectionProvider;

	public UserRepository(IDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public IEnumerable<UserAccount> GetUsers()
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
                UserId,
                FirstName,
                LastName,
                RoleId,
                DateCreated,
				DateUpdated
                FROM Users";
				
		var results = connection.Query<UserAccount>(sqlCommand);

		return results;
	}

	public UserAccount GetUserById(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
                UserId,
                FirstName,
                LastName,
                RoleId,
                DateCreated,
				DateUpdated
                FROM Users
				WHERE UserId = @userId";

		var sqlParameters = new
		{
			userId = id
		};

		var result = connection.Query<UserAccount>(sqlCommand, sqlParameters)
							   .FirstOrDefault();

		if (result == null)
			throw new UserNotFoundException($"User is not found. UserId: {id}.");

		return result;
	}

	public int CreateUser(UserAccount user)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"INSERT INTO Users
                (
                    FirstName,
                    LastName,
					RoleId,
                    DateCreated
                )
                VALUES
                (
                    @FirstName,
                    @LastName,
					@RoleId,
                    datetime('now','localtime')
                );
                SELECT last_insert_rowid()";

		var sqlParameters = new
		{
			user.FirstName,
			user.LastName,
			user.RoleId
		};

		var userId = connection.Query<int>(sqlCommand, sqlParameters).FirstOrDefault();

		if (userId < 1) 
			throw new UserNotCreatedException("Failed to add a new user.");

		return userId;
	}

	public void UpdateUser(UserAccount user)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"UPDATE Users
                SET
                    FirstName = @FirstName,
                    LastName = @LastName,
                    DateUpdated = datetime('now','localtime')
                WHERE UserId = @UserId";

		var sqlParameters = new
		{
			user.UserId,
			user.FirstName,
			user.LastName
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new UserNotUpdatedException($"Failed to update a user. UserId: {user.UserId}.");
	}

	public void RemoveUserById(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"DELETE FROM Users
                WHERE UserId = @UserId";

		var sqlParameters = new
		{
			UserId = id
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new UserNotDeletedException($"Failed to delete a user. UserId: {id}.");
	}

	public UserCredential GetUserCredentialById(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
                UserId,
				Username,
                Password,
                DateCreated,
				DateUpdated
                FROM UserCredentials
				WHERE UserId = @userId";

		var sqlParameters = new
		{
			userId = id
		};

		var result = connection.Query<UserCredential>(sqlCommand, sqlParameters)
							   .FirstOrDefault();

		if (result is null)
			throw new UserCredentialNotFoundException($"User Credential is not found. UserId: {id}.");

		return result;
	}

	public void CreateUserCredential(int userId, string username, string password)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"INSERT INTO UserCredentials
                (
                    UserId,
					Username,
                    Password,
                    DateCreated
                )
                VALUES
                (
                    @UserId,
					@Username,
                    @Password,
                    datetime('now','localtime')
                );";

		var sqlParameters = new
		{
			UserId = userId,
			Username = username,
			Password = password
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new UserCredentialNotCreatedException($"Failed to add a new user credential. UserId: {userId}.");
	}

	public UserCredential GetUserCredentialByUsername(string username)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
                UserId,
				Username,
                Password,
                DateCreated,
				DateUpdated
                FROM UserCredentials
				WHERE Username = @Username";

		var sqlParameters = new
		{
			Username = username
		};

		var result = connection.Query<UserCredential>(sqlCommand, sqlParameters)
							   .FirstOrDefault();

		if (result is null)
			throw new UserCredentialNotFoundException($"User Credential is not found. Username: {username}.");

		return result;
	}

	public void UpdateUserCredentialById(int userId, string password)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"UPDATE UserCredentials
                SET
                    Password = @Password,
                    DateUpdated = datetime('now','localtime')
                WHERE UserId = @UserId";

		var sqlParameters = new
		{
			UserId = userId,
			Password = password
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new UserCredentialNotUpdatedException($"Failed to update a user credential. UserId: {userId}.");
	}

	public void RemoveUserCredentialById(int userId)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"DELETE FROM UserCredentials
                WHERE UserId = @UserId";

		var sqlParameters = new
		{
			UserId = userId
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new UserCredentialNotDeletedException($"Failed to delete a user credential. UserId: {userId}.");
	}
}