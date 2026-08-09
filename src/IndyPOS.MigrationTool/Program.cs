using System.CommandLine;
using IndyPOS.MigrationTool;
using IndyPOS.MigrationTool.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

// ReSharper disable AccessToDisposedClosure

// Create root command
var rootCommand = new RootCommand("IndyPOS Migration Tool - Migrate data from SQLite to StoreHub PostgreSQL");

// Create verify subcommand
var verifyCommand = new Command("verify", "Verify migration by comparing SQLite and PostgreSQL data counts");

// Options
var sqliteOption = new Option<FileInfo>(
    aliases: ["--sqlite", "-s"],
    description: "Path to SQLite database file (Store.db)")
{
    IsRequired = true
};

var postgresOption = new Option<string>(
    aliases: ["--postgres", "-p"],
    description: "PostgreSQL connection string for StoreHub")
{
    IsRequired = true
};

var storeIdOption = new Option<string>(
    aliases: ["--store-id", "-i"],
    description: "Store identifier (e.g., STORE-001)")
{
    IsRequired = true
};

var cloudApiOption = new Option<string?>(
    aliases: ["--cloud-api", "-c"],
    description: "Cloud API URL for syncing (optional, e.g., https://api.indypos.cloud)");

var cloudClientIdOption = new Option<string?>(
    aliases: ["--client-id"],
    description: "OAuth2 Client ID for Cloud API");

var cloudClientSecretOption = new Option<string?>(
    aliases: ["--client-secret"],
    description: "OAuth2 Client Secret for Cloud API");

var dryRunOption = new Option<bool>(
    aliases: ["--dry-run", "-d"],
    description: "Validate without making changes",
    getDefaultValue: () => false);

var verboseOption = new Option<bool>(
    aliases: ["--verbose", "-v"],
    description: "Show detailed output",
    getDefaultValue: () => false);

// Add options to command
rootCommand.AddOption(sqliteOption);
rootCommand.AddOption(postgresOption);
rootCommand.AddOption(storeIdOption);
rootCommand.AddOption(cloudApiOption);
rootCommand.AddOption(cloudClientIdOption);
rootCommand.AddOption(cloudClientSecretOption);
rootCommand.AddOption(dryRunOption);
rootCommand.AddOption(verboseOption);

// Handler
rootCommand.SetHandler(async (context) =>
{
    var sqliteFile = context.ParseResult.GetValueForOption(sqliteOption)!;
    var postgresConn = context.ParseResult.GetValueForOption(postgresOption)!;
    var storeId = context.ParseResult.GetValueForOption(storeIdOption)!;
    var cloudApi = context.ParseResult.GetValueForOption(cloudApiOption);
    var clientId = context.ParseResult.GetValueForOption(cloudClientIdOption);
    var clientSecret = context.ParseResult.GetValueForOption(cloudClientSecretOption);
    var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
    var verbose = context.ParseResult.GetValueForOption(verboseOption);

    // Banner
    AnsiConsole.Write(new FigletText("IndyPOS").Color(Color.Blue));
    AnsiConsole.MarkupLine("[bold]Migration Tool[/] - SQLite to StoreHub PostgreSQL\n");

    // Validate inputs
    if (!sqliteFile.Exists)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] SQLite file not found: {sqliteFile.FullName}");
        context.ExitCode = 1;
        return;
    }

    // Display configuration
    var configTable = new Table();
    configTable.AddColumn("Setting");
    configTable.AddColumn("Value");
    configTable.AddRow("SQLite File", sqliteFile.FullName);
    configTable.AddRow("PostgreSQL", postgresConn.Length > 50 ? postgresConn[..50] + "..." : postgresConn);
    configTable.AddRow("Store ID", storeId);
    configTable.AddRow("Cloud API", cloudApi ?? "[grey](not configured)[/]");
    configTable.AddRow("Dry Run", dryRun ? "[yellow]Yes[/]" : "No");
    AnsiConsole.Write(configTable);
    AnsiConsole.WriteLine();

    if (dryRun)
    {
        AnsiConsole.MarkupLine("[yellow]DRY RUN MODE - No changes will be made[/]\n");
    }

    // Setup services
    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
        builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
        builder.AddConsole();
    });

    services.AddSingleton<SqliteMigrationService>();
    services.AddSingleton(new MigrationOptions
    {
        SqlitePath = sqliteFile.FullName,
        PostgresConnectionString = postgresConn,
        StoreId = storeId,
        CloudApiUrl = cloudApi,
        CloudClientId = clientId,
        CloudClientSecret = clientSecret,
        DryRun = dryRun
    });

    var serviceProvider = services.BuildServiceProvider();
    var migrationService = serviceProvider.GetRequiredService<SqliteMigrationService>();

    try
    {
        // Run migration
        var result = await AnsiConsole.Status()
            .StartAsync("Migrating data...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                return await migrationService.MigrateAllAsync();
            });

        // Written before the summary so the summary can print where it went. The clamp list is the
        // only mitigating control for a deliberate data change on real products, and console output
        // alone cannot carry it -- logging is console-only and the run happens inside a live
        // spinner region, so per-product warnings scroll away.
        var clampedReportPath = WriteClampedStockReport(result, sqliteFile);

        // Display results
        AnsiConsole.WriteLine();
        DisplayResults(result, clampedReportPath);

        // Cloud sync if configured
        if (!string.IsNullOrEmpty(cloudApi) && !string.IsNullOrEmpty(clientId) && result.IsSuccess && !dryRun)
        {
            AnsiConsole.MarkupLine("\n[bold]Syncing to Cloud API...[/]");

            var syncResult = await AnsiConsole.Status()
                .StartAsync("Syncing to cloud...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    return await migrationService.SyncToCloudAsync();
                });

            DisplayCloudSyncResults(syncResult);
        }

        context.ExitCode = result.IsSuccess ? 0 : 1;
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Migration failed:[/] {ex.Message}");
        if (verbose)
        {
            AnsiConsole.WriteException(ex);
        }
        context.ExitCode = 1;
    }
});

// Add verify command options
verifyCommand.AddOption(sqliteOption);
verifyCommand.AddOption(postgresOption);
verifyCommand.AddOption(storeIdOption);

verifyCommand.SetHandler(async (context) =>
{
    var sqliteFile = context.ParseResult.GetValueForOption(sqliteOption)!;
    var postgresConn = context.ParseResult.GetValueForOption(postgresOption)!;
    var storeId = context.ParseResult.GetValueForOption(storeIdOption)!;

    AnsiConsole.Write(new FigletText("IndyPOS").Color(Color.Blue));
    AnsiConsole.MarkupLine("[bold]Migration Verification[/]\n");

    if (!sqliteFile.Exists)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] SQLite file not found: {sqliteFile.FullName}");
        context.ExitCode = 1;
        return;
    }

    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    services.AddSingleton<MigrationVerifier>();
    services.AddSingleton(new MigrationOptions
    {
        SqlitePath = sqliteFile.FullName,
        PostgresConnectionString = postgresConn,
        StoreId = storeId,
        DryRun = false
    });

    var serviceProvider = services.BuildServiceProvider();
    var verifier = serviceProvider.GetRequiredService<MigrationVerifier>();

    try
    {
        var result = await AnsiConsole.Status()
            .StartAsync("Verifying migration...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                return await verifier.VerifyAsync();
            });

        DisplayVerificationResults(result);
        context.ExitCode = result.IsValid ? 0 : 1;
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Verification failed:[/] {ex.Message}");
        AnsiConsole.WriteException(ex);
        context.ExitCode = 1;
    }
});

rootCommand.AddCommand(verifyCommand);

return await rootCommand.InvokeAsync(args);

static void DisplayVerificationResults(VerificationResult result)
{
    var table = new Table();
    table.AddColumn("Entity");
    table.AddColumn("SQLite", c => c.RightAligned());
    table.AddColumn("PostgreSQL", c => c.RightAligned());
    table.AddColumn("Status", c => c.Centered());

    foreach (var check in result.Checks)
    {
        var status = check.IsValid ? "[green]✓[/]" : "[red]✗[/]";
        table.AddRow(check.EntityName, check.SqliteCount.ToString(), check.PostgresCount.ToString(), status);
    }

    AnsiConsole.Write(table);

    if (result.IsValid)
    {
        AnsiConsole.MarkupLine("\n[green]✓ Migration verification passed![/]");
    }
    else
    {
        AnsiConsole.MarkupLine("\n[red]✗ Migration verification failed[/]");
        foreach (var error in result.Errors)
        {
            AnsiConsole.MarkupLine($"  [red]•[/] {error}");
        }
    }
}

// Helper methods

/// <summary>
/// Writes every clamped product to a CSV beside the legacy database, and returns its path.
/// Returns null when there is nothing to report, or when the file could not be written -- a failed
/// report must never fail a migration that otherwise succeeded.
/// </summary>
static string? WriteClampedStockReport(MigrationResult result, FileInfo sqliteFile)
{
    if (result.ClampedStocks.Count == 0)
    {
        return null;
    }

    var directory = sqliteFile.DirectoryName ?? Directory.GetCurrentDirectory();
    var path = Path.Combine(
        directory, $"clamped-stock-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

    try
    {
        var lines = new List<string> { "Barcode,ProductName,LegacyQuantity" };
        lines.AddRange(result.ClampedStocks.Select(clamped =>
            $"{CsvField(clamped.Barcode)},{CsvField(clamped.ProductName)},{clamped.LegacyQuantity}"));

        File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
        return path;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        AnsiConsole.MarkupLine(
            $"[yellow]Could not write the clamped-stock report to {path.EscapeMarkup()}: " +
            $"{ex.Message.EscapeMarkup()}[/]");
        return null;
    }
}

/// <summary>Quotes a CSV field. Product names are free text and routinely contain commas.</summary>
static string CsvField(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

static void DisplayResults(MigrationResult result, string? clampedReportPath = null)
{
    // On an aborted run the "Migrated" numbers describe work that was staged in memory and thrown
    // away. The banner below says so, but the table is printed FIRST and must not read as "these
    // rows landed" to anyone skimming.
    var aborted = result.Outcome == MigrationOutcome.Aborted;

    if (aborted)
    {
        AnsiConsole.MarkupLine(
            "[yellow]These counts are work that was staged in memory and then DISCARDED.[/]");
    }

    var resultsTable = new Table();
    resultsTable.AddColumn("Entity");
    resultsTable.AddColumn(aborted ? "Discarded" : "Migrated", c => c.RightAligned());
    resultsTable.AddColumn("Skipped", c => c.RightAligned());
    resultsTable.AddColumn("Failed", c => c.RightAligned());

    resultsTable.AddRow("Users",
        result.Users.Migrated.ToString(),
        result.Users.Skipped.ToString(),
        FormatFailed(result.Users.Failed));

    resultsTable.AddRow("Products",
        result.Products.Migrated.ToString(),
        result.Products.Skipped.ToString(),
        FormatFailed(result.Products.Failed));

    resultsTable.AddRow("Invoices",
        result.Invoices.Migrated.ToString(),
        result.Invoices.Skipped.ToString(),
        FormatFailed(result.Invoices.Failed));

    resultsTable.AddRow("Payments",
        result.Payments.Migrated.ToString(),
        result.Payments.Skipped.ToString(),
        FormatFailed(result.Payments.Failed));

    resultsTable.AddRow("PayLater",
        result.PayLater.Migrated.ToString(),
        result.PayLater.Skipped.ToString(),
        FormatFailed(result.PayLater.Failed));

    AnsiConsole.Write(resultsTable);

    // Placed above the outcome banner deliberately: on an aborted run the "discarded" warning
    // printed before the table covers this list too.
    if (result.ClampedStocks.Count > 0)
    {
        // "would be" on an aborted run: nothing was written, so the clamp has not happened yet.
        var verb = aborted ? "would be migrated as 0" : "were migrated as 0";
        AnsiConsole.MarkupLine(
            $"\n[yellow]{result.ClampedStocks.Count} product(s) had negative stock in the legacy " +
            $"database and {verb}. These need a physical recount:[/]");

        foreach (var clamped in result.ClampedStocks.Take(10))
        {
            AnsiConsole.MarkupLine(
                $"  [yellow]•[/] {clamped.Barcode.EscapeMarkup()} {clamped.ProductName.EscapeMarkup()} " +
                $"([red]{clamped.LegacyQuantity}[/])");
        }

        if (result.ClampedStocks.Count > 10)
        {
            AnsiConsole.MarkupLine(
                $"  [grey]... and {result.ClampedStocks.Count - 10} more[/]");
        }

        // The full list must be reachable: console-only logging inside a spinner cannot carry it.
        AnsiConsole.MarkupLine(clampedReportPath is null
            ? "  [red]The full list could not be written to a file -- copy the above before " +
              "closing this window.[/]"
            : $"  [grey]Full list: {clampedReportPath.EscapeMarkup()}[/]");
    }

    switch (result.Outcome)
    {
        case MigrationOutcome.Success:
            AnsiConsole.MarkupLine("\n[green]✓ Migration completed successfully![/]");
            break;

        case MigrationOutcome.Aborted:
            // Distinct from "completed with errors" on purpose: NOTHING was written. Saying
            // "completed" here would tell the operator their store is mostly migrated.
            AnsiConsole.MarkupLine("\n[red]✗ Migration ABORTED - nothing was written.[/]");
            AnsiConsole.MarkupLine("[red]  The whole run was discarded because a phase failed:[/]");
            foreach (var failure in result.PhaseFailures)
            {
                // SQLite messages are multi-line ("SQL logic error\nno such column: X"), which would
                // break the bullet across lines and lose the phase association.
                var cause = string.Join(" ", failure.Message.Split(
                    ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                AnsiConsole.MarkupLine($"  [red]•[/] [bold]{failure.Phase}[/]: {cause}");
            }
            AnsiConsole.MarkupLine(
                "[grey]  Fix the cause and re-run. The database is untouched.[/]");
            break;

        default:
            AnsiConsole.MarkupLine("\n[red]✗ Migration completed with errors[/]");
            foreach (var error in result.Errors.Take(10))
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {error}");
            }
            if (result.Errors.Count > 10)
            {
                AnsiConsole.MarkupLine($"  [grey]... and {result.Errors.Count - 10} more errors[/]");
            }
            break;
    }
}

static void DisplayCloudSyncResults(CloudSyncResult result)
{
    if (result.IsSuccess)
    {
        AnsiConsole.MarkupLine($"[green]✓ Synced {result.EventsSent} events to cloud[/]");
    }
    else
    {
        AnsiConsole.MarkupLine($"[red]✗ Cloud sync failed: {result.Error}[/]");
    }
}

static string FormatFailed(int count) => count > 0 ? $"[red]{count}[/]" : "0";
