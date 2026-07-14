# .NET Aspire Plan

Version: 1.3.0
Updated: 2026-02-28

## Decision summary

IndyPOS will add **.NET Aspire** to the solution for **development orchestration**.

Aspire will manage:
- Cloud API
- Sync Worker
- PostgreSQL
- future services such as MCP API / reporting worker / Redis

Aspire will **not** replace Docker for production deployment.

## Recommended solution structure

```text
src/
  IndyPOS.AppHost
  IndyPOS.ServiceDefaults
  IndyPOS.CloudApi
  IndyPOS.SyncWorker
  IndyPOS.StoreHub
  IndyPOS.Domain
  IndyPOS.Application
  IndyPOS.Infrastructure
```

## Example: AppHost setup

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var cloudDb = postgres.AddDatabase("indypos_cloud");

builder.AddProject<Projects.IndyPOS_CloudApi>("cloud-api")
       .WithReference(cloudDb);

builder.AddProject<Projects.IndyPOS_SyncWorker>("sync-worker")
       .WithReference(cloudDb);

builder.Build().Run();
```

## Example: ServiceDefaults

```csharp
public static class Extensions
{
    public static TBuilder AddIndyServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddOpenTelemetry()
            .WithTracing(_ => { })
            .WithMetrics(_ => { });

        builder.Services.AddHealthChecks();

        return builder;
    }
}
```

## Production boundary

Use Aspire for:
- local development
- integration testing
- observability during development

Use Docker / DigitalOcean for:
- production runtime
- deployment
- scaling
