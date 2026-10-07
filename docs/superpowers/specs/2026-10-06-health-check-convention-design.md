# Health-Check Convention — Design

**Date:** 2026-10-06, revised 2026-10-07 after two independent reviews (feasibility, completeness)
**Status:** approved in conversation (Pond), awaiting written-spec review
**Scope:** `IndyPOS.ServiceDefaults`, StoreHub, CloudApi, AppHost, the WinForms till, the installer's
health probe, and the docs that name health routes. One PR.

## 1. Why

Pond asked for IndyPOS health checks to follow two references:
- Milan Jovanović, *Health Checks in ASP.NET Core* (milanjovanovic.tech/blog/health-checks-in-asp-net-core);
- Aspire, *Health checks* (aspire.dev/fundamentals/health-checks).

Checking the code against them found the items below. Each one was **observed on a running system or
proved by a probe**, not reasoned about.

1. **The first-run wizard tests a route that production does not have.**
   - `FirstRunWizard.TestConnectionButton_Click` calls `http://localhost:5000/health`. `/health` is mapped
     only in Development.
   - Probed 2026-10-06 against the installed StoreHub service (v4.0.0, port 5000): `/health` and `/alive`
     → **404**; `/health/live` and `/health/ready` → 200 with the plain-text body `Healthy`.
   - So "Test connection" always reports `NotFound` on a real till.
   - It also hard-codes `localhost:5000` instead of using the configured `StoreHub:BaseUrl`.
2. **`StoreHubUpdateService` is dead and dangerous.**
   - It also calls `/health`, but nothing ever resolves `IStoreHubUpdateService`. It is registered in
     `Windows.Forms/ConfigureServices.cs:56` and unused; the till updates through Velopack (`IUpdateService`).
   - If anyone ever wired it up, it would:
     - stop StoreHub;
     - download a zip from an old GitHub repository (`ponggun/IndyPOS`);
     - overwrite `C:\Program Files\IndyPOS\StoreHub`.

     That goes around the installer's rule that StoreHub is upgraded first, with backup and rollback.
   - **It is deleted, not fixed** (§4.6).
3. **CloudApi maps `/health/ready` twice** (defect I0-C, recorded 2026-08-20, never resolved).
   - `MapDefaultEndpoints()` maps it, and so does a hand-rolled `MapGet("/health/ready")`.
   - Two endpoints on one pattern should give an ambiguous-match 500. The new route-table test settles
     this (§7).
   - The hand-rolled copy also returns `ex.Message` on failure, which leaks database error text from an
     anonymous route.
4. **No integration test touches any health route** in StoreHub or CloudApi.
5. **Aspire's AppHost does not wait for real readiness.** On 2026-10-06 the dashboard reported StoreHub's
   endpoints "Ready" for a process that had already exited.
6. **About ten docs and one script tell people to probe `/health`.** On a real install each of them
   reports a failure or a 404 (§8).

Already right, and kept:
- tag-filtered `/health/live` and `/health/ready`;
- the installer's `HealthProbe` and `verify-install.ps1` use `/health/ready`;
- the CloudApi container `HEALTHCHECK` uses `/health/live` on purpose.

## 2. Decisions (Pond, 2026-10-06)

- **Production probes are terse.** Health *details* (per-check status, timings, errors) are for
  Development only. Production `/health/live` and `/health/ready` stay anonymous and say only
  `Healthy`/`Unhealthy`.
  - The health routes leak nothing.
  - `/version` is a separate anonymous route that shows the environment and commit. This spec leaves it
    alone.
- **`/health` really is Development-only on installed tills**, observed 2026-10-06:
  - `sc.exe create` sets no environment (`StoreHubInstaller.cs:173-177`);
  - `MigrationRunner.cs:45` forces `Production`;
  - the live `/version` reports `"environment":"Production"`;
  - OpenAPI and Scalar answer 404.
- **Approach C:** follow the article's packages.
  - The database readiness check is Xabaril's `AspNetCore.HealthChecks.NpgSql` (`SELECT 1`).
  - The detailed JSON writer is Xabaril's `AspNetCore.HealthChecks.UI.Client`.
- **Offline-first invariant.** StoreHub's `ready` checks **only its local database**. It never includes a
  cloud check, and StoreHub never waits on CloudApi, in production or in Aspire.
- **Left out (YAGNI):**
  - output caching on probes (StoreHub listens on localhost only, per `DatabaseSetup.cs:448`, and Cloud
    Run probes rarely);
  - a health UI dashboard;
  - authenticated health detail in production.

## 3. The contract

| Route | Environments | Checks run | Response |
|---|---|---|---|
| `/health/live` | all | `self` (tag `live`) | text `Healthy`, 200 |
| `/health/ready` | all | `database` (tag `ready`) | text `Healthy` 200, or `Unhealthy` 503 |
| `/health` | **Development only** | every registered check | detailed JSON (`UIResponseWriter`) |

- **Route shapes do not change.**
- **One observable change, said out loud: CloudApi's `/health/ready` body and failure code change.**
  - Today it answers JSON `{status, database}`, and fails with **500** (`Results.Problem`).
  - After this change it answers the text `Healthy`, and fails with **503**.
  - No automated consumer reads that body: `IsHealthyAsync` and every probe look only at the status
    code.
  - `docs/operations/cloud-deployment.md` documents the old body, and is rewritten (§8).
- **How long a probe can take.** A hanging database must not hold a caller. The layers are:
  1. **Npgsql's own connect timeout is the real bound.** Npgsql's connection phase ignores cancellation,
     so neither a check timeout nor a request timeout can cut it short. This was measured by the
     feasibility review: a check against a server that accepts and never replies answered 503 only after
     **15 s**, Npgsql's default `Timeout`. So the check's connection string sets **`Timeout=3`** and
     **`CommandTimeout=3`**. Measured: 503 after **3.2 s**.
  2. **The registration timeout (3 s)** is a backstop.
  3. **A 5-second request timeout** on `live` and `ready` is a backstop.
     - If it ever fires, the answer is **504 with an empty body**, not 503.
     - It does nothing while a debugger is attached.
- **Callers:**

| Caller | Route | Change |
|---|---|---|
| Installer `HealthProbe` (fresh install + upgrade) | `/health/ready` | route unchanged; it now polls until a **60-second deadline** instead of 5 fixed attempts (§4.5) |
| `scripts/verify-install.ps1` | `/health/live`, `/health/ready` | none |
| CloudApi Dockerfile `HEALTHCHECK` | `/health/live` | none |
| WinForms `FirstRunWizard` "Test connection" | `/health` → **`/health/ready`** | fixed (§4.4) |
| WinForms `IStoreHubClient.IsHealthyAsync` | `/health/ready` | now reads the shared route constant |
| WinForms `StoreHubUpdateService` | `/health` | **deleted** (§4.6) |
| AppHost (StoreHub, CloudApi) | none → **`/health/ready`** | `WithHttpHealthCheck` (§4.3) |
| Docs, runbooks, the second smoke script | `/health` → `/health/ready` | §8 |

- **The installer cannot share a constant with the till.** It is not in `IndyPOS.sln`. It keeps its own
  literal, which is already correct.

## 4. Changes

### 4.1 One shared helper in `ServiceDefaults`

`AddDatabaseReadinessCheck(this IHostApplicationBuilder builder, string connectionName)`:
- Registers Xabaril's `AddNpgSql` check, named **`database`**, tagged **`ready`**, with a **3-second**
  registration timeout. It uses the factory overload `AddNpgSql(Func<IServiceProvider,string> …)`.
- **The factory resolves the connection string lazily, on the first check, and the package then caches
  it for the life of the process.** It reads `IConfiguration.GetConnectionString(connectionName)` and
  rebuilds it with `NpgsqlConnectionStringBuilder`, setting `Timeout = 3` and `CommandTimeout = 3`.
- Reading `IConfiguration` lazily gives the right value everywhere:
  - in production, `UnprotectSecrets` decrypts the DPAPI value into `IConfiguration` before anything
    reads it;
  - in dev, Aspire injects it;
  - in tests, the factory overrides it.
- **A missing connection string is a deployment error, not a health state.** The factory throws outside
  the check's try/catch, so `ready` answers **500**. Every caller treats any non-2xx as not ready, so this
  is accepted and documented, not guarded.

Only StoreHub and CloudApi reference `ServiceDefaults` (checked 2026-10-06), so the till never loads these
packages.

**Packages** (all in `ServiceDefaults`):

| Package | Version | Note |
|---|---|---|
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 | Latest stable on 2026-10-06. It targets net8.0 and depends on Npgsql 8.0.3, which unifies up to the repo's Npgsql 10.0.0. Built and run on net10.0 by the feasibility probe. |
| `AspNetCore.HealthChecks.UI.Client` | 9.0.0 | Same. |

### 4.2 Each service

- **StoreHub:**
  - `builder.AddDatabaseReadinessCheck("storehub-db")` replaces `AddDbContextCheck<StoreHubDbContext>`;
  - the direct `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` reference is removed.
    Aspire still brings that package in on its own; the reference was only explicit;
  - the comment beside the route mapping that names `DbContextCheck` is corrected.
- **CloudApi:**
  - `builder.AddDatabaseReadinessCheck("cloud-db")`;
  - the hand-rolled `MapGet("/health/ready", …)` is **deleted**. That closes I0-C.
- **Exactly one database check per service.**
  - Both `AddNpgsqlDbContext<T>(…)` calls pass `settings => settings.DisableHealthChecks = true`.
  - Otherwise Aspire 9.2.0 registers a second check, `AddDbContextCheck<T>` named `typeof(T).Name`, with
    no tags.
- **Mapping in `MapDefaultEndpoints`:**
  - `/health/live` and `/health/ready` keep their tag filters and terse writer, and gain
    `.WithRequestTimeout("health-probe")`;
  - the Development-only `/health` gains `ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse`;
  - `AddServiceDefaults` registers the named 5-second policy with `AddRequestTimeouts`.
- **`app.UseRequestTimeouts()` is called explicitly in each `Program.cs`**, beside the other middleware,
  not hidden inside a `Map*` method.
  - Neither service uses it today.
  - Neither calls `UseRouting` explicitly, so implicit routing runs first and the order is correct.

### 4.3 AppHost

- `storehub-api` and `cloud-api` each get `.WithHttpHealthCheck("/health/ready")`. The Aspire.Hosting
  signature is `WithHttpHealthCheck(path, int? statusCode, string? endpointName)`; the plan confirms each
  resource's endpoint name (`http` or `https`) for each launch profile.
- **`storehub-api` drops `.WaitFor(cloudApi)`** and keeps `.WithReference(cloudApi)` for service
  discovery. Otherwise, once `cloud-api` has a real readiness check, a dev StoreHub would not start until
  the cloud database answered, which breaks the offline-first invariant (§2).
- The till's existing `WaitFor(storeHub)` then waits for StoreHub's real readiness.

### 4.4 The first-run wizard

- **The wizard's own logic moves out of the click handler into a small, testable class**, so the form only
  wires it up. This follows "don't over-invest in WinForms; logic out of the form".
- **`StoreHubConnectionCheck.CheckAsync(CancellationToken)` returns `StoreHubConnectionStatus`:**
  - `Healthy` (200);
  - `NotReady` (503, StoreHub is up but its database is not);
  - `Unexpected` (any other status, carried with the code);
  - `Unreachable` (connection refused);
  - `TimedOut` (the 5-second client timeout).

  It **rethrows `OperationCanceledException` when its token is cancelled**, before any broad catch.
- **The wizard gets the class from DI.** It is already resolved from DI (`AddTransient<FirstRunWizard>`,
  constructor `IStoreConfigurationService`). The class:
  - uses the configured `StoreHub:BaseUrl`;
  - uses the shared route constant;
  - uses its own 5-second `HttpClient` timeout, kept from today, not the client's 30 seconds.
- **The wizard keeps its distinct messages**, and adds one for `NotReady`: "⚠ StoreHub is running but its
  database is not ready". The existing messages stay in English, like today.
- **The shared constant lives in Application:** `StoreHubRoutes.HealthReady = "/health/ready"`.
  `StoreHubHttpClient.IsHealthyAsync` uses it too.

### 4.5 The installer's health probe

- **`HealthProbe.IsReadyAsync` polls until a 60-second deadline**, with a 2-second gap between tries,
  instead of 5 fixed attempts.
- **Why.** Today a failing attempt can take up to 10 seconds, the client timeout, so 5 attempts gave
  PostgreSQL about 60 seconds to come up after a reboot. With the 3-second database timeout each failing
  attempt ends sooner. Five attempts would then be only about 25 seconds, and an upgrade could roll back
  while PostgreSQL is still starting.
- **It rethrows `OperationCanceledException` when the caller's token is cancelled.** Today the bare
  `catch` swallows it.
- **The stale comment is corrected:** `HealthProbe.cs:17-18` says "/health and /alive are dev-only", but
  `/alive` is not mapped at all.
- **A VM upgrade smoke** (4.x → this build) is run before release and recorded in the PR.

### 4.6 Delete `StoreHubUpdateService`

`StoreHubUpdateService`, `IStoreHubUpdateService`, `StoreHubUpdateProgress` and the DI registration are
removed **in their own commit**. Check the build for any other user first. Nothing resolves them today.

## 5. Error handling, in one place

- **A failed `ready` says only `Unhealthy`, with 503.** Xabaril catches the Npgsql exception, and the
  terse writer prints only the status. The no-leak test asserts the body is exactly `Unhealthy`.
- **A hanging database** answers 503 after about 3 seconds, from Npgsql's `Timeout=3`. The registration
  and request timeouts are backstops.
- **A missing connection string** answers 500 (§4.1).
- **`/health/live` never touches the database.** A database outage leaves it at 200, so nothing restarts
  a process that is only waiting for its database.

## 6. Rollout and version skew

- **Old till, new StoreHub:** the only live caller of `/health` in an old till is the wizard. It already
  gets a 404 today, and it only sets a label. No regression.
- **New till, StoreHub rolled back to 4.0.0:** v4.0.0 already serves `/health/ready`. Fine.
- **StoreHub does not need to keep answering `/health` in production.**

## 7. Testing (negative cases first; `Subject_WhenScenario_DirectVerbOutcome`)

**The shared behaviour is tested once, on a slim host.** A minimal `WebApplication` calls
`AddServiceDefaults()`, `AddDatabaseReadinessCheck(…)`, `UseRequestTimeouts()` and `MapDefaultEndpoints()`.
It lives in a new `tests/IndyPOS.ServiceDefaults.Tests` (on the `TestPostgres` seam, so it runs in CI):
- `ReadyProbe_WithAnUnreachableDatabase_ReturnsServiceUnavailable` (a closed port; about 2 s on Windows);
- `ReadyProbe_WithAHangingDatabase_AnswersWithinFiveSeconds` (a test `TcpListener` that accepts and never
  replies);
- `ReadyProbe_WithAFailingDatabase_DoesNotLeakTheError` (the body is exactly `Unhealthy`);
- `LiveProbe_WithAnUnreachableDatabase_ReturnsOk`;
- `ReadyProbe_WithTheDatabaseUp_ReturnsOk`;
- `DetailedHealth_OutsideDevelopment_ReturnsNotFound`;
- `DetailedHealth_InDevelopment_ReportsTheDatabaseCheck` (JSON with a `database` entry).

These factories set the connection string themselves, outside the shared `INDYPOS_TEST_POSTGRES` fixture,
where the test needs a closed port or a hanging listener.

**Each service's wiring:**
- **StoreHub** (existing factory):
  - `RouteTable_WithTheAppBuilt_MapsTheReadyProbeOnce`;
  - `ReadyProbe_WithTheDatabaseUp_ReturnsOk`;
  - `HealthChecks_WithTheAppBuilt_RegisterOneDatabaseCheck`, so that turning off Aspire's check is pinned.
- **CloudApi:** the integration tests have **no host for the real `Program`**. OpenIddict needs
  certificates and a registered client, `EnsureProductionSafe` runs outside Development, and an
  `EventProcessor` hosted service starts. So:
  - the plan first tries a `WebApplicationFactory<Program>` in Development, on a `TestPostgres` database,
    for `RouteTable_WithTheAppBuilt_MapsTheReadyProbeOnce` (it fails today: I0-C) and
    `HealthChecks_WithTheAppBuilt_RegisterOneDatabaseCheck`;
  - **if that cannot be made to work in about half a day, fall back** to slim-host coverage plus a manual
    probe of the running container, recorded in the PR.

**WinForms:**
- `StoreHubConnectionCheck` tests, through a fake `HttpMessageHandler`: one per status, plus "a cancelled
  token throws `OperationCanceledException`" and "the request goes to `{BaseUrl}/health/ready`".
- `IsHealthyAsync` keeps its tests.

**Installer:**
- `HealthProbe` polls until the deadline;
- it returns as soon as `ready` answers 200;
- a cancelled token throws.

The time-dependent tests take an injectable delay or clock, so they do not sleep for real.

**AppHost:**
- no automated test; Aspire hosting tests are heavy.
- Verified by hand: StoreHub shows *Healthy* only once its database answers, StoreHub starts with
  CloudApi stopped, and the till waits for StoreHub. Recorded in the PR.

**Counts are measured, never derived.** `CLAUDE.md` and `ONBOARDING.md` are updated:
- per-suite totals;
- the new suite;
- the Docker-down failure count;
- the installer's count.

## 8. Docs

- **`docs/architecture/api-conventions.md` gains a "Health checks" section:**
  - the contract table (§3);
  - terse in production;
  - `ready` = one local database `SELECT 1`, never the cloud;
  - "callers use `/health/ready`, never `/health`";
  - the timeout layers;
  - I0-C noted as resolved.
- **Every `/health` reference becomes `/health/ready`, or `/health/live` where liveness is meant.**
  Expected bodies become the plain text `Healthy`. Found by the completeness review:
  - `docs/operations/smoke-test.ps1:152` (a script);
  - `docs/operations/update-procedure.md:144` and its checkpoint at `:151`, which claims a
    `{"status":"Healthy"}` body;
  - `docs/operations/troubleshooting-guide.md:18`;
  - `docs/operations/RUNBOOK.md:47`;
  - `docs/operations/pilot-checklist.md:84,96`;
  - `docs/operations/post-deployment-monitoring.md:185`;
  - `src/IndyPOS.Windows.Forms/appsettings.README.md:166`;
  - `docs/diagrams/architecture-overview.md:254,315`, pad-only (Pond, 2026-10-06);
  - `docs/operations/cloud-deployment.md:143-170`: rewritten, since it describes I0-C and the JSON body
    as live behaviour;
  - `docs/operations/store-installation-guide.md:598` probes `192.168.1.100:5000`, but StoreHub listens on
    localhost only. Corrected to `localhost`.
- **ONBOARDING's `/health` vs `/health/ready` trap** is updated to match.
- The 2026-08-20 containerization spec stays as dated history.

## 9. Success

- No caller, script or runbook uses a route that production lacks.
- Each service's `ready` runs exactly one real database check, and StoreHub's never involves the cloud.
- No health route leaks error text in production.
- A hanging database answers in about 3 seconds, not 15, and an upgrade still allows about 60 seconds
  for PostgreSQL to come up.
- Aspire reports a service ready only when it is, and StoreHub starts without CloudApi.
- The dead `StoreHubUpdateService` is gone.
- Every health route has a test, and the convention is written down.
