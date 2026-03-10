using System.Security.Claims;
using System.Text;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using IndyPOS.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
// Default scheme: OpenIddict for M2M (store-to-cloud) auth
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

// Add StoreHub JWT validation for admin endpoints (S3: RBAC)
// IMPORTANT: SecretKey must be configured - fail fast if missing
var localTokenSecretKey = builder.Configuration["LocalToken:SecretKey"]
    ?? throw new InvalidOperationException("LocalToken:SecretKey configuration is required for admin authentication");
var localTokenIssuer = builder.Configuration["LocalToken:Issuer"] ?? "indypos-storehub";
var localTokenAudience = builder.Configuration["LocalToken:Audience"] ?? "indypos-clients";

builder.Services.AddAuthentication()
    .AddJwtBearer("StoreHubJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = localTokenIssuer,
            ValidAudience = localTokenAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(localTokenSecretKey)),
        };
    });

builder.Services.AddAuthorization();

// Add capability-based authorization handler (S3: RBAC)
builder.Services.AddSingleton<IAuthorizationHandler, CapabilityAuthorizationHandler>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("SystemAdminOnly", policy =>
    {
        policy.AuthenticationSchemes.Add("StoreHubJwt");
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new CapabilityRequirement(Capability.AdminStoresRegister));
    });

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

// GET /master/users/{storeId} - Get users for a specific store (S2: Local User Cache)
// Uses version-based sync to avoid clock drift issues
// Requires OAuth2 token - stores can only fetch their own users
app.MapGet("/master/users/{storeId}", [Authorize] async (
    string storeId,
    long? sinceVersion,
    CloudDbContext db,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) =>
{
    // Authorization: Ensure requesting store can only fetch its own users
    var tokenStoreId = user.FindFirst("store_id")?.Value;
    if (tokenStoreId != storeId)
    {
        return Results.Forbid();
    }

    var query = db.Users.Where(u => u.StoreId == storeId);

    if (sinceVersion.HasValue)
    {
        query = query.Where(u => u.Version > sinceVersion.Value);
    }

    var users = await query
        .OrderBy(u => u.Version)
        .Select(u => new CloudUserDto(
            u.Id,
            u.Username,
            u.FirstName,
            u.LastName,
            u.RoleId,
            u.IsActive,
            u.Version))
        .ToListAsync(cancellationToken);

    var maxVersion = users.Count > 0 ? users.Max(u => u.Version) : sinceVersion ?? 0;

    return Results.Ok(new CloudUserSyncResponse(users.Count, users, maxVersion, DateTime.UtcNow));
}).RequireAuthorization();

// ============================================
// Admin Endpoints (F5)
// ============================================

// POST /admin/stores/register - Register a new store with OAuth2 credentials
// Protected by SystemAdminOnly policy (S3: RBAC)
app.MapPost("/admin/stores/register", [Authorize(Policy = "SystemAdminOnly")] async (
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
}).RequireAuthorization("SystemAdminOnly");

app.Run();

// ============================================
// DTOs for user sync (S2: Local User Cache)
// ============================================
public record CloudUserDto(
    Guid Id,
    string Username,
    string FirstName,
    string LastName,
    int RoleId,
    bool IsActive,
    long Version);

public record CloudUserSyncResponse(
    int Count,
    List<CloudUserDto> Users,
    long MaxVersion,
    DateTime Timestamp);
