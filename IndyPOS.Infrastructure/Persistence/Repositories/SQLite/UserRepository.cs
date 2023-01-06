using Dapper;
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

        return results ?? Enumerable.Empty<UserAccount>();
    }

    public UserAccount? GetUserById(int id)
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

        return userId;
    }

    public bool UpdateUser(UserAccount user)
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

    public bool RemoveUserById(int id)
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

        return affectedRowsCount == 1;
    }

    public UserCredential? GetUserCredentialById(int id)
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

    public bool CreateUserCredential(int userId, string username, string password)
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

    public UserCredential? GetUserCredentialByUsername(string username)
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

    public bool UpdateUserCredentialById(int userId, string password)
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

    public bool RemoveUserCredentialById(int userId)
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