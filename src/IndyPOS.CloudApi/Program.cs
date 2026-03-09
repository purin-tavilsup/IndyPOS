using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using IndyPOS.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nokpirab;
using OpenIddict.Validation.AspNetCore;
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
builder.Services.AddTransient<ICommandHandler<RegisterStoreCommand, RegisterStoreResponse>, RegisterStoreHandler>();

// Add OpenIddict OAuth2 server
builder.Services.AddOpenIddictServer(builder.Configuration);

// Add authentication & authorization
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

// Add controllers for token endpoint
builder.Services.AddControllers();

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

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers (for TokenController)
app.MapControllers();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Minimal API endpoints
app.MapGet("/", () => "IndyPOS Cloud API");

// Sync endpoint - idempotent event ingestion (F2)
// Requires OAuth2 token with sync.write scope
app.MapPost("/sync/events", [Authorize] async (
    ICommandHandler<IngestEventsCommand, SyncEventsResponse> handler,
    SyncEventsRequest request,
    CancellationToken cancellationToken) =>
{
    var command = new IngestEventsCommand(request.Events);
    var response = await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(response);
}).RequireAuthorization();

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

// ============================================
// Master Data Endpoints (F4)
// ============================================

// GET /master/products - Get all active products for store sync
// Requires OAuth2 token with master.read scope
app.MapGet("/master/products", [Authorize] async (
    CloudDbContext db,
    bool? activeOnly,
    DateTime? modifiedSince,
    CancellationToken cancellationToken) =>
{
    var query = db.Products.AsQueryable();

    if (activeOnly ?? true)
    {
        query = query.Where(p => p.IsActive);
    }

    if (modifiedSince.HasValue)
    {
        query = query.Where(p => p.LastModifiedAtUtc > modifiedSince.Value);
    }

    var products = await query
        .OrderBy(p => p.Name)
        .Select(p => new
        {
            p.Id,
            p.Barcode,
            p.Name,
            p.Description,
            p.Manufacturer,
            p.Brand,
            p.Category,
            p.UnitPrice,
            p.GroupPrice,
            p.GroupPriceQuantity,
            p.IsActive,
            p.LastModifiedAtUtc
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new
    {
        count = products.Count,
        products,
        timestamp = DateTime.UtcNow
    });
}).RequireAuthorization();

// GET /master/config/{storeId} - Get store-specific configuration
// Requires OAuth2 token with master.read scope
app.MapGet("/master/config/{storeId}", [Authorize] async (
    string storeId,
    CloudDbContext db,
    CancellationToken cancellationToken) =>
{
    var config = await db.StoreConfigs.FindAsync([storeId], cancellationToken);

    if (config is null)
    {
        return Results.NotFound(new { error = $"Store config not found for storeId: {storeId}" });
    }

    return Results.Ok(new
    {
        config.StoreId,
        config.StoreName,
        config.StoreFullName,
        config.AddressLine1,
        config.AddressLine2,
        config.PhoneNumber,
        config.PrinterName,
        config.LastModifiedAtUtc,
        timestamp = DateTime.UtcNow
    });
}).RequireAuthorization();

// ============================================
// Admin Endpoints (F5)
// ============================================

// POST /admin/stores/register - Register a new store with OAuth2 credentials
// Note: In production, this should be protected by admin authentication
app.MapPost("/admin/stores/register", async (
    ICommandHandler<RegisterStoreCommand, RegisterStoreResponse> handler,
    RegisterStoreRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var command = new RegisterStoreCommand(
            StoreId: request.StoreId,
            StoreName: request.StoreName,
            StoreFullName: request.StoreFullName,
            AddressLine1: request.AddressLine1,
            AddressLine2: request.AddressLine2,
            PhoneNumber: request.PhoneNumber,
            PrinterName: request.PrinterName);

        var response = await handler.HandleAsync(command, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.Run();
