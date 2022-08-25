using IndyPOS.Common.Interfaces;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace IndyPOS.DataAccess
{
	public class DbConnectionProvider : IDbConnectionProvider
	{
		private readonly string _databasePath;

		public DbConnectionProvider(IConfiguration configuration)
		{
			_databasePath = configuration.DatabasePath;
		}

		public IDbConnection GetDbConnection()
		{
			if (!File.Exists(_databasePath))
				throw new FileNotFoundException("Database file could not be found.");

			return new SQLiteConnection($"Data Source={_databasePath};Version=3;");
		}

		public void BackupDatabase(string backupDatabaseDirectory)
		{
			var backupDbPath = $"{backupDatabaseDirectory}\\Store.db";
			var dbConnection = GetDbConnection();

			if (dbConnection.State != ConnectionState.Closed)
				dbConnection.Close();

			File.Copy(_databasePath, backupDbPath, true);
		}
	}
}
