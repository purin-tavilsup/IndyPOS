using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Nokpirab;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, OpenTelemetry, service discovery)
builder.AddServiceDefaults();

// Add PostgreSQL with EF Core via Aspire
// Connection name must match AppHost: postgres.AddDatabase("cloud-db")
builder.AddNpgsqlDbContext<CloudDbContext>("cloud-db");

// Add Cloud infrastructure services
builder.Services.AddScoped<ISyncedEventRepository, DbSyncedEventRepository>();

// Add EventProcessor background service
builder.Services.AddHostedService<EventProcessor>();

// Register Cloud CQRS handlers
builder.Services.AddTransient<ICommandHandler<IngestEventsCommand, SyncEventsResponse>, IngestEventsCommandHandler>();

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-create database schema in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Map default endpoints (health, alive)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Minimal API endpoints
app.MapGet("/", () => "IndyPOS Cloud API");

// Sync endpoint - idempotent event ingestion (F2)
app.MapPost("/sync/events", async (
    ICommandHandler<IngestEventsCommand, SyncEventsResponse> handler,
    SyncEventsRequest request,
    CancellationToken cancellationToken) =>
{
    var command = new IngestEventsCommand(request.Events);
    var response = await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(response);
});

// Sync status endpoint
app.MapGet("/sync/status", async (CloudDbContext db, CancellationToken cancellationToken) =>
{
    var totalEvents = await db.SyncedEvents.CountAsync(cancellationToken);
    var unprocessedEvents = await db.SyncedEvents.CountAsync(e => e.ProcessedAtUtc == null, cancellationToken);
    var processedEvents = await db.ProcessedEvents.CountAsync(cancellationToken);
    var totalInvoices = await db.Invoices.CountAsync(cancellationToken);

    return Results.Ok(new
    {
        status = "running",
        storage = "postgresql",
        totalEvents,
        unprocessedEvents,
        processedEvents,
        totalInvoices,
        timestamp = DateTime.UtcNow
    });
});

// Health/ready endpoint with database check
app.MapGet("/health/ready", async (CloudDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", database = "connected" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database connection failed: {ex.Message}");
    }
});

app.Run();
