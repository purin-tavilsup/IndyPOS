# Store Users Keep Their Cloud Id — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A StoreHub user that comes from the cloud has the **same `Id` as its cloud `Users` row**,
so every user id the store sends up (`Invoices.UserId`, `InvoiceReprints.CreatedByUserId`, and the
cash mirror's `CreatedByUserId` / `LastModifiedByUserId` in D2) joins cloud `Users.Id` directly.

**Architecture:** Two small changes on StoreHub. `UserSyncService.MapToStoreUser` stops minting
`Guid.NewGuid()` and uses the cloud id. `StoreUserRepository.UpsertByCloudIdAsync` also recognises a
**migrated** user — a row whose `Id` already equals the cloud id but whose `CloudUserId` was never
set — and links it instead of inserting a second row. No migration, no cloud change.

**Tech Stack:** C# .NET 10, EF Core 10 + Npgsql, xUnit + FluentAssertions + Moq, Testcontainers
Postgres (`StoreHub.IntegrationTests`).

**Spec:** `docs/superpowers/specs/2026-09-20-cash-payout-float-persistence-design.md` §7 "Joinable
ids": `CreatedByUserId` / `LastModifiedByUserId` "should join cloud `Users` for the cashier's name —
verify at plan time that StoreHub user ids equal the cloud master ids". Verified 2026-10-18: they do
**not**, except for users migrated from v3. Pond: "let's fix". Runs **before** cash drawer D2.

## What the survey found (2026-10-18)

- `UserSyncService.MapToStoreUser` (`src/IndyPOS.Infrastructure/Services/StoreHub/UserSyncService.cs:88-94`)
  creates `Id = Guid.NewGuid()` and keeps the cloud id in `StoreUser.CloudUserId`.
- Migrated users go the other way: the MigrationTool creates the `StoreUser` (`SqliteMigrationService.cs:273`)
  and bulk-uploads it, and the cloud stores **that** `Id` as `CloudUser.Id`. The store row's
  `CloudUserId` is never set.
- `UpsertByCloudIdAsync` matches on `CloudUserId` only. So when the cloud sends a migrated user
  back, it finds nothing and **inserts a second row** with the same `Username`, which the unique
  index `(StoreId, Username)` refuses. `UserSyncService` logs the error and moves on, so the
  migrated user is never linked and never gets cloud updates (name, role, deactivation).
  **Suspected, not yet proven** — Task 2's first test proves or disproves it.

## Global Constraints

- **The store id IS the cloud id** for every user that arrives from the cloud from now on.
- **Users already synced with a random id keep it.** `StoreUser.Id` is referenced by invoices,
  cash rows and inventory movements; re-keying it is a data migration, not this change. Real stores
  are not on v4 yet (Phase B), so only dev and rehearsal databases hold such rows.
- **Matching order in `UpsertByCloudIdAsync`:** first `CloudUserId == user.CloudUserId`; if none,
  `Id == user.CloudUserId && CloudUserId == null` (the migrated user). An update **keeps the
  password and local-only fields**, as today, and sets `CloudUserId` when linking.
- **The local seeded admin** (`InitialAdminSeeder`) has no cloud user; it stays unlinked. Out of scope.
- **Test naming:** `Subject_WhenScenario_DirectVerbOutcome`, never `Should`; one behaviour per test;
  negative first; named constants; AAA blank lines; dots vertically aligned.
- **Docker must be running** for Task 2.

## Review Focus

1. **A migrated user arriving from the cloud** must be linked, not duplicated, and keep its
   password. → Task 2 `…_WithAMigratedUser_LinksItInsteadOfAddingAnother`, `…_WithAMigratedUser_KeepsItsPassword`.
2. **A user synced before this fix** (random id, `CloudUserId` set) must keep its id when the cloud
   sends it again. → Task 2 `…_WithAUserSyncedBeforeTheFix_KeepsItsStoreId`.
3. **A cloud user whose id happens to equal an unrelated row already linked elsewhere** must not be
   merged into it. → Task 2 `…_WithAnIdHeldByAUserLinkedToAnotherCloudUser_LeavesThatUserAlone`.
4. **A deactivated cloud user** still updates `IsActive` on a migrated row once linked. → Task 2
   `…_WithAMigratedUserDeactivatedInTheCloud_DeactivatesIt`.
5. **A brand-new cloud user** is stored under the cloud id. → Task 2 `…_WithANewUser_StoresItUnderTheCloudId`.

---

### Task 1: The sync maps the cloud id onto the store user

**Files:**
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/UserSyncService.cs`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Services/UserSyncServiceTests.cs`

**Interfaces:**
- Produces: `MapToStoreUser` sets `Id = cloudUser.Id` (and `CloudUserId = cloudUser.Id`, unchanged).

- [ ] **Step 1: Write the failing test** `SyncUsersFromCloudAsync_WithANewUser_GivesItTheCloudId`
  — one `CloudUserDto` with a named `CloudId`; capture the `StoreUser` passed to
  `UpsertByCloudIdAsync` (`Callback`) and assert `.Id.Should().Be(CloudId)`. Match the file's
  existing setup.

- [ ] **Step 2: Run, expect FAIL** (`Id` is a fresh Guid).
  Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~UserSyncServiceTests"`

- [ ] **Step 3: Set `Id = cloudUser.Id`** in `MapToStoreUser`, with a WHY comment: the cloud's user
  id is the one every synced row is joined on.

- [ ] **Step 4: Run, expect PASS.** Same command.

- [ ] **Step 5: Commit** — `fix(storehub): a user synced from the cloud keeps its cloud id`

### Task 2: The upsert links a migrated user instead of duplicating it

**Files:**
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/StoreUserRepository.cs:98-122`
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IStoreUserRepository.cs:62` (doc comment)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Persistence/StoreUserUpsertTests.cs` (create; `IntegrationTestBase`, `GetDbContext()`)

**Interfaces:**
- Consumes: Task 1's mapping (the incoming `StoreUser` has `Id == CloudUserId`).
- Produces: `UpsertByCloudIdAsync(StoreUser user, CancellationToken)` — signature unchanged.

- [ ] **Step 1: Write the failing tests.** Seed rows directly through `GetDbContext()` (a migrated
  row = `Id = MigratedId`, `CloudUserId = null`, `Username = "somchai"`, a non-empty
  `PasswordHash`, `LegacyUserId = NextLegacyUserId()`), then call the repository with the user as
  `UserSyncService` would now build it (`Id = CloudUserId = MigratedId`). Unique usernames per
  test (the database is shared, not reset). Negative first:
  - `UpsertByCloudIdAsync_WithAMigratedUser_LinksItInsteadOfAddingAnother` — no throw; exactly one
    row with that username; its `CloudUserId == MigratedId`. **This is the RED that proves the
    suspected bug** — record the exception message (expected `23505` on `(store_id, username)`).
  - `…_WithAMigratedUser_KeepsItsPassword` — `PasswordHash` unchanged.
  - `…_WithAMigratedUserDeactivatedInTheCloud_DeactivatesIt` — incoming `IsActive = false` → stored false.
  - `…_WithAUserSyncedBeforeTheFix_KeepsItsStoreId` — row `Id = OldRandomId`, `CloudUserId = CloudId`;
    incoming `Id = CloudId` → the row's `Id` is still `OldRandomId`, one row.
  - `…_WithAnIdHeldByAUserLinkedToAnotherCloudUser_LeavesThatUserAlone` — row `Id = X`,
    `CloudUserId = OtherCloudId`; incoming `Id = CloudUserId = X` → that row's name and
    `CloudUserId` unchanged. (The insert then fails on the primary key; assert it throws
    `DbUpdateException`, which `UserSyncService` already logs per user.)
  - `…_WithANewUser_StoresItUnderTheCloudId` — no row; incoming → row with `Id == CloudId`.
  - `…_WithAUserAlreadyLinked_UpdatesItsName` — today's behaviour, kept.

- [ ] **Step 2: Run, expect FAIL** on the migrated-user tests (and read the exception).
  Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~StoreUserUpsertTests"`

- [ ] **Step 3: Change `UpsertByCloudIdAsync`** to the Global-Constraints matching order: look up by
  `CloudUserId`; if null, look up `Id == user.CloudUserId && CloudUserId == null`; when that second
  lookup hits, set `existing.CloudUserId = user.CloudUserId` before the usual field updates. Keep it
  ≤20 lines by extracting `private Task<StoreUser?> FindForSyncAsync(Guid? cloudUserId, CancellationToken)`.
  Comment WHY: migrated users were uploaded with their store id, so the cloud sends them back under
  it with no `CloudUserId` on the store side. Update the interface doc comment to the new order.

- [ ] **Step 4: Run, expect PASS.** Same command.

- [ ] **Step 5: Run the full solution with TRX, nothing else running**; update CLAUDE.md and
  ONBOARDING.md counts (Application +1, StoreHub +7; StoreHub's Docker-down split +7, DERIVED).
  Run: `dotnet test --logger trx --results-directory <scratchpad>/trx`

- [ ] **Step 6: Commit** — `fix(storehub): user sync links a migrated user instead of adding a second one`,
  then `docs: counts after the user-id fix`; PR to `development`, Codex review, then `--auto` merge.
