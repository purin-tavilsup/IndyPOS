using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SQLite;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Infrastructure.Persistence.Repositories.SQLite;

public class DbConnectionProvider : IDbConnectionProvider
{
    private readonly string _databasePath;

    public DbConnectionProvider(IConfiguration configuration)
    {
        _databasePath = GetDatabasePath(configuration);
    }

    private static string GetDatabasePath(IConfiguration configuration)
    {
        var path = configuration.GetValue<string>("Database:Path");

        return path ?? "C:\\ProgramData\\IndyPOS\\db\\Store.db";
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