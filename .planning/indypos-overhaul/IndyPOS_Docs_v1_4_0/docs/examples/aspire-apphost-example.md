# Example Implementation: .NET Aspire in IndyPOS

Version: 1.3.0
Updated: 2026-02-28

## Create AppHost + ServiceDefaults

```bash
dotnet new aspire-apphost -n IndyPOS.AppHost
dotnet new aspire-servicedefaults -n IndyPOS.ServiceDefaults
```

## Example AppHost Program.cs

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

## Developer workflow

```bash
dotnet run --project src/IndyPOS.AppHost
```
