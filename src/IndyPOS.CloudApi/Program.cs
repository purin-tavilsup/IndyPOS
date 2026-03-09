using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.ServiceDefaults;
using Nokpirab;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, OpenTelemetry, service discovery)
builder.AddServiceDefaults();

// Add Cloud infrastructure services
// Register as singleton so in-memory state persists across requests
var eventRepository = new InMemorySyncedEventRepository();
builder.Services.AddSingleton<ISyncedEventRepository>(eventRepository);
builder.Services.AddSingleton(eventRepository); // Also register concrete type for status endpoint

// Register Cloud CQRS handlers
builder.Services.AddTransient<ICommandHandler<IngestEventsCommand, SyncEventsResponse>, IngestEventsCommandHandler>();

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

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
app.MapGet("/sync/status", (InMemorySyncedEventRepository repository) =>
{
    return Results.Ok(new
    {
        status = "running",
        storage = "in-memory",
        totalEvents = repository.GetTotalCount(),
        unprocessedEvents = repository.GetUnprocessedCount(),
        timestamp = DateTime.UtcNow
    });
});

app.Run();
