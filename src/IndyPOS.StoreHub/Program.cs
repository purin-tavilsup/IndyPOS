using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.Get;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.ServiceDefaults;
using Nokpirab;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, OpenTelemetry, service discovery)
builder.AddServiceDefaults();

// Add PostgreSQL with EF Core via Aspire
// Connection name must match AppHost: postgres.AddDatabase("storehub-db")
builder.AddNpgsqlDbContext<StoreHubDbContext>("storehub-db");

// Add StoreHub infrastructure services (repositories, store identity)
builder.Services.AddStoreHubServices(builder.Configuration);

// Register StoreHub CQRS handlers manually
// Note: We don't use AddApplicationServices() as it registers ALL handlers including legacy ones
builder.Services.AddTransient<IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>, GetProductsQueryHandler>();
builder.Services.AddTransient<ICommandHandler<CompleteSaleCommand, CompleteSaleResponse>, CompleteSaleCommandHandler>();

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
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Minimal API endpoints
app.MapGet("/", () => "IndyPOS StoreHub API");

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

app.Run();
