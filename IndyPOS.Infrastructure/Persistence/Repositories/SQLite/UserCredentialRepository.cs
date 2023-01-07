using Dapper;
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

	public UserCredential? GetById(int id)
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

        return result;
    }

    public bool Add(int userId, string username, string password)
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

        return affectedRowsCount == 1;
    }

    public UserCredential? GetByUsername(string username)
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

        return result;
    }

    public bool UpdatePasswordById(int userId, string password)
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

        return affectedRowsCount == 1;
    }

    public bool RemoveById(int userId)
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

        return affectedRowsCount == 1;
    }
}