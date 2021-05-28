using System;
using System.IO;
using System.Data.SQLite;

namespace IndyPOS.DataServices
{
    public class SQLiteDatabase
    {
        protected static string DatabaseFilePath => Environment.CurrentDirectory + @"\Store.db";

        private static readonly string Database = DatabaseFilePath + ";";
        private static readonly string Version = "Version=3;";

        protected static SQLiteConnection GetDatabaseConnection()
        {
            if (!File.Exists(DatabaseFilePath))
                throw new FileNotFoundException("Database file could not be found.");

            return new SQLiteConnection("Data Source=" + Database + Version);
        }
    }
}
