# Design: Admin Provisioning — Single-Use Bootstrap Password + Forced Rotation

**Date:** 2026-07-14
**Status:** Draft (pending final review)
**Author:** Pond + Claude (pair)
**Reviewed by:** Architect / Software Engineer / QA sub-agents (2026-07-14)

## Problem

The v4 installer currently collects the **admin login password** (secret #1 of
four — the only *human-facing* one) in the wizard: the operator types a Store ID,
an admin username, and an admin password. That password is:

1. Written **plaintext** into `InitialAdmin:Password` in
   `C:\ProgramData\IndyPOS\v4\StoreHub\appsettings.json` (ACL-locked, but never
   cleared — it lingers forever after the seed).
2. Seeded once as a BCrypt hash by `InitialAdminSeeder` during `StoreHub.exe migrate`.

This works and is VM-validated, but it does not suit a product that will
eventually be installed by **non-technical store staff** (not just the dev):

- Staff shouldn't have to invent a strong password, and we can't rely on them
  choosing one well.
- A human-chosen password reused across stores + persisted in plaintext is a
  standing weakness.

### The other three secrets are already solved

Only the admin login password (#1) is in scope. The DB app-user password (#2),
the Postgres superuser password (#3), and the JWT signing key (#4) are all
machine-to-machine — auto-generated, DPAPI-protected or not persisted, never
typed by a human. See `2026-06-21-vault-dpapi-secret-protection-design.md`.

## Goals

- The installer generates the admin bootstrap password; the operator never
  invents one. Wizard shrinks to **Store-ID-only**.
- The bootstrap password is a **single-use** credential: valid until first
  login, then **force-rotated** to a real password the admin chooses.
- **Server-side enforcement** of the rotation — a must-change session can call
  nothing except change-password, regardless of client.
- No standing plaintext admin secret: the plaintext `InitialAdmin` node is
  removed from `appsettings.json` after the seed.
- A **recovery path** so a lost bootstrap credential is not a permanent lockout.
- Correct behaviour on **re-install / upgrade** over the 3 existing populated
  stores (no dead credential shown, no migration failure).

## Non-Goals

- Login rate-limiting / lockout on repeated failures (future hardening; noted
  because the bootstrap is now single-use, so brute-force value is low).
- Password complexity policy beyond length + "not equal to current" (deliberate:
  this product's risk level does not warrant a policy engine — see Decisions).
- CloudApi-driven user provisioning (Epic I / S2, blocked on CloudApi deploy).
- A full user-management UI (separate future work).
- Outbound sync of the rotated password / flag to CloudApi (noted as a guard to
  add when sync lands; out of scope here).

## Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | **Random shown-once** bootstrap password, not a date-derived template | Since the credential is shown on the finish screen anyway, a random value is strictly stronger and leaks no guessable scheme in the binary. |
| D2 | **+ documented reset path** (`reset-admin` CLI) | A lost finish-screen credential must not be a permanent lockout; also fixes the re-install "dead credential" case. |
| D3 | **Server-side enforced** must-change (a `must_change` token claim gates all routes) | The rotation guarantee must hold regardless of the client. A client-only gate is cosmetic. |
| D4 | Flag carried on the **`AuthResult` / `LoginResponse` envelope**, not `StoreUserDto` | The DTO hydrates `ILoggedInUser` for the whole session; a transient auth-flow flag there would go stale. |
| D5 | Password policy = **length ≥ 8, ≠ current** only | Accepted risk for a small-retail POS; explicitly not an oversight. |

## Threat model / framing

The bootstrap password is intentionally low-value: it is a *provisioning*
credential, valid for exactly one login, immediately rotated, and then removed
from disk. Its only job is to get the admin to the change-password screen. All
real access is gated behind the rotated password. StoreHub listens only on
`127.0.0.1` / the LAN and the clients are ones we control, but the server-side
`must_change` gate (D3) means even a rogue client cannot bypass rotation.

---

## Architecture

Four coordinated slices. Each is independently testable.

### 1. Installer (bootstrapper)

- **Wizard → Store-ID-only.** Remove the Admin Username / Password / Confirm
  fields and their validation from `InstallationWizard`. Username is fixed to
  `admin`.
- **`InstallationConfig`:** drop the `required` on `AdminPassword`; it becomes a
  **random** value generated once (reuse the `GenerateSecret` approach, tuned to
  a human-typable length/charset). Evaluate **once** as a property initializer so
  the seeded value and the finish-screen value are the same instance.
- **Seed → clear sequence** (ordering is already safe — the service is not
  running until a later orchestrator step):
  1. `DatabaseSetup` writes `appsettings.json` with the `InitialAdmin` node
     (plaintext, ACL-locked), alongside the already-DPAPI-protected
     `ConnectionStrings:storehub-db` and `LocalToken:SecretKey`.
  2. Bootstrapper runs `StoreHub.exe migrate` → `InitialAdminSeeder` seeds the
     admin and **reports whether it actually seeded** (vs. skipped an existing
     admin).
  3. On success, bootstrapper performs a **surgical** edit of `appsettings.json`:
     parse → remove **only** the `InitialAdmin` node → write atomically
     (temp-file + rename) → re-apply the ACL. The DPAPI-protected connection
     string and JWT secret are preserved **byte-identical** (no `BuildStoreHubConfigJson`
     rebuild — that would mint a new JWT secret). A failure is **logged**, not
     silently swallowed, and is idempotent-safe on re-run.
- **Finish screen adapts to the seeder report:**
  - Seeded → show `admin` + the random bootstrap password **once**, plus write a
    protected (ACL-locked) summary file, plus the line "you'll set a new password
    on first sign-in."
  - Skipped (admin already existed) → show "existing admin retained — use your
    current password." **Never** display a credential the DB did not adopt.

### 2. Reset / recovery path (new)

- New StoreHub CLI mode **`StoreHub.exe reset-admin`** (sibling to `migrate`):
  generates a fresh random password, **upserts** the admin (create if missing,
  else overwrite the hash), sets `MustChangePassword = true`, prints the new
  password to stdout **once**. Uses the same DB context / hasher as `migrate`.
- Works post-install without a full reinstall. Documented in the ops guide.
- This is also the sanctioned way to recover the 3 existing stores or any box
  where the finish-screen credential was lost.

### 3. Data model + server

- **`StoreUser.MustChangePassword`** (`bool`, C# default `false`).
  - EF migration must emit **`nullable: false, defaultValue: false`** so it
    applies cleanly to the populated `store_user` tables on the 3 existing
    stores (a `migrate` failure is fatal and would abort the upgrade).
- **`InitialAdminSeeder`** sets `MustChangePassword = true` on the seeded admin
  and returns a result indicating seeded-vs-skipped.
- **Flag threading (D4):** `StoreUser.MustChangePassword` →
  `AuthResult` / `AuthenticatedUser` (read the entity value — easy to add the
  field and forget to populate it) → **`LoginResponse`**. Not on `StoreUserDto`.
- **`must_change` token claim.** When authentication succeeds for a user with
  `MustChangePassword == true`, the issued JWT carries a `must_change` claim.
- **Authorization gate.** A small middleware / endpoint filter runs after
  authentication: if the principal carries `must_change`, every route **except
  `POST /auth/change-password`** returns **403**. This is the server-side teeth.
- **`POST /auth/change-password`** (`RequireAuthorization`), body
  `{ currentPassword, newPassword }`:
  - Target user identity comes from the **token**, read via
    `ClaimTypes.NameIdentifier` (JWT `sub` is remapped by the default inbound
    claim mapping — this is why `/auth/me`'s `FindFirst("sub")` currently returns
    null; fix `/auth/me` to match). **Never** from the request body.
  - Verify `currentPassword` with `IPasswordHasher.Verify` against the loaded
    user — **not** via `StoreAuthService.AuthenticateAsync` (which updates
    LastLogin, issues a token, and runs legacy migration).
  - **One atomic UPDATE**: `PasswordHash` (BCrypt), `PasswordHashVersion = 2`,
    `MustChangePassword = false`, `LastModifiedAtUtc`. Extend the repository
    (`UpdatePasswordHashAsync` → an update that also clears the flag) so it is a
    single write, never two.
  - Returns a **fresh full token** with **no** `must_change` claim.
  - Policy: `newPassword` length ≥ 8, ≠ `currentPassword`, trimmed the same way
    the login path trims (`.Trim()`), whitespace-only rejected before the length
    check.
- New use case `ChangePasswordCommand` + `ChangePasswordCommandHandler`
  (general-purpose — it also powers the eventual "change my password" feature).

### 4. Client (WinForms)

- **`IUserLogInService.LogInAsync`** return type `Task<bool>` →
  `Task<LogInResult>` (`Success`, `MustChangePassword`). The only production
  caller is `UserLogInPanel.TryLogInAsync`; the compiler finds the rest.
- **`ChangePasswordAsync(current, new)`** is added to **`IUserLogInService`**
  (which delegates to `IStoreHubClient` internally). The UI never references
  `IStoreHubClient` directly.
- **Deferred session establishment.** On a login where `MustChangePassword` is
  true, `StoreHubUserLogInService` sets the (restricted) token on the client but
  **does not** publish `UserLoggedInEvent` and does **not** sync products.
- **The gate lives in a testable coordinator**, not the `[ExcludeFromCodeCoverage]`
  panel. Flow:
  - must-change login → coordinator opens modal `ChangePasswordForm`
    (new + confirm; reuses the just-typed bootstrap as `currentPassword`).
  - Success → `ChangePasswordAsync` returns a fresh full token → set it →
    **now** publish `UserLoggedInEvent` + sync → proceed into the POS.
  - Cancel / close → `LogOut()` (clear token, publish `UserLoggedOutEvent`).
    Because nothing was published yet, this is a clean teardown, not a
    half-logged-in state.
  - Respects the existing re-entrancy guard (`LogInButton.Enabled` + shared
    `MessageForm`) so a second click/Enter cannot open a second dialog.

---

## Data flow (happy path, fresh install)

```
Installer wizard (Store-ID only)
  -> InstallationConfig.AdminPassword = <random, generated once>
  -> DatabaseSetup writes appsettings.json (InitialAdmin plaintext, ACL-locked)
  -> StoreHub.exe migrate
        -> EF MigrateAsync (adds must_change_password default false)
        -> InitialAdminSeeder: seed admin, MustChangePassword=true  [seeded=true]
  -> bootstrapper: surgical-remove InitialAdmin node, atomic write, re-ACL
  -> Finish screen: show admin / <random> once + summary file
  -> service starts

First login (WinForms)
  -> POST /auth/login (admin / <random>)  => 200, token has must_change claim,
                                              LoginResponse.MustChangePassword=true
  -> client sets token, DEFERS event/sync
  -> ChangePasswordForm (new + confirm)
  -> POST /auth/change-password {current:<random>, new:<chosen>}
        -> verify current, atomic UPDATE (hash+v2+flag=false+ts), fresh token (no claim)
  -> client swaps token, publishes UserLoggedInEvent + syncs products
  -> POS ready

Later logins => normal (flag false, no claim, no gate)
```

## Error handling / edge cases

| Scenario | Behaviour |
|----------|-----------|
| Re-install over existing DB (admin exists) | Seeder skips; finish screen shows "existing admin retained", **not** a bootstrap credential. |
| `migrate` seeds but appsettings-clear fails | Logged as a warning; install not aborted (DB already provisioned). Re-run is idempotent. Plaintext-gone is a verified post-condition. |
| Lost finish-screen credential | `StoreHub.exe reset-admin` regenerates + re-arms must-change. |
| must-change session, dialog cancelled | Client logs out; server-side `must_change` gate means the token could call nothing but change-password anyway. |
| new password == current / < 8 / whitespace-only | Rejected by the handler before the update. |
| Two users (admin must-change, cashier not) | Only the admin's token carries the claim; cashier logs in normally, no dialog. |
| EF migration on 3 populated stores | Ships `defaultValue: false`; existing rows get `false`, only freshly-seeded admin gets `true`. |
| Thai locale (`th-TH`) | N/A now that the password is random (no date formatting). Any date rendering elsewhere uses `CultureInfo.InvariantCulture`. |

## Testing strategy

**Unit**
- `InitialAdminSeeder`: sets `MustChangePassword=true`; reports seeded vs skipped
  (existing-admin path).
- `ChangePasswordCommandHandler`: happy path clears flag + rehashes; wrong
  current → rejected, flag unchanged; new < 8 → rejected; new == current →
  rejected; whitespace/trim handling.
- `InstallationConfig`: generates a random `AdminPassword`, single-evaluation
  (same value read twice).
- Surgical appsettings edit: `InitialAdmin` absent afterward; `storehub-db` +
  `LocalToken:SecretKey` **byte-identical** before/after; JSON still valid; ACL
  re-applied; simulated write failure surfaces (not swallowed).
- `LoginResponse` / `AuthResult` carries the flag; `LogInResult` surfaces it;
  `StoreHubHttpClient` deserializes `mustChangePassword` from the body.

**Integration / HTTP**
- `/auth/change-password`: unauthenticated → 401; wrong current → 401/400, DB
  flag unchanged; `new` length 7 → reject, 8 → accept (boundary); success
  **persists** flag=false (re-query DB) and returns a token with no `must_change`
  claim.
- **Server-side gate:** a `must_change` token → 403 on `/products`,
  `/sales/complete`, etc.; 200 only on `/auth/change-password`.
- **Migration:** apply the new migration to a DB pre-seeded with N users →
  succeeds, all existing rows `false`.

**VM clean-install validation (the real gate)**
- Fresh install → finish screen shows `admin` / `<random>` + summary file.
- Login forces the change dialog; rotation succeeds; new password logs in; POS
  reachable.
- **Causation-isolating single-use matrix** (proves rotation, not file-clearing,
  is what revokes the bootstrap):
  - (a) rotate but leave appsettings untouched → old bootstrap login → **401**.
  - (b) clear appsettings but do **not** rotate → old bootstrap login → **still
    200** (documents the real behaviour).
- Re-install over the existing DB → finish screen shows "existing admin retained".
- `reset-admin` round-trip: run it → new credential logs in → forces change.
- In-guest verification: `SELECT username, must_change_password FROM store_user;`
  and confirm `InitialAdmin` is gone from `appsettings.json` while the DPAPI
  secrets remain.

**Observability**
- Log (no secrets): "seeded admin, must-change=true"; a must-change login
  occurred for `admin`; successful rotation cleared the flag. Document the
  recovery path (`reset-admin`) for a dismissed finish screen.

## Follow-ups (out of scope)

- Login rate-limiting / failed-attempt lockout.
- Outbound CloudApi sync must not reset `MustChangePassword` (add a guard when
  sync lands).
- Full user-management UI (add/disable users, admin-initiated resets from the app).
- Reuse the `/auth/change-password` endpoint for a self-service "change my
  password" feature.

## Impacted files (indicative)

- `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs` (Store-ID-only)
- `installer/IndyPOS.Bootstrapper/Installers/InstallationConfig.cs` (random gen, drop `required`)
- `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs` (surgical clear, seeded report)
- `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs` / `StoreHubInstaller.cs` (clear step)
- `src/IndyPOS.Domain/Entities/Core/StoreUser.cs` (+ EF config + migration)
- `src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/InitialAdminSeeder.cs`
- `src/IndyPOS.Infrastructure/Services/StoreHub/StoreAuthService.cs` (flag through `AuthResult`)
- `src/IndyPOS.Application/UseCases/StoreHub/Auth/` (LoginResponse, ChangePassword use case)
- `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs` + repository
- `src/IndyPOS.Application/Common/Interfaces/IUserLogInService.cs` (+ `LogInResult`, `ChangePasswordAsync`)
- `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubUserLogInService.cs` (deferred session)
- `src/IndyPOS.Windows.Forms/UI/Login/` (coordinator + `ChangePasswordForm`)
- `src/IndyPOS.StoreHub/Program.cs` (`must_change` gate, change-password endpoint, `reset-admin` CLI, `/auth/me` claim fix)
