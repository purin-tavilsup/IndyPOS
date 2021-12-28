using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Dapper;
using IndyPOS.DataAccess.Models;
using IndyPOS.DataAccess.Repositories;

namespace IndyPOS.DataAccess.SQLite.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionProvider _dbConnectionProvider;

        public UserRepository(IDbConnectionProvider dbConnectionProvider)
        {
            _dbConnectionProvider = dbConnectionProvider;
        }

		public IEnumerable<User> GetUsers()
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
				connection.Open();

				const string sqlCommand = @"SELECT
                UserId,
                FirstName,
                LastName,
                RoleId,
                DateCreated,
				DateUpdated
                FROM Users";
				
				var results = connection.Query(sqlCommand);

				return MapUsers(results);
			}
		}

		public User GetUserById(int id)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
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

				var results = connection.Query(sqlCommand, sqlParameters);

				return MapUsers(results).FirstOrDefault();
			}
		}

		public int CreateUser(User user)
        {
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
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

				if (userId < 1) throw new Exception("Failed to get the last insert Row ID after adding a user.");

				return userId;
			}
		}

		public void UpdateUser(User user)
        {
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
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
					throw new Exception("Failed to update the user.");
			}
		}

		public UserCredential GetUserCredentialById(int id)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
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

				var result = connection.Query(sqlCommand, sqlParameters).FirstOrDefault();

				return MapUserCredential(result);
			}
		}

		public void CreateUserCredential(int userId, string username, string password)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
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
					throw new Exception("Failed to add user credential.");
			}
		}

		public UserCredential GetUserCredentialByUsername(string username)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
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

				var result = connection.Query(sqlCommand, sqlParameters).FirstOrDefault();

				return MapUserCredential(result);
			}
		}

		public void UpdateUserCredentialById(int userId, string password)
        {
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
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
					throw new Exception("Failed to update user credential.");
			}
		}

		private UserCredential MapUserCredential(dynamic result)
		{
			var credential = new UserCredential
							 {
								 UserId = (int) result.UserId,
								 Username = result.Username,
								 Password = result.Password,
								 DateCreated = result.DateCreated,
								 DateUpdated = result.DateUpdated
							 };

			return credential;
		}

		private IList<User> MapUsers(IEnumerable<dynamic> results)
		{
			var users = results?.Select(x => new User
												{
													UserId = (int)x.UserId,
													FirstName = x.FirstName,
													LastName = x.LastName,
													RoleId = (int)x.RoleId,
													DateCreated = x.DateCreated,
													DateUpdated = x.DateUpdated
												}) ?? Enumerable.Empty<User>();
			
			return users.ToList();
		}
    }
}
