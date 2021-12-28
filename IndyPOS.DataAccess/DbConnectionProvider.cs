using System.Data;
using System.Data.SQLite;
using System.IO;

namespace IndyPOS.DataAccess
{
    public class DbConnectionProvider : IDbConnectionProvider
	{
		private static string DatabaseFilePath => @"C:\ProgramData\IndyPOS\db\Store.db";
		private static readonly string Database = $"{DatabaseFilePath};";
		private static readonly string Version = "Version=3;";

		public IDbConnection GetDbConnection()
		{
			if (!File.Exists(DatabaseFilePath))
				throw new FileNotFoundException("Database file could not be found.");

			return new SQLiteConnection($"Data Source={Database}{Version}");
		}
	}
}
