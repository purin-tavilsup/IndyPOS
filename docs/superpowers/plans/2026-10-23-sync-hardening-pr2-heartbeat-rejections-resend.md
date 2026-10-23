# Sync hardening PR 2: heartbeat, rejection log and resend — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The cloud knows, per store, whether StoreHub is up and syncing and which events it refused, and an admin can ask a store to resend its failed events, which the store picks up on its own.

**Architecture:** StoreHub sends a heartbeat every 2 minutes (counts, oldest pending age, up to 50 failed events, completed resend requests). CloudApi keeps one row per store, logs every refusal it gives on `/sync/events` (resolved when that event is later accepted), and answers each heartbeat with the store's open resend requests. A pure evaluator turns heartbeat + rejections into OK / Behind / Failed events / Offline. Two admin endpoints expose that and create resend requests; the page itself is PR 3.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core + Npgsql (jsonb), xUnit, FluentAssertions, Moq, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-23-sync-hardening-design.md` (PR 2 of 3: Heartbeat, Rejection log, Resend, Store status). PR 1 (#148) is merged: `Failed` status, `LastError`, `SyncSendResult`, `SyncRefusal`.

**Branch:** `feat/sync-hardening-pr2` from `development`.

## Global Constraints

- Heartbeat every **2 minutes**, in its **own** background worker; it never blocks or delays a sale or the sync worker. A failed heartbeat is logged and the next one goes on schedule.
- Heartbeat carries: StoreHub version, `Pending` count, `Failed` count, oldest `Pending` event's age, the **oldest 50** `Failed` events (id, type, failed-at, reason), and the resend requests completed since the last heartbeat with the event ids they moved.
- The store is the source of truth for its own `Failed` events. The rejection log fills the gap when the store cannot get a heartbeat through.
- One heartbeat row per store, overwritten. No history.
- **Keep it light** (Pond, 2026-10-23): interval stays **2 minutes**. `FailedEvents` and `CompletedResends` are sent
  as null (omitted) when empty, so a normal heartbeat is about 0.5 KB; each `FailedEventSummary.Reason` in the
  heartbeat is cut to **200** characters (the full reason stays in StoreHub's `last_error`), without splitting a
  surrogate pair. Worst case (50 failed events) stays near 15 KB.
- Rejection log: per identifiable event, store (from the token), event id, type, status, reason, first/last time, count. Same event refused again updates its row. An unreadable batch is logged once with no event id. **Resolved** when that event is later accepted by `/sync/events`; resolved rows stay as history.
- Resend: "all" (store resends every `Failed` event it has) or one event id. An open request is not duplicated. Store moves `Failed` → `Pending` with attempts and backoff reset, reports the moved ids; cloud marks the request completed. An id the store does not have, or that is not `Failed`, is ignored and reported back, not an error.
- Status: 🟢 OK = seen in last **10 min**, no failed, no pending or oldest pending under **30 min**; 🟡 Behind = seen in last 10 min, oldest pending ≥ 30 min; 🔴 Failed events = latest heartbeat reports failed **or** an unresolved rejection newer than the latest heartbeat; ⚪ Offline = not seen for 10 min (neutral). 🔴 wins. Newer source wins when they disagree. Thresholds are settings with these defaults.
- Wire contract between versions: both sides ignore fields they do not know (System.Text.Json default); never make a new field required.
- Store token for the heartbeat (same as `/sync/events`). Admin endpoints use today's `SystemAdminOnly` policy; cloud slice 1 swaps it for an Admin person token.
- Cloud migrations are forward-only: new tables only. Run the **cloud** gate recipe in `docs/operations/upgrade-procedure.md`.
- Never run two `dotnet test` processes at once.

## Review Focus

1. **Heartbeat while the cloud is down for hours** — the worker keeps its 2-minute schedule, logs, never throws, never touches the outbox. (Task 7.)
2. **A resend request for an event id that is Pending, Sent or unknown** — ignored, reported back as not moved, request still completed. (Tasks 6, 7.)
3. **An older StoreHub (no heartbeat) or a newer one (extra fields)** — cloud accepts a heartbeat with unknown fields; store accepts a reply with unknown fields. (Tasks 3, 7.)
4. **A store refused, then the cause fixed and the event accepted** — its rejection resolves and the store leaves 🔴 even before its next heartbeat. (Tasks 4, 5.)
5. **A store registered but never seen** — listed as ⚪ "never seen", not missing and not 🔴. (Task 5.)

---

### Task 1: Heartbeat contract and the store status evaluator

**Files:**
- Create: `src/IndyPOS.Application/UseCases/Cloud/Sync/Heartbeat/HeartbeatContracts.cs`
- Create: `src/IndyPOS.Application/UseCases/Cloud/Sync/Heartbeat/StoreSyncHealth.cs`
- Test: `tests/IndyPOS.Application.Tests/UseCases/Cloud/Sync/StoreSyncHealthTests.cs`

**Interfaces:**
- Produces (records, all in `IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat`):
  ```csharp
  public sealed record FailedEventSummary(Guid EventId, string EventType, DateTime? FailedAtUtc, string? Reason);
  public sealed record CompletedResend(Guid RequestId, IReadOnlyList<Guid> MovedEventIds, IReadOnlyList<Guid> IgnoredEventIds);
  public sealed record HeartbeatRequest(string? StoreHubVersion, int PendingCount, int FailedCount,
      DateTime? OldestPendingCreatedAtUtc, IReadOnlyList<FailedEventSummary>? FailedEvents, IReadOnlyList<CompletedResend>? CompletedResends);
  public sealed record ResendInstruction(Guid RequestId, Guid? EventId);   // EventId null = all Failed
  public sealed record HeartbeatResponse(IReadOnlyList<ResendInstruction> Resends);
  public const int MaxFailedEventsPerHeartbeat = 50;  // on a static class HeartbeatLimits
  public enum StoreSyncState { Ok, Behind, FailedEvents, Offline }
  public sealed record StoreSyncHealthOptions { int OnlineWindowMinutes = 10; int BehindAfterMinutes = 30; }
  public static StoreSyncState StoreSyncHealth.Evaluate(DateTime? lastSeenUtc, int failedCount,
      DateTime? oldestPendingCreatedAtUtc, DateTime? newestUnresolvedRejectionUtc, DateTime nowUtc, StoreSyncHealthOptions options);
  ```
- [ ] **Step 1: Write the failing tests** (`now = 2026-10-23T12:00Z`; one behaviour each):
  - `Evaluate_WhenSeenRecentlyWithNothingWaiting_ReturnsOk` (seen 1 min ago, 0 failed, no pending).
  - `Evaluate_WhenTheOldestPendingIsUnder30Minutes_ReturnsOk` (29 min).
  - `Evaluate_WhenTheOldestPendingIs30MinutesOrMore_ReturnsBehind` (exactly 30).
  - `Evaluate_WhenTheHeartbeatReportsFailedEvents_ReturnsFailedEvents`.
  - `Evaluate_WhenOfflineWithFailedEvents_ReturnsFailedEvents` (🔴 wins).
  - `Evaluate_WhenARejectionIsNewerThanTheHeartbeat_ReturnsFailedEvents`.
  - `Evaluate_WhenTheHeartbeatIsNewerThanAnUnresolvedRejection_ReturnsOk` (newer source wins).
  - `Evaluate_WhenNotSeenFor10Minutes_ReturnsOffline` (exactly 10).
  - `Evaluate_WhenNeverSeen_ReturnsOffline`.
  - `Evaluate_WithCustomThresholds_UsesThem` (Behind at 5 min).
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreSyncHealthTests"` — Expected: FAIL to compile.
- [ ] **Step 3: Implement** the records and `Evaluate` (order: failed or newer rejection → FailedEvents; not seen within window → Offline; oldest pending ≥ behind → Behind; else Ok).
- [ ] **Step 4: Run** the filter — Expected: PASS.
- [ ] **Step 5: Commit** `feat(sync): heartbeat contract and store sync status rules`

---

### Task 2: Cloud tables — heartbeats, rejections, resend requests

**Files:**
- Create: `src/IndyPOS.CloudApi/Domain/StoreSyncHealth.cs` (entities `CloudStoreHeartbeat`, `CloudSyncRejection`, `CloudResendRequest`)
- Create: `src/IndyPOS.CloudApi/Infrastructure/SyncHealthModel.cs` (static `Configure(ModelBuilder)`, called from `CloudDbContext.OnModelCreating` beside `PayLaterMirrorModel`)
- Modify: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs` (three `DbSet`s)
- Create: cloud migration `AddStoreSyncHealth`
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/SyncHealthSchemaTests.cs`

**Interfaces:**
- `CloudStoreHeartbeat` → table `StoreHeartbeats`: `StoreId` (PK, 50), `ReceivedAtUtc`, `StoreHubVersion` (50, null), `PendingCount`, `FailedCount`, `OldestPendingCreatedAtUtc` (null), `FailedEventsJson` (`jsonb`, null; the serialized `FailedEventSummary` list).
- `CloudSyncRejection` → `SyncRejections`: `Id` (Guid PK), `StoreId` (50), `EventId` (Guid, null = whole batch), `EventType` (100, null), `StatusCode`, `Reason` (1000), `FirstRejectedAtUtc`, `LastRejectedAtUtc`, `Count`, `ResolvedAtUtc` (null). Unique index `(StoreId, EventId)` filtered `"EventId" IS NOT NULL`; index `(StoreId, ResolvedAtUtc)`.
- `CloudResendRequest` → `SyncResendRequests`: `Id` (Guid PK), `StoreId` (50), `EventId` (Guid, null = all), `RequestedAtUtc`, `CompletedAtUtc` (null), `MovedEventIdsJson` / `IgnoredEventIdsJson` (`jsonb`, null). Index `(StoreId, CompletedAtUtc)`.
- Every table and column carries a `HasComment` (agents read the schema; follow `PayLaterMirrorModel`).
- [ ] **Step 1: Write the failing tests** (fresh database per test via `CloudPostgresFixture`):
  - `StoreHeartbeats_AfterMigrating_HasOneRowPerStore` (insert two rows with one `StoreId` → `DbUpdateException`).
  - `SyncRejections_AfterMigrating_AllowsOneRowPerStoreAndEvent` (duplicate `(StoreId, EventId)` → throws; two rows with `EventId` null → allowed).
  - `SyncHealthTables_AfterMigrating_HaveCommentsOnEveryColumn` (query `pg_description` like `PayLaterMirrorSchemaTests`).
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~SyncHealthSchemaTests"` — Expected: FAIL to compile.
- [ ] **Step 3: Implement** entities, model, DbSets, and `dotnet ef migrations add AddStoreSyncHealth --project src/IndyPOS.CloudApi` (new tables only).
- [ ] **Step 4: Run** the filter — Expected: PASS.
- [ ] **Step 5: Gate** — cloud recipe: previous release's last migration (`20261019151511_AddCashDrawerDaysView`) → latest; snapshots of `information_schema.columns` and `pg_indexes` differ only by the three tables; then ingest and process one `InvoiceCompleted` writing only pre-existing columns. Record the result paragraph in `upgrade-procedure.md`.
- [ ] **Step 6: Commit** `feat(cloud): store heartbeat, sync rejection and resend request tables`

---

### Task 3: `POST /sync/heartbeats` (store token)

**Files:**
- Create: `src/IndyPOS.CloudApi/Endpoints/SyncHeartbeatEndpoints.cs` (`MapSyncHeartbeats()`), mapped in `Program.cs` beside `MapSyncEvents()`
- Create: `src/IndyPOS.CloudApi/Infrastructure/HeartbeatRecorder.cs`
- Modify: `tests/IndyPOS.CloudApi.IntegrationTests/SyncTestHost.cs` (map the route; `PostHeartbeatAsStoreAsync(storeId, json)`)
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/SyncHeartbeatEndpointTests.cs`

**Interfaces:**
- Consumes: Task 1 contracts, Task 2 entities.
- Produces: `internal static Task<HeartbeatResponse> HeartbeatRecorder.RecordAsync(CloudDbContext db, string storeId, HeartbeatRequest request, DateTime nowUtc, CancellationToken ct)` — upserts the heartbeat (keeps at most 50 failed events), completes the reported resend requests of **this** store (sets `CompletedAtUtc`, moved/ignored ids), returns this store's open requests oldest first.
- Route: `POST /sync/heartbeats`, store from the token's `store_id`; no claim → 403 (as `/sync/status`). Answers 200 with `HeartbeatResponse`.
- [ ] **Step 1: Write the failing tests:**
  - `PostHeartbeat_FromAStore_StoresItsLatestState` (counts, version, oldest pending, failed list read back).
  - `PostHeartbeat_Twice_KeepsOneRowWithTheNewest`.
  - `PostHeartbeat_WithMoreThan50FailedEvents_KeepsTheFirst50`.
  - `PostHeartbeat_WithAnOpenResendRequest_ReturnsIt`.
  - `PostHeartbeat_ReportingACompletedResend_CompletesItAndStopsReturningIt`.
  - `PostHeartbeat_ReportingAnotherStoresResend_LeavesItOpen`.
  - `PostHeartbeat_WithFieldsItDoesNotKnow_IsAccepted` (extra `"futureField": 1`).
  - `PostHeartbeat_WithoutAStoreClaim_ReturnsForbidden`.
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~SyncHeartbeatEndpointTests"` — Expected: FAIL.
- [ ] **Step 3: Implement** recorder and endpoint.
- [ ] **Step 4: Run** the filter — Expected: PASS.
- [ ] **Step 5: Commit** `feat(cloud): POST /sync/heartbeats records store health and hands out resend requests`

---

### Task 4: Rejection log on `/sync/events`

**Files:**
- Create: `src/IndyPOS.CloudApi/Infrastructure/SyncRejectionLog.cs`
- Modify: `src/IndyPOS.CloudApi/Endpoints/SyncEventsEndpoints.cs` (record in both refusal branches)
- Modify: `src/IndyPOS.Application/UseCases/Cloud/Sync/IngestEvents/IngestEventsCommandHandler.cs` **or** the endpoint (builder's choice) to resolve on accept
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/SyncRejectionLogTests.cs`

**Interfaces:**
- Produces: `internal static Task SyncRejectionLog.RecordAsync(CloudDbContext db, string storeId, IReadOnlyList<SyncEventRequest?>? events, int statusCode, string reason, DateTime nowUtc, CancellationToken ct)` — one row per non-null event id (upsert: `Count + 1`, `LastRejectedAtUtc`, reason, `ResolvedAtUtc = null`), or one row with `EventId` null when none can be identified; reason cut to 1000 without splitting a surrogate pair (reuse the PR 1 rule).
- `Task SyncRejectionLog.ResolveAsync(CloudDbContext db, string storeId, IEnumerable<Guid> acceptedEventIds, DateTime nowUtc, CancellationToken ct)` — sets `ResolvedAtUtc` on this store's unresolved rows for those ids. Called for every event the batch reports `Accepted` (new **and** duplicate).
- Recording must never change the refusal the store receives: a failure to log is logged and swallowed (not `OperationCanceledException`).
- [ ] **Step 1: Write the failing tests:**
  - `SyncEvents_ForAnotherStore_LogsARejectionPerEvent` (two events → two rows, status 403, reason contains `store-mismatch`).
  - `SyncEvents_RefusedTwice_CountsTwiceInOneRow`.
  - `SyncEvents_WithoutAnEventsList_LogsOneBatchRejection` (`EventId` null, 400).
  - `SyncEvents_AcceptingARejectedEvent_ResolvesItsRejection` (refuse with wrong payload store, then same id with the right payload → `ResolvedAtUtc` set).
  - `SyncEvents_AcceptingADuplicate_ResolvesItsRejection`.
  - `SyncEvents_WhenTheLogCannotBeWritten_StillReturnsTheRefusal` (drop the `SyncRejections` table in the test database first → still 403 with `syncRefusal`).
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~SyncRejectionLogTests"` — Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** the filter and `SyncEventsEndpointTests` — Expected: PASS.
- [ ] **Step 5: Commit** `feat(cloud): log every sync refusal, resolved when the event is later accepted`

---

### Task 5: Admin endpoints — store sync health and resend requests

**Files:**
- Create: `src/IndyPOS.CloudApi/Infrastructure/StoreSyncHealthQuery.cs`
- Create: `src/IndyPOS.CloudApi/Endpoints/AdminSyncHealthEndpoints.cs` (`MapAdminSyncHealth()`), mapped in `Program.cs`
- Modify: `src/IndyPOS.CloudApi/appsettings.json` (section `SyncHealth`: `OnlineWindowMinutes` 10, `BehindAfterMinutes` 30) bound to `StoreSyncHealthOptions` with `IOptions<T>`
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/StoreSyncHealthQueryTests.cs`, `ResendRequestTests.cs`

**Interfaces:**
- Consumes: Tasks 1–4.
- Produces:
  - `public sealed record StoreSyncHealthView(string StoreId, string? StoreName, StoreSyncState State, DateTime? LastSeenUtc, string? StoreHubVersion, int PendingCount, int FailedCount, DateTime? OldestPendingCreatedAtUtc, IReadOnlyList<FailedEventSummary> FailedEvents, IReadOnlyList<RejectionView> UnresolvedRejections, int CloudUnprocessedEvents, Guid? OpenResendRequestId)` and `RejectionView(Guid? EventId, string? EventType, int StatusCode, string Reason, int Count, DateTime LastRejectedAtUtc)`.
  - `internal static Task<IReadOnlyList<StoreSyncHealthView>> StoreSyncHealthQuery.ListAsync(CloudDbContext db, StoreSyncHealthOptions options, DateTime nowUtc, CancellationToken ct)` — one entry per **registered** store (`StoreConfigs`), heartbeat or not; `CloudUnprocessedEvents` from `StoreSyncStatusQuery`.
  - `internal static Task<(Guid RequestId, bool Created)> ResendRequests.RequestAsync(CloudDbContext db, string storeId, Guid? eventId, DateTime nowUtc, CancellationToken ct)` — returns the open request with the same `(StoreId, EventId)` instead of a new one.
  - `GET /admin/stores/sync-health` → 200 list. `POST /admin/stores/{storeId}/resend-requests` body `{ "eventId": null | guid }` → 201 with `Location` and `{ requestId }`, or 200 with the existing open one; 404 for an unregistered store. Both `.RequireAuthorization("SystemAdminOnly")` with a comment that slice 1 replaces it with the Admin person token.
- [ ] **Step 1: Write the failing tests:**
  - `ListAsync_WithARegisteredStoreNeverSeen_ListsItOffline`.
  - `ListAsync_WithARecentHeartbeat_ReportsItsCountsAndState`.
  - `ListAsync_WithAnUnresolvedRejectionNewerThanTheHeartbeat_ReportsFailedEvents`.
  - `ListAsync_WithOnlyResolvedRejections_DoesNotReportThem`.
  - `ListAsync_WithAnOpenResendRequest_ReportsItsId`.
  - `RequestAsync_Twice_ReturnsTheSameOpenRequest`.
  - `RequestAsync_AfterTheFirstCompleted_CreatesANewOne`.
  - `RequestAsync_ForOneEventAndForAll_KeepsThemApart`.
  - `ResendEndpoint_ForAnUnregisteredStore_ReturnsNotFound`.
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~StoreSyncHealthQueryTests|FullyQualifiedName~ResendRequestTests"` — Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** the filter — Expected: PASS.
- [ ] **Step 5: Commit** `feat(cloud): admin store sync health and resend requests`

---

### Task 6: StoreHub outbox — what a heartbeat reports, and requeue

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IOutboxRepository.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/OutboxRepository.cs`
- Modify: `tests/IndyPOS.Application.Tests/StoreHub/Services/InMemoryOutboxRepository.cs` (same rules)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/OutboxRepositoryTests.cs`

**Interfaces:**
- Produces on `IOutboxRepository`:
  - `Task<DateTime?> GetOldestPendingCreatedUtcAsync(CancellationToken ct = default)`
  - `Task<IReadOnlyList<FailedEventSummary>> GetFailedEventsAsync(int max, CancellationToken ct = default)` — oldest `CreatedUtc` first; `FailedAtUtc` = `LastAttemptUtc`, `Reason` = `LastError`.
  - `Task<IReadOnlyList<Guid>> RequeueFailedAsync(Guid? eventId, CancellationToken ct = default)` — `Failed` → `Pending`, `Attempts = 0`, `NextRetryUtc = null`; `eventId` null = every `Failed` event; returns the ids it moved (an id not `Failed` or unknown moves nothing).
- [ ] **Step 1: Write the failing tests** (shared DB: assert on own ids; read with large limits):
  - `GetFailedEventsAsync_ReturnsRefusedEventsWithTheirReason`.
  - `GetOldestPendingCreatedUtcAsync_IgnoresFailedEvents` (a refused event older than the pending one).
  - `RequeueFailedAsync_ForOneEvent_MovesItBackToPendingWithItsAttemptsReset`.
  - `RequeueFailedAsync_ForAPendingEvent_MovesNothing`.
  - `RequeueFailedAsync_ForAnUnknownEvent_MovesNothing`.
  - `RequeueFailedAsync_ForAll_MovesEveryFailedEvent` (assert own ids are in the result and now `Pending`).
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~OutboxRepositoryTests"` — Expected: FAIL to compile.
- [ ] **Step 3: Implement** (repository with `ExecuteUpdateAsync`; requeue reads the ids first, then updates where still `Failed`, returning what changed).
- [ ] **Step 4: Run** the filter — Expected: PASS.
- [ ] **Step 5: Commit** `feat(storehub): outbox queries for the heartbeat and requeue of failed events`

---

### Task 7: StoreHub heartbeat client and worker

**Files:**
- Create: `src/IndyPOS.Application/Abstractions/StoreHub/Services/ICloudHeartbeatClient.cs`
- Create: `src/IndyPOS.Infrastructure/Services/StoreHub/HttpCloudHeartbeatClient.cs`
- Create: `src/IndyPOS.Infrastructure/Services/StoreHub/HeartbeatWorker.cs`, `HeartbeatOptions.cs` (`SectionName = "Heartbeat"`, `IntervalSeconds = 120`, `Enabled = true`)
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs` (register client with the same base address and `ITokenService` as `HttpCloudSyncClient`, only when a cloud client id is configured; `AddSyncWorker` also adds `HeartbeatWorker` in that case)
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Services/HttpCloudHeartbeatClientTests.cs`, `HeartbeatWorkerTests.cs`

**Interfaces:**
- Consumes: Task 1 contracts; Task 6 repository members.
- Produces: `Task<HeartbeatResponse?> ICloudHeartbeatClient.SendAsync(HeartbeatRequest request, CancellationToken ct = default)` — null on no token, network error, timeout or non-2xx; one 401 retry with a fresh token as `HttpCloudSyncClient` does; never throws except cancellation.
- `HeartbeatWorker : BackgroundService` — each tick: build the request (version = entry assembly's informational version; counts from `GetPendingCountAsync`/`GetFailedCountAsync`; oldest pending; `GetFailedEventsAsync(HeartbeatLimits.MaxFailedEventsPerHeartbeat)`; completed resends carried from the previous tick), send, then for each `ResendInstruction` call `RequeueFailedAsync(eventId)`, recording moved and (for a single id) ignored ids into the next tick's `CompletedResends`. Completed resends are kept until a heartbeat that carried them succeeds. Wakes the sync worker? **No** — requeued events are due immediately and the sync worker's next poll (≤ 5 s) sends them.
- [ ] **Step 1: Write the failing tests:**
  - Light: `Heartbeat_WithNothingFailed_SendsNoFailedList` (FailedEvents and CompletedResends null); `Heartbeat_WithALongReason_SendsItCutTo200Characters`.
  - Client: `SendAsync_WhenTheCloudAnswers_ReturnsItsResends`; `SendAsync_WhenTheReplyHasFieldsItDoesNotKnow_ReturnsItsResends`; `SendAsync_WhenTheNetworkFails_ReturnsNull`; `SendAsync_WhenNoTokenCanBeObtained_ReturnsNull`; `SendAsync_WhenTheCloudAnswers500_ReturnsNull`.
  - Worker (with `InMemoryOutboxRepository`, mocked client): `Heartbeat_ReportsTheOutboxState` (counts, oldest pending, failed list); `Heartbeat_WithAResendForAll_MovesFailedEventsBackToPending`; `Heartbeat_AfterAResend_ReportsItCompletedOnTheNextBeat` (moved ids); `Heartbeat_WithAResendForAnEventThatIsNotFailed_ReportsItIgnored`; `Heartbeat_WhenTheCloudIsDown_KeepsTheCompletedResendForTheNextBeat`; `Heartbeat_WhenSendingThrows_KeepsRunning` (second beat still sent).
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~HttpCloudHeartbeatClientTests|FullyQualifiedName~HeartbeatWorkerTests"` — Expected: FAIL to compile.
- [ ] **Step 3: Implement.** Keep each method ≤ 20 lines; the worker body is a tick method the tests can drive through `StartAsync` with `IntervalSeconds = 1`.
- [ ] **Step 4: Run** the filter, then `dotnet build` — Expected: PASS, 0 errors.
- [ ] **Step 5: Commit** `feat(storehub): heartbeat to the cloud every 2 minutes and apply resend requests`

---

### Task 8: End to end on Aspire, docs, counts, review

**Files:**
- Modify: `docs/operations/cloud-deployment.md` (top section: the heartbeat route, admin routes, the "Proving it" scenarios now runnable), `CLAUDE.md` / `ONBOARDING.md` counts

- [ ] **Step 1: Live run** (testing-indypos skill): `dotnet run --project src/IndyPOS.AppHost -- --store GeneralHardware`; within 2–3 min `StoreHeartbeats` has the store's row (query cloud-db). Then make one outbox event `Failed` by hand (`UPDATE outbox_event SET status='Failed', last_error='manual' WHERE id=…`), call `POST /admin/stores/{storeId}/resend-requests` with an admin token if one can be minted in dev, otherwise insert the request row directly — and watch the event go `Pending` → `Sent` and the request complete. Record what was and was not exercised.
- [ ] **Step 2:** Full solution `dotnet test --logger trx --results-directory <scratch>` — Expected: 0 failures. Update counts in the existing style.
- [ ] **Step 3: Commit** `docs: sync health routes, proving scenarios and suite counts`
- [ ] **Step 4:** Independent multi-lens review **before push** (code review on the most capable model + an adversarial probe reviewer). Fix Critical/Important with RED→GREEN, then push, open the PR with auto-merge on, handle Codex threads.
