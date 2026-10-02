using Npgsql;
using Testcontainers.PostgreSql;

namespace IndyPOS.Testing.Postgres;

/// <summary>
/// An empty PostgreSQL database for one test fixture, from whichever server the environment offers.
/// </summary>
/// <remarks>
/// By default it starts a throwaway <c>postgres:16-alpine</c> container. That cannot work on a
/// GitHub Actions Windows runner, which cannot run Linux containers, so CI points
/// <see cref="ServerVariable"/> at the PostgreSQL service preinstalled on the runner. Each fixture
/// then gets its own freshly created database on that shared server, which keeps the isolation a
/// private container used to give.
/// </remarks>
public sealed class TestPostgres : IAsyncDisposable
{
    /// <summary>
    /// Full Npgsql connection string of a server that may create and drop databases. When unset,
    /// a Docker container is used instead.
    /// </summary>
    public const string ServerVariable = "INDYPOS_TEST_POSTGRES";

    private const string DatabasePrefix = "indypos_test_";

    private readonly PostgreSqlContainer? _container;
    private readonly string _adminConnectionString;
    private readonly List<string> _createdDatabases = [];

    private TestPostgres(string adminConnectionString, PostgreSqlContainer? container)
    {
        _adminConnectionString = adminConnectionString;
        _container = container;
        ConnectionString = adminConnectionString;
    }

    /// <summary>Connection string of this instance's own empty database.</summary>
    public string ConnectionString { get; private set; }

    public static async Task<TestPostgres> StartAsync(CancellationToken cancellationToken = default)
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable);

        if (string.IsNullOrWhiteSpace(server))
        {
            return await StartContainerAsync(cancellationToken);
        }

        var instance = new TestPostgres(server, container: null);
        instance.ConnectionString = await instance.CreateDatabaseAsync(cancellationToken);

        return instance;
    }

    /// <summary>
    /// Creates a further empty database on the same server, for fixtures that need more than one.
    /// It is dropped when this instance is disposed.
    /// </summary>
    public async Task<string> CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var name = $"{DatabasePrefix}{Guid.NewGuid():N}";
        await ExecuteAdminAsync($"CREATE DATABASE {name}", cancellationToken);
        _createdDatabases.Add(name);

        return new NpgsqlConnectionStringBuilder(_adminConnectionString) { Database = name }.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        // Pooled connections would make DROP DATABASE wait on idle sessions, so close them first.
        NpgsqlConnection.ClearAllPools();

        try
        {
            // A container takes its databases with it; only a shared server needs cleaning.
            if (_container is null)
            {
                await DropDatabasesBestEffortAsync();
            }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();

            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }
    }

    private async Task DropDatabasesBestEffortAsync()
    {
        foreach (var name in _createdDatabases)
        {
            try
            {
                await ExecuteAdminAsync($"DROP DATABASE IF EXISTS {name} WITH (FORCE)", CancellationToken.None);
            }
            catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
            {
                // Cleanup must not fail a test run; one stuck database should not block the others.
                Console.Error.WriteLine($"TestPostgres: could not drop {name}: {exception.Message}");
            }
        }
    }

    private static async Task<TestPostgres> StartContainerAsync(CancellationToken cancellationToken)
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await container.StartAsync(cancellationToken);

        return new TestPostgres(container.GetConnectionString(), container);
    }

    private async Task ExecuteAdminAsync(string sql, CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString);

        // Without a database Npgsql would target one named after the user, which may not exist.
        if (string.IsNullOrEmpty(builder.Database))
        {
            builder.Database = "postgres";
        }

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
