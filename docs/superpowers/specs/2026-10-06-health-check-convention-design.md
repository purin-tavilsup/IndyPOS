# Health-Check Convention — Design

**Date:** 2026-10-06 · **Status:** approved in conversation (Pond), awaiting written-spec review
**Scope:** `IndyPOS.ServiceDefaults`, StoreHub, CloudApi, AppHost, and the WinForms till's health callers. One PR.

## 1. Why

Pond asked for IndyPOS health checks to follow two references:
- Milan Jovanović, *Health Checks in ASP.NET Core* (milanjovanovic.tech/blog/health-checks-in-asp-net-core);
- Aspire, *Health checks* (aspire.dev/fundamentals/health-checks).

Checking the code against them found two real bugs and two gaps. Every item below was **observed on a
running system**, not reasoned about.

1. **The till calls a route that production does not have.**
   - `StoreHubUpdateService` (`StoreHubHealthUrl`) and `FirstRunWizard.TestConnectionButton_Click` call
     `http://localhost:5000/health`.
   - `/health` is mapped only in Development (`ServiceDefaults/Extensions.cs`).
   - Probed on 2026-10-06 against the installed StoreHub service (v4.0.0, port 5000): `/health` → **404**;
     `/health/live` and `/health/ready` → 200.
   - So on every installed till, the post-update health check always logs a failure, and the wizard's
     "Test connection" always reports `NotFound`.
2. **CloudApi maps `/health/ready` twice** (defect I0-C, recorded 2026-08-20, never resolved).
   - `MapDefaultEndpoints()` maps it, and so does a hand-rolled `MapGet("/health/ready")` in
     `CloudApi/Program.cs`.
   - The hand-rolled copy returns `ex.Message` in its failure body, which leaks database error text
     from an anonymous endpoint.
3. **No integration test touches any health route** in StoreHub or CloudApi.
4. **Aspire's AppHost does not wait for real readiness.** `WaitFor` only covers Postgres and the project
   start. On 2026-10-06 the Aspire dashboard reported StoreHub's endpoints "Ready" for a process that had
   already exited.

Already right, and kept:
- tag-filtered `/health/live` and `/health/ready`;
- StoreHub's database readiness check;
- the installer's `HealthProbe` and `verify-install.ps1` already use `/health/ready`;
- the CloudApi container `HEALTHCHECK` uses `/health/live` on purpose.

## 2. Decisions (Pond, 2026-10-06)

- **Production probes are terse.** Health *details* (per-check status, timings, errors) are for
  Development only. Production `/health/live` and `/health/ready` stay anonymous and say only
  `Healthy`/`Unhealthy` with 200/503. Anonymous endpoints leak nothing.
- **Approach C:** follow the article's packages.
  - The database readiness check is Xabaril's `AspNetCore.HealthChecks.NpgSql` (`SELECT 1`).
  - The detailed JSON writer is Xabaril's `AspNetCore.HealthChecks.UI.Client`.
- **Left out (YAGNI):**
  - output caching on probes (StoreHub listens on localhost only, and Cloud Run probes rarely);
  - a health UI dashboard;
  - authenticated health detail in production.

## 3. The contract

| Route | Environments | Checks run | Response |
|---|---|---|---|
| `/health/live` | all | `self` (tag `live`) | terse text, 200 or 503 |
| `/health/ready` | all | `database` (tag `ready`) | terse text, 200 or 503 |
| `/health` | **Development only** | every registered check | detailed JSON (`UIResponseWriter`) |

- **Route shapes do not change.** Every caller that works today keeps working.
- **Production probes time out.** `/health/live` and `/health/ready` carry a **5-second request
  timeout** (`RequestTimeouts`, a named policy in `ServiceDefaults`), so a hung dependency cannot stall
  the installer, the till or a container runtime. The `database` check's own timeout is **3 seconds**,
  inside that budget, so a hung database reports **503**, not a request timeout.
- **Callers:**

| Caller | Route | Change |
|---|---|---|
| Installer `HealthProbe` (fresh install + upgrade) | `/health/ready` | none |
| `scripts/verify-install.ps1` | `/health/live`, `/health/ready` | none |
| CloudApi Dockerfile `HEALTHCHECK` | `/health/live` | none |
| WinForms `StoreHubUpdateService` | `/health` → **`/health/ready`** | fixed |
| WinForms `FirstRunWizard` "Test connection" | `/health` → **`/health/ready`** | fixed |
| AppHost (StoreHub, CloudApi) | none → **`/health/ready`** | `WithHttpHealthCheck("/health/ready")` |

- **The two WinForms callers go through one place, so they cannot drift again.**
  - Where DI allows, they reuse `IStoreHubClient.IsHealthyAsync()`. It already calls `/health/ready`
    and already has tests (`StoreHubHttpClientTests.IsHealthyAsync_*`).
  - Where a caller has no DI, it uses a single shared constant whose value a test pins.
  - Which one each caller uses is settled in the plan, after reading their wiring.

## 4. Registration

### 4.1 One shared helper in `ServiceDefaults`

`AddDatabaseReadinessCheck(this IHostApplicationBuilder builder, string connectionName)`:
- registers Xabaril's `AddNpgSql` check, named **`database`** and tagged **`ready`**, with a **3-second**
  timeout;
- reads the connection string from `IConfiguration.GetConnectionString(connectionName)` **when the check
  runs**, through the package's factory overload, not once at startup.

Why "when the check runs":
- In production the StoreHub string is DPAPI-protected. `UnprotectSecrets` decrypts it into
  `IConfiguration` before anything reads it.
- In dev, Aspire injects it.
- In tests, the factory overrides it.

Reading it lazily from `IConfiguration` gives the same answer in all three.

**`ServiceDefaults` is the right home:** only StoreHub and CloudApi reference it (checked 2026-10-06).
The till never loads these packages.

### 4.2 Each service

- **StoreHub:**
  - `builder.AddDatabaseReadinessCheck("storehub-db")` replaces
    `AddHealthChecks().AddDbContextCheck<StoreHubDbContext>(...)`;
  - the `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` reference is removed;
  - the comment beside the route mapping that names the `DbContextCheck` is corrected.
- **CloudApi:**
  - `builder.AddDatabaseReadinessCheck("cloud-db")`;
  - the hand-rolled `MapGet("/health/ready", …)` is **deleted**. That closes I0-C.
- **Exactly one database check per service.** Both `AddNpgsqlDbContext<T>(…)` calls pass
  `settings => settings.DisableHealthChecks = true`. Aspire's EF integration would otherwise register a
  second, untagged database check.

### 4.3 Mapping (`MapDefaultEndpoints`)

- `/health/live` and `/health/ready`:
  - tag-filtered as today;
  - terse;
  - `.WithRequestTimeout(<the named 5-second policy>)`.
- `/health`, still inside `if (app.Environment.IsDevelopment())`, gains
  `ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse`.
- `AddServiceDefaults` registers the request-timeout services (`AddRequestTimeouts` with the named
  policy). `MapDefaultEndpoints` adds `UseRequestTimeouts()` if neither service already does. The plan
  checks the order against each `Program.cs`.

### 4.4 AppHost

- `storehub-api` and `cloud-api` each get `.WithHttpHealthCheck("/health/ready")`.
- Existing `WaitFor(storeHub)` and `WaitFor(cloudApi)` then wait for real readiness.

## 5. Packages

| Package | Version | Project | Note |
|---|---|---|---|
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 | ServiceDefaults | latest stable on 2026-10-06; no 10.x yet. It must build and run on net10.0. If it does not, stop and ask. |
| `AspNetCore.HealthChecks.UI.Client` | 9.0.0 | ServiceDefaults | same |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | — | StoreHub | removed |

## 6. Error handling

- **A failed `ready` says only `Unhealthy`, with 503.** The check catches the Npgsql exception, as
  Xabaril does, and the terse writer prints only the status. No exception text reaches an anonymous
  caller.
- **A hung database** gives a 503 within about 3 seconds, from the check timeout. The 5-second request
  timeout is the backstop.
- **`/health/live` never touches the database.** A database outage leaves it at 200, so a container
  runtime does not restart a process that is only waiting for its database.

## 7. Testing (negative cases first; `Subject_WhenScenario_DirectVerbOutcome`)

New `HealthEndpointTests` in `IndyPOS.StoreHub.IntegrationTests` and in `IndyPOS.CloudApi.IntegrationTests`,
on real Postgres:
- `ReadyProbe_WithAnUnreachableDatabase_ReturnsServiceUnavailable`: a factory whose connection string
  points at a closed port.
- `ReadyProbe_WithAHangingDatabase_AnswersWithinTheTimeout`: a test `TcpListener` accepts and never
  replies. The probe answers 503 in under 5 seconds.
- `ReadyProbe_WithAFailingDatabase_DoesNotLeakTheError`: the body is exactly `Unhealthy`.
- `LiveProbe_WithAnUnreachableDatabase_ReturnsOk`.
- `ReadyProbe_WithTheDatabaseUp_ReturnsOk`.
- `DetailedHealth_OutsideDevelopment_ReturnsNotFound`.
- `DetailedHealth_InDevelopment_ReportsTheDatabaseCheck`: JSON with a `database` entry.
- `RouteTable_WithTheAppBuilt_MapsTheReadyProbeOnce`. **It fails today on CloudApi** (I0-C).

WinForms:
- each health caller is pinned to `/health/ready`, through `IsHealthyAsync` or the shared constant's
  test. One test per caller.

AppHost:
- no automated test; Aspire hosting tests are heavy.
- Verified by hand: run Aspire, see StoreHub *Healthy* only once its database answers, and see the till
  wait for it. Recorded in the PR body.

Counts are measured, never derived. `CLAUDE.md` and `ONBOARDING.md` are updated.

## 8. Docs

- **`docs/architecture/api-conventions.md` gains a "Health checks" section:**
  - the contract table (§3);
  - terse in production;
  - `ready` = one database `SELECT 1`;
  - "callers use `/health/ready`, never `/health`";
  - I0-C noted as resolved.
- **ONBOARDING's `/health` vs `/health/ready` trap** is updated to match.
- The 2026-08-20 containerization spec stays as dated history.

## 9. Success

- No caller anywhere uses a route that production lacks.
- Each service's `ready` runs exactly one real database check.
- No health route leaks error text in production.
- No probe can hang longer than about 5 seconds.
- Aspire reports a service ready only when it is.
- Every health route has a test, and the convention is written down.
