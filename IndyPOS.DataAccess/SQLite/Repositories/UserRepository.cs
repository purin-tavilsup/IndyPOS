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

		public IList<User> GetUsers()
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

		public User GetUserByUserId(int id)
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

		public int AddUser(User user)
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

		public UserCredential GetUserCredentialByUserId(int id)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
				connection.Open();

				const string sqlCommand = @"SELECT
                UserId,
                Password,
                DateCreated,
				DateUpdated,
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

		public void AddUserCredential(int userId, string encryptedSecret)
		{
			using (var connection = _dbConnectionProvider.GetDbConnection())
			{
				connection.Open();

				const string sqlCommand = @"INSERT INTO UserCredentials
                (
                    UserId,
                    Password,
                    DateCreated
                )
                VALUES
                (
                    @UserId,
                    @Password,
                    datetime('now','localtime')
                );";

				var sqlParameters = new
				{
					UserId = userId,
					Password = encryptedSecret
				};

				var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

				if (affectedRowsCount != 1)
					throw new Exception("Failed to add user credential.");
			}
		}

		public void UpdateUserCredential(int userId, string encryptedSecret)
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
					Password = encryptedSecret
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
