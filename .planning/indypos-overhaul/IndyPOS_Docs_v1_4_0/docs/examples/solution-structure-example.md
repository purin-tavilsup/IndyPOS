# Example Solution Structure Mapping

Version: 1.4.0
Updated: 2026-02-28

## Suggested near-term mapping from current IndyPOS code

Current likely core:
- `IndyPOS.Domain`
- `IndyPOS.Application`
- `IndyPOS.Infrastructure`
- `IndyPOS.Windows.Forms`

Add next:
- `IndyPOS.StoreHub`
- `IndyPOS.CloudApi`
- `IndyPOS.AppHost`
- `IndyPOS.ServiceDefaults`

Optional later:
- `IndyPOS.SyncWorker`
- `IndyPOS.McpServer`

## Example command sequence

```bash
dotnet new webapi -n IndyPOS.StoreHub
dotnet new webapi -n IndyPOS.CloudApi
dotnet new aspire-apphost -n IndyPOS.AppHost
dotnet new aspire-servicedefaults -n IndyPOS.ServiceDefaults
```

## Example AppHost registration

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
