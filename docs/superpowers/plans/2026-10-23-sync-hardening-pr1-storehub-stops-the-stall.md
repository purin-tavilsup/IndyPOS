# Sync hardening PR 1: StoreHub stops the stall — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** One refused outbox event can never again hold back the events behind it, and no outage, however long, turns a good event into a failed one.

**Architecture:** CloudApi marks its own deliberate refusals. `HttpCloudSyncClient` turns every answer into one of three outcomes (Sent, RetryLater, Refused). `SyncWorker` marks Refused events `Failed` (kept, with the reason) and retries everything else forever with backoff capped at 15 minutes. The migrator refuses a `--store-id` that is not the installed StoreHub's.

**Tech Stack:** .NET 10, EF Core + Npgsql, xUnit, FluentAssertions, Moq, System.CommandLine, Testcontainers (StoreHub and CloudApi integration suites).

**Spec:** `docs/superpowers/specs/2026-10-23-sync-hardening-design.md` (PR 1 of 3; read it first).

**Branch:** `feat/sync-hardening-pr1` from `development`.

## Decision for Pond before Task 6

The spec says the migrator also checks `--store-id` against the token's `store_id` "when the cloud is
reachable". The migrator's only cloud code today (`SqliteMigrationService.SyncToCloudAsync`) cannot get
a token: it posts to `/connect/token` (the cloud's endpoint is `/oauth/token`) and sends the form as a
JSON body. Cloud slice 1 moves token issuance to IndyPOS.Cloud.Auth, on another host. **This plan does
the configuration half only, and leaves the token half to slice 1**, where the token call is written
once, against Auth. The broken call is recorded as a found defect in the PR description. If Pond wants
the token half now, add it as Task 6b before executing.

## Global Constraints

- Only CloudApi's own **marked** 400/403 refusals are permanent. Any other 4xx, 5xx, 429, network
  error, timeout or missing token is temporary.
- Backoff starts at **30 s**, doubles, **capped at 15 min (900 s)**. No attempt limit.
- `MaxRetries` must not move an event to `Failed` (this plan removes the setting).
- `Failed` events are kept with their payload and the cloud's reason, and are not picked up again.
- Migrations are forward-only (CLAUDE.md): additive, nullable. Run the gate recipe in
  `docs/operations/upgrade-procedure.md` for the new StoreHub migration.
- Store ids compare **ordinally** (the cloud's check is `!=` on strings).
- Never run two `dotnet test` processes at once (the StoreHub test host crashes).
- Test names: `Subject_WhenScenario_ReturnsOutcome` style, as in `HttpCloudSyncClientTests`.

## Review Focus

1. **A cloud that is down for a day** — events keep `Pending`, attempts climb past any old limit, the
   delay stays at 900 s and never overflows. (Task 4: `NextDelay` at attempts 0..100.)
2. **A bare 403/404 from something that is not the sync handler** (auth layer, proxy, missing route)
   — RetryLater, not Failed. (Task 3.)
3. **A refusal reason longer than the column** — stored truncated, never throws and never leaves the
   event `Pending` in a loop. (Task 2.)
4. **A token refresh that ends in a refusal** — 401, then a marked 403 on the retry: Refused.
   (Task 3.)
5. **A migrator run outside the StoreHub folder** — no `appsettings.json` beside it: refuses with a
   message that names `--storehub-config`, never falls back to a machine-name id. (Task 6.)

---

### Task 1: CloudApi marks its own sync refusals

**Files:**
- Create: `src/IndyPOS.Application/UseCases/Cloud/Sync/SyncRefusal.cs`
- Modify: `src/IndyPOS.CloudApi/Endpoints/SyncEventsEndpoints.cs`
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/SyncEventsEndpointTests.cs`

**Interfaces:**
- Produces: `public static class SyncRefusal` in `IndyPOS.Application.UseCases.Cloud.Sync` with
  `const string ExtensionKey = "syncRefusal"`, `const string MalformedBatch = "malformed-batch"`,
  `const string StoreMismatch = "store-mismatch"`. StoreHub reads the same constants (Task 3).

- [ ] **Step 1: Write the failing tests** in `SyncEventsEndpointTests`:
  - `SyncEvents_WithoutAnEventsList_ReturnsAMarkedRefusal`: status 400, content type
    `application/problem+json`, body's `syncRefusal` == `"malformed-batch"`.
  - `SyncEvents_ForAnotherStore_ReturnsAMarkedRefusal`: an event whose payload is
    `{"StoreId":"OTHER"}` posted as `STORE-001` → 403, `syncRefusal` == `"store-mismatch"`.
  - `SyncEvents_WithAPayloadWithoutAStoreId_ReturnsAMarkedRefusal`: payload `{}` → 403,
    `"store-mismatch"` (the spec's "no readable StoreId" case).
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~SyncEventsEndpointTests"` — Expected: the three new tests FAIL (no `syncRefusal`).
- [ ] **Step 3: Implement** — pass `extensions: new Dictionary<string, object?> { [SyncRefusal.ExtensionKey] = ... }` to both `Results.Problem` calls. Replace the endpoint's comment: StoreHub treats only a *marked* 400/403 as permanent (the event becomes `Failed`, kept for resend); any other failure status is retried.
- [ ] **Step 4: Run** the same filter — Expected: all PASS.
- [ ] **Step 5: Commit** `feat(cloud): mark /sync/events refusals so stores can tell them from outages`

---

### Task 2: Outbox `Failed` status with the cloud's reason

**Files:**
- Modify: `src/IndyPOS.Domain/Entities/Core/OutboxEvent.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/OutboxEventConfiguration.cs`
- Create: StoreHub migration `AddOutboxLastError` (via `dotnet ef migrations add`, StoreHub context)
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IOutboxRepository.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/OutboxRepository.cs`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/OutboxRepositoryTests.cs` (new)

**Interfaces:**
- Produces on `OutboxEvent`: `string? LastError` (column `last_error`, max length **1000**).
  `public const int LastErrorMaxLength = 1000;` on `OutboxEvent`.
- Produces on `IOutboxRepository` (replacing `MarkAsFailedAsync`):
  - `Task ScheduleRetryAsync(Guid eventId, DateTime nextRetryUtc, string? reason, CancellationToken cancellationToken = default)` — stays `Pending`, `Attempts + 1`, sets `LastAttemptUtc`, `NextRetryUtc`, `LastError`.
  - `Task MarkAsRefusedAsync(Guid eventId, string reason, CancellationToken cancellationToken = default)` — `Status = "Failed"`, `Attempts + 1`, `LastAttemptUtc = now`, `NextRetryUtc = null`, `LastError = reason` truncated to 1000.
  - `GetFailedCountAsync` now counts `Status == "Failed"` only.

- [ ] **Step 1: Write the failing tests** (`[Collection("Integration")]`, `IntegrationTestBase`; the shared database is not reset, so assert on your own event ids, never on positions or totals; use `batchSize: 10_000` when reading pending events):
  - `GetPendingEventsAsync_WithRefusedEvents_DoesNotReturnThem`: ten events marked refused + one pending → result contains the pending id and none of the ten.
  - `MarkAsRefusedAsync_WithAReason_StoresItAsFailed`: status `"Failed"`, `LastError` equals the reason, `NextRetryUtc` null, `Attempts` 1.
  - `MarkAsRefusedAsync_WithAReasonLongerThanTheColumn_StoresItTruncated`: 1,500-char reason → `LastError.Length` == 1000.
  - `ScheduleRetryAsync_WithAReason_KeepsTheEventPending`: status `"Pending"`, `NextRetryUtc` as given, `LastError` set.
  - `GetFailedCountAsync_WithARetriedPendingEvent_DoesNotCountIt`: count before/after scheduling a retry on a new event is unchanged; after refusing it, +1.
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~OutboxRepositoryTests"` — Expected: FAIL to compile (new members).
- [ ] **Step 3: Implement** the entity property, configuration (`HasColumnName("last_error").HasMaxLength(1000)`), migration `AddOutboxLastError` (nullable column only), and the repository members above. Update every caller of `MarkAsFailedAsync` (only `SyncWorker`, keep its current behaviour for now: `ScheduleRetryAsync(id, next, reason: null)`).
- [ ] **Step 4: Run** the filter, then the whole StoreHub suite once — Expected: PASS.
- [ ] **Step 5: Gate** — run the forward-only recipe for `AddOutboxLastError` from `docs/operations/upgrade-procedure.md` (apply the schema, write a complete sale with only the pre-existing columns). Expected: the sale is written.
- [ ] **Step 6: Commit** `feat(storehub): Failed outbox status with the cloud's reason`

---

### Task 3: Classify every send into Sent, RetryLater or Refused

**Files:**
- Create: `src/IndyPOS.Application/Abstractions/StoreHub/Services/SyncSendResult.cs`
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/Services/ICloudSyncClient.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/HttpCloudSyncClient.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StubCloudSyncClient.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/SyncWorker.cs` (compile only)
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Services/HttpCloudSyncClientTests.cs`

**Interfaces:**
- Consumes: `SyncRefusal` (Task 1).
- Produces:
  ```csharp
  public enum SyncSendOutcome { Sent, RetryLater, Refused }
  public sealed record SyncSendResult(SyncSendOutcome Outcome, string? Reason = null)
  {
      public static readonly SyncSendResult Sent = new(SyncSendOutcome.Sent);
      public static SyncSendResult RetryLater(string reason) => new(SyncSendOutcome.RetryLater, reason);
      public static SyncSendResult Refused(string reason) => new(SyncSendOutcome.Refused, reason);
  }
  ```
  `ICloudSyncClient.SendEventAsync` returns `Task<SyncSendResult>`. A Refused reason carries the
  status code and the problem's `detail`, e.g. `"403 store-mismatch: Event … is not for store …"`.

- [ ] **Step 1: Rewrite the tests** to assert `Outcome` (and that Refused has a non-empty `Reason`). Existing `…ReturnsFalse` become `…ReturnsRetryLater`, `…ReturnsTrue` become `…ReturnsSent`, except as below. New or changed:
  - `SendEventAsync_WhenTheCloudRefusesTheBatchAsMalformed_ReturnsRefused` (400 + `syncRefusal: malformed-batch`).
  - `SendEventAsync_WhenTheCloudRefusesTheStore_ReturnsRefused` (403 + `store-mismatch`).
  - `SendEventAsync_WhenTheCloudForbidsTheEvent_ReturnsRetryLater` (bare 403, empty body — the existing test, outcome changed).
  - `SendEventAsync_WithAProblemThatIsNotASyncRefusal_ReturnsRetryLater` (403 problem+json without `syncRefusal`).
  - `[Theory] SendEventAsync_WithAnUnmarkedFailureStatus_ReturnsRetryLater` for 400, 404, 408, 413, 429, 500, 503.
  - `SendEventAsync_WhenTheRetryAfterA401IsRefused_ReturnsRefused`.
  - `SendEventAsync_WhenNoTokenCanBeObtained_ReturnsRetryLater`.
  - `SendEventAsync_WhenTheNetworkFails_ReturnsRetryLater` (handler throws `HttpRequestException`).
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~HttpCloudSyncClientTests"` — Expected: FAIL to compile.
- [ ] **Step 3: Implement** `SyncSendResult`, the new interface signature, and the classification in `HttpCloudSyncClient` (read the problem body only on 400/403; anything unreadable is RetryLater). Stub returns `SyncSendResult.Sent`. In `SyncWorker`, keep today's behaviour: `Sent` → `MarkAsSentAsync`, anything else → the existing failure path.
- [ ] **Step 4: Run** the filter, then `dotnet build` — Expected: PASS, 0 errors.
- [ ] **Step 5: Commit** `feat(storehub): classify cloud answers into sent, retry later and refused`

---

### Task 4: SyncWorker — refused events step aside, outages retry forever

**Files:**
- Create: `src/IndyPOS.Infrastructure/Services/StoreHub/SyncRetryPolicy.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/SyncWorker.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/SyncWorkerOptions.cs`
- Modify: any `appsettings*.json` with a `SyncWorker:MaxRetries` key (remove it)
- Create: `tests/IndyPOS.Application.Tests/StoreHub/Services/InMemoryOutboxRepository.cs`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Services/SyncWorkerTests.cs`, `SyncRetryPolicyTests.cs` (new)

**Interfaces:**
- Consumes: `SyncSendResult` (Task 3); `ScheduleRetryAsync`, `MarkAsRefusedAsync` (Task 2).
- Produces: `public static TimeSpan SyncRetryPolicy.NextDelay(int attemptsSoFar, SyncWorkerOptions options)`;
  `SyncWorkerOptions.MaxRetryDelaySeconds` (default **900**); `MaxRetries` removed.
- `InMemoryOutboxRepository : IOutboxRepository` (test helper) keeps events in a list and answers
  `GetPendingEventsAsync` with **the real query's rules**: `Status == "Pending"`, `NextRetryUtc` null
  or due, oldest `CreatedUtc` first, `Take(batchSize)`.

- [ ] **Step 1: Write the regression test first, on today's worker logic:**
  `SyncWorker_WithTenRefusedEventsAheadOfANewOne_StillSendsTheNewOne` — the in-memory repository
  holds ten old events with `Attempts = 4` that the mock client answers `Refused`, and one newer event
  it answers `Sent`. Options: `BatchSize = 10`, `PollingIntervalSeconds = 1`. Start the worker, wait up
  to 10 s for the newer event to be `Sent`. Assert it is `Sent` and the ten are `Failed`.
- [ ] **Step 2: Run it** — `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~SyncWorker_WithTenRefusedEventsAheadOfANewOne"` — Expected: **FAIL** (timeout: today's max-retries path writes nothing, so the ten fill every batch). This is the spec's "test that shows it". Do not continue until it fails for that reason.
- [ ] **Step 3: Write the remaining tests:**
  - `SyncRetryPolicyTests.NextDelay_WithAttempts_ReturnsTheDoublingDelayCappedAt15Minutes` — `[Theory]` attempts 0→30 s, 1→60 s, 2→120 s, 3→240 s, 4→480 s, 5→900 s, 6→900 s, 100→900 s.
  - `SyncWorker_WhenTheCloudIsUnreachableAfterManyAttempts_KeepsTheEventPending` — event with `Attempts = 50`, client answers RetryLater → `ScheduleRetryAsync` called, `MarkAsRefusedAsync` never.
  - `SyncWorker_WhenTheCloudRefusesAnEvent_MarksItFailedWithTheReason` — `MarkAsRefusedAsync(id, reason)` with the client's reason.
  - `SyncWorker_WhenSendingThrows_SchedulesARetry` — client throws `InvalidOperationException` → `ScheduleRetryAsync`.
  - Update the two existing tests that use `MarkAsFailedAsync` / `ReturnsAsync(false)`.
- [ ] **Step 4: Implement** `SyncRetryPolicy.NextDelay` (compute in `double` seconds, `Math.Min` with the cap before building the `TimeSpan`, so large attempt counts cannot overflow) and the worker: `Sent` → `MarkAsSentAsync`; `Refused` → `MarkAsRefusedAsync` + a warning log with id, type and reason; `RetryLater` or an exception (not cancellation) → `ScheduleRetryAsync(now + NextDelay(event.Attempts), reason)`. Remove `MaxRetries`.
- [ ] **Step 5: Run** `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~SyncWorker|FullyQualifiedName~SyncRetryPolicy"` — Expected: all PASS, including the regression test.
- [ ] **Step 6: Commit** `fix(storehub): a refused event no longer stalls sync, outages retry forever`

---

### Task 5: Cloud ordering audit after a skipped event

**Files:**
- Read: `src/IndyPOS.CloudApi/Infrastructure/EventProcessor.cs`, `CashDrawerMirror`, `PayLaterMirror`
- Test (only if a gap is found): the matching `tests/IndyPOS.CloudApi.IntegrationTests/*` file

A `Failed` event is now skipped, so later events can reach the cloud before it. For each handled type
(`InvoiceCompleted`, `InvoiceReprinted`, `CashPayoutChanged`, `CashFloatChanged`,
`DebtRepaymentChanged`, `CashCountChanged`, `PayLaterChanged`, `PayLaterRepaid`), decide which
outcome an early arrival of a dependent event has: **delayed** (throws, inbox retries until the
missing row arrives), **tolerated** (newest-wins upsert), or **corrupted**.

- [ ] **Step 1:** Write the table (type → depends on → outcome → evidence: file:line or an existing test name) into the PR description draft.
- [ ] **Step 2:** For every row marked from reading only, where no existing test proves it, add one integration test that sends the dependent event first and the prerequisite second, and asserts the final mirror row. Run the CloudApi suite. Expected: PASS.
- [ ] **Step 3:** If any row is **corrupted**: stop, do not fix it here, and bring it to Pond.
- [ ] **Step 4: Commit** (only if tests were added) `test(cloud): an event arriving before its prerequisite ends correct`

---

### Task 6: Migrator refuses a store id that is not the installed StoreHub's

**Files:**
- Create: `src/IndyPOS.MigrationTool/Services/StoreHubStoreIdCheck.cs`
- Modify: `src/IndyPOS.MigrationTool/Program.cs`
- Modify: `docs/operations/pilot-checklist.md`, `docs/operations/store-installation-guide.md`
- Test: `tests/IndyPOS.MigrationTool.Tests/StoreHubStoreIdCheckTests.cs` (new, pure unit)

**Interfaces:**
- Produces:
  ```csharp
  public sealed record StoreIdCheckResult(bool IsMatch, string Message);
  public static class StoreHubStoreIdCheck
  {
      public static StoreIdCheckResult Check(string storeId, string storeHubConfigPath);
  }
  ```
  Reads `Store:Id` the way StoreHub does (`ConfigurationBuilder().AddJsonFile(path)`, key
  `Store:Id`). New option `--storehub-config` (default `Path.Combine(AppContext.BaseDirectory, "appsettings.json")`; the runbooks run the tool from the StoreHub folder).

- [ ] **Step 1: Write the failing tests** (temp files under `Path.GetTempPath()`, deleted after):
  - `Check_WhenTheIdsMatch_ReturnsAMatch`.
  - `Check_WhenTheIdsDiffer_ReturnsAMismatchNamingBoth` — message contains both ids.
  - `Check_WhenTheIdsDifferOnlyInCase_ReturnsAMismatch` — `STORE-001` vs `store-001`.
  - `Check_WhenTheConfigFileIsMissing_ReturnsAMismatchNamingTheOption` — message contains `--storehub-config`.
  - `Check_WhenStoreIdIsNotSet_ReturnsAMismatch` — `{"Store":{"Type":"Minimart"}}`; message says Store:Id is not set (the machine-name fallback does not count).
  - `Check_WhenStoreIdIsBlank_ReturnsAMismatch`.
- [ ] **Step 2: Run** `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "FullyQualifiedName~StoreHubStoreIdCheckTests"` — Expected: FAIL to compile.
- [ ] **Step 3: Implement** the check and wire it into the root command's handler **before** the configuration table and before a dry run, so a dry run reports it too: on mismatch print the message in red and set `context.ExitCode = 1`. The `verify` subcommand is unchanged.
- [ ] **Step 4: Run** the filter — Expected: PASS. Then run the tool by hand once with a mismatching `--store-id` and a temp `--storehub-config`; Expected: red message, exit code 1, nothing written.
- [ ] **Step 5:** Add `--storehub-config` (and when it is needed) to the migrator commands in both runbooks.
- [ ] **Step 6: Commit** `feat(migrator): refuse a --store-id that is not the installed StoreHub's`

---

### Task 7: Counts, docs and the whole suite

**Files:**
- Modify: `CLAUDE.md` (suite totals and per-suite counts, in the existing style), `ONBOARDING.md` if it repeats them

- [ ] **Step 1:** Run the full solution once, Docker running: `dotnet test --logger trx --results-directory <scratch dir>` — Expected: 0 failures (note any known flake by name with its TRX message).
- [ ] **Step 2:** Update the totals in `CLAUDE.md` from the measured per-suite numbers, saying which suites grew and by how much.
- [ ] **Step 3: Commit** `docs: suite counts after sync hardening PR 1`
- [ ] **Step 4:** Independent multi-lens review **before push** (standing rule): test integrity (does the regression test fail on `development`?), spec fidelity (each Global Constraint), doc claims. Fix findings, then push, open the PR with the Task 5 table and the found migrator token defect in its description, and turn on auto-merge.
