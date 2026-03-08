using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, OpenTelemetry, service discovery)
builder.AddServiceDefaults();

// Add PostgreSQL with EF Core via Aspire
builder.AddNpgsqlDbContext<StoreHubDbContext>("storehub");

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

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

// Products endpoint (placeholder)
app.MapGet("/products", () =>
{
    return Results.Ok(new { message = "Products endpoint - coming soon" });
});

// Sales endpoint (placeholder)
app.MapPost("/sales/complete", () =>
{
    return Results.Ok(new { message = "Sales complete endpoint - coming soon" });
});

app.Run();
