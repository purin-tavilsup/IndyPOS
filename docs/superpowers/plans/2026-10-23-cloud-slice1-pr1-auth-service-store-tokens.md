# Cloud slice 1, PR S1-1: IndyPOS.Cloud.Auth and store tokens — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A new service, IndyPOS.Cloud.Auth, issues the stores' access tokens; CloudApi stops issuing tokens and only validates Auth's; register-store creates the store's client in Auth.

**Architecture:** Machines first. Auth is an ASP.NET Core app with its own database (`auth-db`, same PostgreSQL), OpenIddict server with the client-credentials grant, a `StoreClients` table (enabled flag, last authenticated), and a small store-client API that only CloudApi's own service client may call. CloudApi swaps its OpenIddict *server* for OpenIddict *validation* against Auth's issuer and audience `indypos-api`, and calls Auth during register-store. StoreHub asks Auth, not CloudApi, for its token. No UI and no people yet (S1-2, S1-3).

**Tech Stack:** .NET 10, ASP.NET Core, OpenIddict 7 (server + validation + EF Core stores), EF Core + Npgsql, .NET Aspire, xUnit, FluentAssertions, Testcontainers (`tests/IndyPOS.Testing.Postgres`).

**Spec:** `docs/superpowers/specs/2026-10-21-cloud-platform-and-identity-design.md` — "Build slice 1", "The rules", "Hosts and names", "Left to the builder"; runbook must-haves in `docs/operations/cloud-deployment.md` (persistent signing keys, forwarded HTTPS issuer, service-to-service auth). Slice split agreed with Pond 2026-10-23: S1-1 machines → S1-2 people → S1-3 Cloud.Web shell + person tokens → S1-4 screens.

**Branch:** `feat/cloud-auth-store-tokens` from `development`.

## Global Constraints

- **Auth issues every token; CloudApi issues none** (spec rule 3). After this PR CloudApi has no `/oauth/token`, no OpenIddict server and no OpenIddict tables.
- **Auth owns whether a store may get a token** (its own enabled flag) and `LastAuthenticatedAtUtc`; CloudApi never reads Auth's database and Auth never reads CloudApi's (rule 2). Each has its own login role in production; in dev both use the Aspire Postgres.
- Store tokens: client credentials, scopes `sync.write` and `master.read`, claim `store_id`, **audience (resource) `indypos-api`**, access-token lifetime 15 minutes, JWT not encrypted (as today).
- CloudApi validates **issuer** (Auth's, exact, ending in `/`) and **audience `indypos-api`**. A token from any other issuer or for another audience is refused.
- **Signing keys persist across redeploys**: production loads the RSA key from configuration (`INDYPOS_RSA_SIGNING_KEY` / `OpenIddict:RsaSigningKey`, moved from CloudApi as is); Development may use OpenIddict's development certificate.
- **Behind a TLS terminator**: `OpenIddict:TlsTerminatedUpstream` (moved as is) plus forwarded headers, so the issuer and every advertised URL are `https`.
- **Deactivation is final** (spec): a disabled client never gets a token again; re-registering a store makes a new client and secret.
- The cloud has never been deployed: CloudApi's OpenIddict tables are **dropped**, not left dead (spec, Pond 2026-10-21). Auth's database is new. Run the cloud gate recipe only to confirm no *business* table changed.
- Dev and tests never call real Google or any external issuer. Nothing in this PR involves Google.
- StoreHub keeps working offline; a missing Auth only means no token, which `HttpCloudSyncClient` / the heartbeat already treat as "retry later".
- Never run two `dotnet test` processes at once.

## Review Focus

1. **A disabled store asking for a token** — `invalid_client`, no token, `LastAuthenticatedAtUtc` unchanged. (Task 2.)
2. **A token signed by Auth but for another audience, or from another issuer** — CloudApi answers 401 on `/sync/events`. (Task 5.)
3. **Register-store when Auth is down, or when CloudApi's commit fails after Auth created the client** — no store row without an enabled client, and no enabled client left behind for a store CloudApi does not have (compensating delete). (Task 6.)
4. **Auth restarted** — tokens issued before the restart still validate in CloudApi (same signing key; dev certificate in Development). (Tasks 2, 7.)
5. **A store token used on Auth's store-client API** — 403; only CloudApi's service client may create or disable store clients. (Task 3.)

---

### Task 1: The IndyPOS.Cloud.Auth project, its database and health

**Files:**
- Create: `src/IndyPOS.Cloud.Auth/` (`IndyPOS.Cloud.Auth.csproj` web SDK, net10.0; `Program.cs`; `Data/AuthDbContext.cs`; `Domain/StoreClient.cs`; `Data/Migrations/*` initial migration `InitialAuth`)
- Create: `tests/IndyPOS.Cloud.Auth.IntegrationTests/` (csproj referencing `IndyPOS.Testing.Postgres` and the Auth project; `AuthPostgresFixture.cs`; `AuthTestHost.cs`)
- Modify: `IndyPOS.sln` (both projects), `src/IndyPOS.AppHost/Program.cs` (`auth-db` database, `cloud-auth` project resource with `/health/ready` check, `WaitFor(postgres)`), `src/IndyPOS.AppHost/IndyPOS.AppHost.csproj` (project reference)

**Interfaces:**
- Produces: `StoreClient { string StoreId (PK, 50); string ClientId (unique, 100); bool Enabled; DateTime CreatedAtUtc; DateTime? DisabledAtUtc; DateTime? LastAuthenticatedAtUtc }`; `AuthDbContext : DbContext` with `DbSet<StoreClient> StoreClients` and `options.UseOpenIddict()`; connection name `auth-db`; `AddDatabaseReadinessCheck("auth-db")`, `MapDefaultEndpoints()` (API conventions: `/health/live`, `/health/ready`).
- Auth applies its migrations on start in Development (as CloudApi does); production runs them as a one-shot step (runbook).
- [ ] **Step 1: Write the failing tests:** `HealthReady_WithItsDatabase_ReturnsHealthy`, `HealthLive_ReturnsHealthy`, `AuthDatabase_AfterMigrating_HasStoreClientsAndOpenIddictTables`.
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.Cloud.Auth.IntegrationTests` — Expected: FAIL (project/route missing).
- [ ] **Step 3: Implement** the project skeleton, DbContext, entity, migration, Aspire resource.
- [ ] **Step 4: Run** the suite and `dotnet build` — Expected: PASS, 0 errors.
- [ ] **Step 5: Commit** `feat(cloud-auth): new IndyPOS.Cloud.Auth service with its own database`

---

### Task 2: Auth issues store tokens

**Files:**
- Create: `src/IndyPOS.Cloud.Auth/OpenIddict/AuthServerSetup.cs` (moved and adapted from `CloudApi/Infrastructure/Auth/OpenIddictExtensions.cs`: RSA key loading, dev certificate, `TlsTerminatedUpstream`), `src/IndyPOS.Cloud.Auth/Tokens/StoreTokenEndpoint.cs` (the `/oauth/token` passthrough, adapted from `TokenController`)
- Modify: `src/IndyPOS.Cloud.Auth/Program.cs` (forwarded headers: `X-Forwarded-For`, `X-Forwarded-Proto`, all proxies trusted only when `TlsTerminatedUpstream` is on)
- Test: `tests/IndyPOS.Cloud.Auth.IntegrationTests/StoreTokenTests.cs`

**Interfaces:**
- Consumes: Task 1.
- Produces: `POST /oauth/token` (client credentials only). Claims `sub` = `store_id` = StoreId, `name`; scopes as requested within the client's permissions; resource `indypos-api` set on every store token. Constant `AuthResources.CloudApi = "indypos-api"` in a small shared spot both projects can reference (builder's choice: `IndyPOS.Application` or a constants file duplicated with a comment — prefer `IndyPOS.Application/Common/Constants`).
- Issuer: configured `OpenIddict:Issuer` (absolute, ending in `/`); default from the request in Development.
- [ ] **Step 1: Write the failing tests** (seed a client directly through `IOpenIddictApplicationManager` + a `StoreClients` row in the test):
  - `Token_ForAnEnabledStore_HasItsStoreIdAndTheApiAudience` (decode the JWT: `store_id`, `aud` = `indypos-api`, `iss` = configured issuer).
  - `Token_ForAnEnabledStore_RecordsWhenItAuthenticated`.
  - `Token_ForADisabledStore_IsRefusedAsInvalidClient` (and `LastAuthenticatedAtUtc` unchanged).
  - `Token_ForAnUnknownClient_IsRefusedAsInvalidClient`.
  - `Token_WithAWrongSecret_IsRefused`.
  - `Token_WithAnotherGrantType_IsRefused`.
  - `Token_BehindATlsTerminator_AdvertisesAnHttpsIssuer` (`TlsTerminatedUpstream=true`, request over http with `X-Forwarded-Proto: https`; discovery document's `issuer` and `token_endpoint` are https).
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.Cloud.Auth.IntegrationTests --filter "FullyQualifiedName~StoreTokenTests"` — Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** the filter — Expected: PASS.
- [ ] **Step 5: Commit** `feat(cloud-auth): issue store access tokens for the cloud API audience`

---

### Task 3: Auth's store-client API, callable only by CloudApi

**Files:**
- Create: `src/IndyPOS.Cloud.Auth/StoreClients/StoreClientEndpoints.cs`, `StoreClientService.cs`, `ServiceClientSeeder.cs`
- Test: `tests/IndyPOS.Cloud.Auth.IntegrationTests/StoreClientApiTests.cs`

**Interfaces:**
- Produces (all require scope `auth.store-clients`, which only the service client `cloud-api` may request):
  - `PUT /store-clients/{storeId}` body `{ "displayName": "..." }` → 200 `{ "clientId": "store_<storeId>", "clientSecret": "<random 32 bytes, base64>" }`. Creates the OpenIddict application **and** the `StoreClients` row, enabled. If one exists and is **disabled**, answers 409 (deactivation is final; a re-registered store gets a new store id — spec). If one exists and is enabled, replaces its secret and returns the new one, so a register-store retried after a failed compensation (Task 6) recovers instead of being stuck on a leftover client.
  - `DELETE /store-clients/{storeId}` → 204 (removes application and row; for Task 6's compensation only).
  - `POST /store-clients/{storeId}/disable` → 204; sets `Enabled = false`, `DisabledAtUtc`. Idempotent.
- `ServiceClientSeeder` (runs at start, every environment): ensures the confidential client `cloud-api` exists with the secret from configuration `ServiceClients:CloudApi:ClientSecret`, permission client-credentials + scope `auth.store-clients` only; updates the secret when configuration changes. Missing setting → Auth refuses to start (fail fast).
- [ ] **Step 1: Write the failing tests:**
  - `PutStoreClient_WithTheServiceToken_ReturnsCredentialsThatWork` (then get a store token with them).
  - `PutStoreClient_ForAnEnabledStore_ReplacesTheSecret` (old secret refused afterwards).
  - `PutStoreClient_ForADisabledStore_ReturnsConflict`.
  - `DisableStoreClient_ThenToken_IsRefused`.
  - `StoreClientApi_WithoutAToken_ReturnsUnauthorized`.
  - `StoreClientApi_WithAStoreToken_ReturnsForbidden`.
  - `DeleteStoreClient_RemovesItsCredentials`.
  - `Startup_WithoutTheServiceClientSecret_Fails`.
- [ ] **Step 2: Run** the filter `StoreClientApiTests` — Expected: FAIL.
- [ ] **Step 3: Implement** (validation of the service token: OpenIddict validation with `UseLocalServer()` inside Auth).
- [ ] **Step 4: Run** — Expected: PASS.
- [ ] **Step 5: Commit** `feat(cloud-auth): store-client API for CloudApi, guarded by its own service client`

---

### Task 4: Dev stores get their clients from Auth

**Files:**
- Create: `src/IndyPOS.Cloud.Auth/StoreClients/DevStoreClients.cs` (Development only: each `IndyPOS.StoreProfiles` profile's `CloudClientId` with `StoreProfiles.DevCloudClientSecret`, enabled; idempotent; never in other environments)
- Modify: `src/IndyPOS.Cloud.Auth/IndyPOS.Cloud.Auth.csproj` (reference `IndyPOS.StoreProfiles`); AppHost: `cloud-auth` gets `ServiceClients__CloudApi__ClientSecret` (a fixed dev value in `IndyPOS.StoreProfiles` next to `DevCloudClientSecret`), `cloud-api` gets the same secret and `Auth__Issuer` / a reference to `cloud-auth`
- Test: `tests/IndyPOS.Cloud.Auth.IntegrationTests/DevStoreClientsTests.cs`

- [ ] **Step 1: Write the failing tests:** `DevStoreClients_InDevelopment_RegistersEveryProfile`; `DevStoreClients_RunTwice_RegistersNothingNew`; `DevStoreClients_OutsideDevelopment_RegistersNothing`; `DevStoreClients_TokenWithTheDevSecret_Works`.
- [ ] **Step 2: Run** — Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** — Expected: PASS.
- [ ] **Step 5: Commit** `feat(cloud-auth): dev store clients for the Aspire stack`

---

### Task 5: CloudApi becomes a resource server

**Files:**
- Delete: `src/IndyPOS.CloudApi/Infrastructure/Auth/TokenController.cs`, `OpenIddictStoreClientCredentialStore.cs`, `OpenIddictExtensions.cs` (its server half moved in Task 2); `tests/IndyPOS.CloudApi.Tests/Infrastructure/Auth/TokenControllerTests.cs`, `OpenIddictStoreClientCredentialStoreTests.cs` (behaviour now covered in Auth's tests)
- Create: `src/IndyPOS.CloudApi/Infrastructure/Auth/AuthTokenValidation.cs` (`AddAuthTokenValidation(IConfiguration)`: OpenIddict validation with `SetIssuer(Auth:Issuer)`, `AddAudiences("indypos-api")`, `UseSystemNetHttp()` discovery, `UseAspNetCore()`)
- Modify: `src/IndyPOS.CloudApi/Program.cs`, `Infrastructure/CloudDbContext.cs` (remove `UseOpenIddict()`), `Domain/CloudStoreConfig.cs` (remove `LastAuthenticatedAtUtc`; `ClientId` and `IsActive` stay as CloudApi's record of the store)
- Create: cloud migration `MoveTokensToAuth` (drops the four OpenIddict tables and `StoreConfigs.LastAuthenticatedAtUtc`)
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/AuthTokenValidationTests.cs`

**Interfaces:**
- Produces: `/sync/*`, `/master/*` and `/sync/bulk-migration` accept only Auth-issued store tokens (default scheme = OpenIddict validation). `StoreHubJwt` and `SystemAdminOnly` stay untouched until S1-3.
- [ ] **Step 1: Write the failing tests** (an in-process Auth test host issues real tokens; the test wires CloudApi's validation to it — the builder chooses how discovery reaches the in-process issuer, e.g. OpenIddict's `ConfigureHttpClientHandler` with the TestServer handler):
  - `SyncStatus_WithAStoreTokenFromAuth_ReturnsOk`.
  - `SyncStatus_WithATokenForAnotherAudience_ReturnsUnauthorized`.
  - `SyncStatus_WithATokenFromAnotherIssuer_ReturnsUnauthorized` (a second Auth host with another key/issuer).
  - `SyncStatus_WithoutAToken_ReturnsUnauthorized`.
  - `OAuthToken_OnCloudApi_IsGone` (404).
- [ ] **Step 2: Run** — Expected: FAIL.
- [ ] **Step 3: Implement**, including the migration; fix every CloudApi test that relied on the removed server (`HealthEndpointTests`, `DevStoreRegistrationTests` → adjusted in Task 6, `EnsureCreatedDatabaseTests`).
- [ ] **Step 4: Run** `dotnet test tests/IndyPOS.CloudApi.IntegrationTests` and `tests/IndyPOS.CloudApi.Tests` — Expected: PASS. **Gate:** cloud recipe — previous last migration → latest; the column diff touches only the OpenIddict tables and `StoreConfigs.LastAuthenticatedAtUtc`; record the result (a deliberate drop, allowed only because the cloud was never deployed) in `upgrade-procedure.md`.
- [ ] **Step 5: Commit** `feat(cloud): CloudApi validates Auth's tokens and issues none`

---

### Task 6: Register-store creates the client in Auth

**Files:**
- Create: `src/IndyPOS.Application/Abstractions/Cloud/Auth/IAuthStoreClients.cs` (replaces `IStoreClientCredentialStore`, which is deleted)
- Create: `src/IndyPOS.CloudApi/Infrastructure/Auth/HttpAuthStoreClients.cs` (typed HttpClient to Auth; gets and caches its own `auth.store-clients` token with client id `cloud-api` and `ServiceClients:CloudApi:ClientSecret`)
- Modify: `RegisterStoreHandler.cs`, `DevStoreRegistration.cs` (now writes only `StoreConfigs`; the clients are Auth's, Task 4), `Program.cs`
- Test: `tests/IndyPOS.CloudApi.Tests/Infrastructure/Auth/RegisterStoreHandlerTests.cs` (rewritten), `tests/IndyPOS.CloudApi.IntegrationTests/RegisterStoreTransactionTests.cs`, `DevStoreRegistrationTests.cs` (adjusted)

**Interfaces:**
- Produces: `Task<StoreClientCredentials> IAuthStoreClients.CreateAsync(string storeId, string displayName, CancellationToken ct)`, `Task DeleteAsync(string storeId, CancellationToken ct)`, `Task DisableAsync(string storeId, CancellationToken ct)`; `record StoreClientCredentials(string ClientId, string ClientSecret)`. Auth unreachable or non-2xx → `AuthUnavailableException`.
- Register-store order *(Claude's call, spec leaves it to the builder)*: begin CloudApi transaction → insert `StoreConfigs` → `CreateAsync` in Auth → commit. If Auth fails → roll back, answer **503** with `{ "error": "..." }` (nothing anywhere). If the commit fails after Auth created the client → `DeleteAsync` (compensation), log, rethrow. A compensation that also fails is logged as an error with the store id (the only orphan path; the client belongs to a store CloudApi does not know, so ingest refuses nothing it would otherwise accept — documented as a known gap).
- [ ] **Step 1: Write the failing tests** (fake `IAuthStoreClients` in unit tests; real Postgres for the transaction tests):
  - `Register_WhenAuthCreatesTheClient_SavesTheStoreAndReturnsTheSecret`.
  - `Register_WhenAuthIsDown_SavesNothingAndThrows`.
  - `Register_WhenTheCommitFailsAfterAuth_DeletesTheClientInAuth` (interceptor fails the commit).
  - `Register_ForAnExistingStore_ReturnsConflictWithoutCallingAuth`.
  - `RegisterEndpoint_WhenAuthIsDown_ReturnsServiceUnavailable`.
  - Dev registration: `DevStoreRegistration_InDevelopment_WritesEveryProfilesStoreConfig` (no Auth call).
- [ ] **Step 2: Run** — Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** CloudApi suites — Expected: PASS.
- [ ] **Step 5: Commit** `feat(cloud): register-store creates the store's client in Auth`

---

### Task 7: StoreHub and the migrator ask Auth for tokens

**Files:**
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs` (the `ITokenService` client's base address = Auth: `services:cloud-auth:https:0` → `services:cloud-auth:http:0` → `https+http://cloud-auth`; `ICloudSyncClient` and `ICloudHeartbeatClient` stay on CloudApi), `src/IndyPOS.AppHost/Program.cs` (StoreHubs `WithReference(cloudAuth)`), `src/IndyPOS.MigrationTool` (`--auth-url`; `SyncToCloudAsync` gets its token from `{auth-url}/oauth/token` as a form post, and the check deferred from sync hardening: when Auth is reachable, the token's `store_id` must equal `--store-id`, else refuse)
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Services/CloudTokenServiceTests.cs` (base address), `tests/IndyPOS.MigrationTool.Tests/StoreTokenCheckTests.cs`

- [ ] **Step 1: Write the failing tests:**
  - `AddStoreHubServices_WithACloudClient_PointsTheTokenServiceAtAuth` (resolve `ITokenService`'s HttpClient base address from a configuration with both service URLs).
  - Migrator: `Check_WhenTheTokensStoreIdMatches_ReturnsAMatch`; `Check_WhenItDiffers_ReturnsAMismatchNamingBoth`; `Check_WhenAuthIsUnreachable_SaysSoAndContinues`; `RequestToken_PostsAFormToTheOauthTokenEndpoint` (fake handler asserts path `/oauth/token` and `application/x-www-form-urlencoded`).
- [ ] **Step 2: Run** — Expected: FAIL.
- [ ] **Step 3: Implement.** Update the two migrator runbooks (`--auth-url`), and `docs/superpowers/specs/2026-10-23-sync-hardening-design.md`'s amendment note ("check 2 built in S1-1").
- [ ] **Step 4: Run** — Expected: PASS.
- [ ] **Step 5: Commit** `feat(storehub,migrator): store tokens come from Cloud.Auth`

---

### Task 8: Live run, docs, counts, review

**Files:**
- Modify: `CLAUDE.md` (project list: `IndyPOS.Cloud.Auth`, its test project; counts), `ONBOARDING.md` (the new Aspire resource, test suite, dev service-client secret), `docs/operations/cloud-deployment.md` (Auth settings: `OpenIddict:Issuer`, RSA key, `TlsTerminatedUpstream`, `ServiceClients:CloudApi:ClientSecret`, `auth-db` and its role; CloudApi `Auth:Issuer`), `docs/architecture/overview.md` if it lists services

- [ ] **Step 1: Live run** (testing-indypos): `--store GeneralHardware`; Auth healthy; StoreHub gets a token **from Auth** (Auth log) and its heartbeat and events reach CloudApi; restart `cloud-auth` and confirm the next heartbeat still succeeds without CloudApi restarting. Register a test store through `/admin/stores/register` (dev admin StoreHubJwt) and get a token with the returned secret; disable it through Auth's API with the service token and confirm `invalid_client`.
- [ ] **Step 2:** Full solution with TRX — Expected: 0 failures. Update counts.
- [ ] **Step 3: Commit** `docs: Cloud.Auth service, settings and suite counts`
- [ ] **Step 4:** Independent multi-lens review **before push**; fix Critical/Important RED→GREEN; push; PR with auto-merge; handle Codex threads.
