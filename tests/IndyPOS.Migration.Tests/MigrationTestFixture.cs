using System.Data.SQLite;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace IndyPOS.Migration.Tests;

/// <summary>
/// Fixture that provides both SQLite (source) and PostgreSQL (target) databases for migration tests.
/// </summary>
public class MigrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("migration_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    private readonly string _sqliteDbPath;
    private Respawner? _respawner;

    public string PostgresConnectionString => _postgresContainer.GetConnectionString();
    public string SqliteConnectionString => $"Data Source={_sqliteDbPath};Version=3;";

    public MigrationTestFixture()
    {
        // Create temp SQLite database
        _sqliteDbPath = Path.Combine(Path.GetTempPath(), $"migration_test_{Guid.NewGuid():N}.db");
    }

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        await _postgresContainer.StartAsync();

        // Create SQLite database with legacy schema
        CreateSqliteSchema();

        // Create PostgreSQL schema
        await CreatePostgresSchemaAsync();

        // Initialize Respawner for PostgreSQL cleanup
        await using var connection = new NpgsqlConnection(PostgresConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"]
        });
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();

        // Clean up SQLite temp file
        if (File.Exists(_sqliteDbPath))
        {
            File.Delete(_sqliteDbPath);
        }
    }

    /// <summary>
    /// Resets both databases to clean state.
    /// </summary>
    public async Task ResetDatabasesAsync()
    {
        // Reset PostgreSQL
        if (_respawner is not null)
        {
            await using var connection = new NpgsqlConnection(PostgresConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }

        // Reset SQLite by dropping and recreating tables
        ResetSqliteDatabase();
    }

    /// <summary>
    /// Gets a fresh StoreHubDbContext for PostgreSQL.
    /// </summary>
    public StoreHubDbContext CreatePostgresDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseNpgsql(PostgresConnectionString)
            .Options;

        return new StoreHubDbContext(options);
    }

    /// <summary>
    /// Gets a SQLite connection for the legacy database.
    /// </summary>
    public SQLiteConnection CreateSqliteConnection()
    {
        return new SQLiteConnection(SqliteConnectionString);
    }

    private void CreateSqliteSchema()
    {
        using var connection = new SQLiteConnection(SqliteConnectionString);
        connection.Open();

        // Create legacy schema matching the production SQLite database
        var schemaScript = """
            -- Users
            CREATE TABLE User (
                UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                RoleId INTEGER NOT NULL,
                DateCreated TEXT NOT NULL,
                DateUpdated TEXT
            );

            CREATE TABLE UserCredential (
                UserId INTEGER PRIMARY KEY,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                DateCreated TEXT NOT NULL,
                DateUpdated TEXT,
                FOREIGN KEY (UserId) REFERENCES User(UserId)
            );

            -- Products
            CREATE TABLE InventoryProduct (
                InventoryProductId INTEGER PRIMARY KEY AUTOINCREMENT,
                Barcode TEXT NOT NULL UNIQUE,
                Description TEXT NOT NULL,
                Manufacturer TEXT,
                Brand TEXT,
                Category INTEGER,
                UnitPrice REAL NOT NULL,
                QuantityInStock INTEGER NOT NULL DEFAULT 0,
                GroupPrice REAL,
                GroupPriceQuantity INTEGER,
                IsTrackable INTEGER NOT NULL DEFAULT 1,
                DateCreated TEXT NOT NULL,
                DateUpdated TEXT
            );

            CREATE TABLE ProductBarcodeCounter (
                Id INTEGER PRIMARY KEY,
                Counter INTEGER NOT NULL DEFAULT 1
            );

            INSERT INTO ProductBarcodeCounter (Id, Counter) VALUES (1, 1);

            -- Invoices
            CREATE TABLE Invoice (
                InvoiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Total REAL NOT NULL,
                DateCreated TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES User(UserId)
            );

            CREATE TABLE InvoiceProduct (
                InvoiceProductId INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                InventoryProductId INTEGER NOT NULL,
                Barcode TEXT NOT NULL,
                Description TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPrice REAL NOT NULL,
                Priority INTEGER NOT NULL DEFAULT 0,
                DateCreated TEXT NOT NULL,
                FOREIGN KEY (InvoiceId) REFERENCES Invoice(InvoiceId),
                FOREIGN KEY (InventoryProductId) REFERENCES InventoryProduct(InventoryProductId)
            );

            CREATE TABLE InvoicePayment (
                InvoicePaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                PaymentTypeId INTEGER NOT NULL,
                Amount REAL NOT NULL,
                Note TEXT,
                DateCreated TEXT NOT NULL,
                FOREIGN KEY (InvoiceId) REFERENCES Invoice(InvoiceId)
            );

            -- PayLater (accounts receivable)
            CREATE TABLE AccountsReceivable (
                AccountsReceivableId INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                ReceivableDescription TEXT NOT NULL,
                ReceivableAmount REAL NOT NULL,
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                DateCreated TEXT NOT NULL,
                DateUpdated TEXT,
                FOREIGN KEY (InvoiceId) REFERENCES Invoice(InvoiceId)
            );

            CREATE TABLE AccountsReceivablePayment (
                AccountsReceivablePaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                AccountsReceivableId INTEGER NOT NULL,
                PaymentAmount REAL NOT NULL,
                DateCreated TEXT NOT NULL,
                FOREIGN KEY (AccountsReceivableId) REFERENCES AccountsReceivable(AccountsReceivableId)
            );

            -- Store Constants
            CREATE TABLE StoreConstant (
                StoreConstantId INTEGER PRIMARY KEY AUTOINCREMENT,
                ConstantKey TEXT NOT NULL UNIQUE,
                ConstantValue TEXT NOT NULL
            );
            """;

        using var command = new SQLiteCommand(schemaScript, connection);
        command.ExecuteNonQuery();
    }

    private void ResetSqliteDatabase()
    {
        using var connection = new SQLiteConnection(SqliteConnectionString);
        connection.Open();

        // Delete all data but keep schema
        var deleteScript = """
            DELETE FROM AccountsReceivablePayment;
            DELETE FROM AccountsReceivable;
            DELETE FROM InvoicePayment;
            DELETE FROM InvoiceProduct;
            DELETE FROM Invoice;
            DELETE FROM InventoryProduct;
            DELETE FROM UserCredential;
            DELETE FROM User;
            UPDATE ProductBarcodeCounter SET Counter = 1 WHERE Id = 1;
            """;

        using var command = new SQLiteCommand(deleteScript, connection);
        command.ExecuteNonQuery();
    }

    private async Task CreatePostgresSchemaAsync()
    {
        await using var dbContext = CreatePostgresDbContext();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
