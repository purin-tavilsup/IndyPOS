using System.Text;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Auth.Login;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.Get;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Services.StoreHub;
using IndyPOS.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Nokpirab;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, OpenTelemetry, service discovery)
builder.AddServiceDefaults();

// Add PostgreSQL with EF Core via Aspire
// Connection name must match AppHost: postgres.AddDatabase("storehub-db")
builder.AddNpgsqlDbContext<StoreHubDbContext>("storehub-db");

// Add StoreHub infrastructure services (repositories, store identity)
builder.Services.AddStoreHubServices(builder.Configuration);

// Add SyncWorker background service
builder.Services.AddSyncWorker();

// Add auth services
builder.Services.AddStoreHubAuthServices(builder.Configuration);

// Register StoreHub CQRS handlers manually
// Note: We don't use AddApplicationServices() as it registers ALL handlers including legacy ones
builder.Services.AddTransient<IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>, GetProductsQueryHandler>();
builder.Services.AddTransient<ICommandHandler<CompleteSaleCommand, CompleteSaleResponse>, CompleteSaleCommandHandler>();
builder.Services.AddTransient<ICommandHandler<LoginCommand, LoginResponse>, LoginCommandHandler>();

// Add JWT authentication
var tokenOptions = builder.Configuration.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>()
    ?? new LocalTokenOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidateLifetime = true,
               ValidateIssuerSigningKey = true,
               ValidIssuer = tokenOptions.Issuer,
               ValidAudience = tokenOptions.Audience,
               IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOptions.SecretKey)),
               ClockSkew = TimeSpan.FromMinutes(1)
           };
       });

builder.Services.AddAuthorization();

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-create database schema in development
if (app.Environment.IsDevelopment())
{
    await app.EnsureStoreHubDatabaseCreatedAsync();
}

// Map default endpoints (health, alive)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Minimal API endpoints
app.MapGet("/", () => "IndyPOS StoreHub API");

// Auth endpoints
app.MapPost("/auth/login", async (
    ICommandHandler<LoginCommand, LoginResponse> handler,
    LoginRequest request,
    CancellationToken cancellationToken) =>
{
    var command = new LoginCommand(request.Username, request.Password);
    var response = await handler.HandleAsync(command, cancellationToken);

    return response.Success
        ? Results.Ok(response)
        : Results.Unauthorized();
});

app.MapGet("/auth/me", (HttpContext context) =>
{
    var user = context.User;
    if (user.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        userId = user.FindFirst("sub")?.Value,
        username = user.FindFirst("unique_name")?.Value,
        roleId = user.FindFirst("role_id")?.Value,
        storeId = user.FindFirst("store_id")?.Value,
        firstName = user.FindFirst("first_name")?.Value,
        lastName = user.FindFirst("last_name")?.Value
    });
}).RequireAuthorization();

app.MapGet("/health/ready", async (StoreHubDbContext db) =>
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

// Products endpoint
app.MapGet("/products", async (
    IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>> handler,
    bool? activeOnly,
    string? category,
    string? search,
    CancellationToken cancellationToken) =>
{
    var query = new GetProductsQuery(
        ActiveOnly: activeOnly ?? true,
        Category: category,
        SearchTerm: search);

    var products = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(products);
});

// Sales endpoint
app.MapPost("/sales/complete", async (
    ICommandHandler<CompleteSaleCommand, CompleteSaleResponse> handler,
    IStoreIdentityService storeIdentity,
    CompleteSaleRequest request,
    CancellationToken cancellationToken) =>
{
    var command = new CompleteSaleCommand(
        StoreId: storeIdentity.StoreId,
        UserId: request.UserId,
        Lines: request.Lines,
        Payments: request.Payments);

    var response = await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(response);
});

// Sync status endpoint (E4)
app.MapGet("/sync/status", async (
    IOutboxRepository outboxRepository,
    CancellationToken cancellationToken) =>
{
    var pendingCount = await outboxRepository.GetPendingCountAsync(cancellationToken);
    var failedCount = await outboxRepository.GetFailedCountAsync(cancellationToken);

    return Results.Ok(new
    {
        status = pendingCount == 0 ? "synced" : "pending",
        pending = pendingCount,
        failed = failedCount,
        timestamp = DateTime.UtcNow
    });
});

app.Run();
