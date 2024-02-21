using Dapper;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Infrastructure.Persistence.Repositories.SQLite;

public class UserCredentialRepository : IUserCredentialRepository
{
	private readonly IDbConnectionProvider _dbConnectionProvider;

	public UserCredentialRepository(IDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public UserCredential GetById(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM UserCredential WHERE UserId = @userId";

        var sqlParameters = new
        {
            userId = id
        };

        var result = connection.Query<UserCredential>(sqlCommand, sqlParameters)
                               .FirstOrDefault();

		if (result is null)
		{
			throw new UserCredentialNotFoundException($"Could not find User Credential by ID: {id}");
		}

        return result;
    }

    public bool Add(UserCredential userCredential)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"INSERT INTO UserCredential
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
			userCredential.UserId,
			userCredential.Username,
			userCredential.Password
        };

        var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

        return affectedRowsCount == 1;
    }

    public UserCredential GetByUsername(string username)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM UserCredential WHERE Username = @Username";

        var sqlParameters = new
        {
            Username = username
        };

        var result = connection.Query<UserCredential>(sqlCommand, sqlParameters)
                               .FirstOrDefault();

		if (result is null)
		{
			throw new UserCredentialNotFoundException($"Could not find User Credential by Username: {username}");
		}

        return result;
    }

    public bool UpdatePassword(UserCredential userCredential)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"UPDATE UserCredential
                SET
                    Password = @Password,
                    DateUpdated = datetime('now','localtime')
                WHERE UserId = @UserId";

        var sqlParameters = new
        {
            userCredential.UserId,
            userCredential.Password
        };

        var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

        return affectedRowsCount == 1;
    }

    public bool RemoveById(int userId)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"DELETE FROM UserCredential WHERE UserId = @UserId";

        var sqlParameters = new
        {
            UserId = userId
        };

        var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

        return affectedRowsCount == 1;
    }
}