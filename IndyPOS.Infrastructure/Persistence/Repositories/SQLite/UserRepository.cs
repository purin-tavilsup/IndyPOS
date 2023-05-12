using Dapper;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Infrastructure.Persistence.Repositories.SQLite;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionProvider _dbConnectionProvider;

    public UserRepository(IDbConnectionProvider dbConnectionProvider)
    {
        _dbConnectionProvider = dbConnectionProvider;
    }

    public IEnumerable<UserAccount> GetAll()
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM Users";

        var results = connection.Query<UserAccount>(sqlCommand);

        return results ?? Enumerable.Empty<UserAccount>();
    }

    public UserAccount GetById(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM Users WHERE UserId = @userId";

        var sqlParameters = new
        {
            userId = id
        };

        var result = connection.Query<UserAccount>(sqlCommand, sqlParameters)
                               .FirstOrDefault();

		if (result is null)
		{
			throw new UserNotFoundException($"Could not find User by ID: {id}");
		}

        return result;
    }

    public int Add(UserAccount user)
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

        return userId;
    }

    public bool Update(UserAccount user)
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

        return affectedRowsCount == 1;
    }

    public bool RemoveById(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"DELETE FROM Users WHERE UserId = @UserId";

        var sqlParameters = new
        {
            UserId = id
        };

        var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

        return affectedRowsCount == 1;
    }
}