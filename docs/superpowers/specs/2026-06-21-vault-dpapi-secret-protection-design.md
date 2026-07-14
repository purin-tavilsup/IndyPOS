# Design: IndyPOS.Vault — DPAPI Secret Protection

**Date:** 2026-06-21
**Status:** Draft (pending final review)
**Author:** Pond + Claude (pair)
**Reviewed by:** Architect / Software Engineer / QA sub-agents (2026-06-21)

## Problem

The v4 installer auto-generates two machine-to-machine secrets and writes them
**plaintext** into `C:\ProgramData\IndyPOS\v4\StoreHub\appsettings.json`:

1. The PostgreSQL app-user password (inside `ConnectionStrings:storehub-db`)
2. The JWT signing key (`LocalToken:SecretKey`)

The JWT key is *also* written plaintext to `C:\ProgramData\IndyPOS\v4\keys\storehub.key`
(ACL-locked, but plaintext). Plaintext-at-rest means anyone who can read those
files — or grabs a disk image, a backup, or copies the folder — gets the
secrets. We want at-rest protection so the files alone are useless off the
machine they were created on.

### Framing

The DB password is a **machine-to-machine credential**: Postgres listens only on
`127.0.0.1`, and the only thing that ever reads the password is StoreHub at
startup. No human needs to know it. The same is true of the JWT key. So the
right model is: **the app reads these secrets automatically; a human never
does.** Rare human DB access is a *separate* break-glass concern (out of scope —
see Follow-ups).

## Goals

- Encrypt the two secrets at rest in `appsettings.json` using Windows DPAPI.
- StoreHub transparently decrypts them at startup before they are consumed.
- Dev / Aspire runs are completely unaffected (no markers → no-op).
- A single source of truth for the encryption contract (marker + scope +
  entropy), shared by the installer (encrypts) and StoreHub (decrypts).
- Eliminate the redundant plaintext `storehub.key` file (Decision 2a).

## Non-Goals

- Break-glass operator DB access (separate spec).
- Cross-machine secret portability (DPAPI machine scope is intentionally
  machine-bound — that bind is the feature).
- A general secret *store* — this is stateless seal/unseal only.
- Consolidating the existing `DpapiSecretStorage` (different scope/lifecycle;
  see "Relationship to existing prior art").

## Threat Model (honest scope)

DPAPI **machine scope** protects against **off-box** exposure: stolen disk
images, backups, folder copies, or a file leaked to another machine — none can
be decrypted elsewhere.

It does **not** protect against a local attacker who already has admin/code
execution on the box: machine scope means any process on that machine can
unprotect. That residual risk is mitigated by NTFS ACLs (Administrators +
LocalSystem only). DPAPI + ACLs together are defense in depth, and that
combination is the bar we target.

**ACL parity note:** today only the `storehub.key` file is ACL-locked;
`appsettings.json` is not. Since Decision 2a moves the JWT secret *into*
`appsettings.json`, the installer must apply the same Administrators+LocalSystem
ACL to `appsettings.json` — otherwise dropping the locked key file would weaken
the at-rest posture. The existing `RestrictFilePermissions` helper is repurposed
for this.

## Relationship to existing prior art

`IndyPOS.Infrastructure/Services/Security/DpapiSecretStorage.cs` already wraps
DPAPI, but is **not** reusable here and is intentionally left untouched:

| | `DpapiSecretStorage` (existing) | `SecretProtector` (new, Vault) |
|---|---|---|
| Scope | `CurrentUser` | `LocalMachine` (installer ≠ service account) |
| Storage | file-per-secret store | stateless string seal/unseal |
| On failure | swallow → return `null` | **fail fast** (throw) |
| Layer | Infrastructure (heavy) | zero-dep leaf (installer-safe) |

We **do** reuse its entropy convention for consistency:
`SHA256("IndyPOS:" + key)`.

## Architecture

### New project: `IndyPOS.Vault`

A **strict zero-dependency leaf** assembly. Only dependency beyond the BCL is the
`System.Security.Cryptography.ProtectedData` NuGet package (a thin managed
wrapper over the always-present `crypt32.dll` — no extra native asset to bundle,
verified safe under the installer's `PublishSingleFile`+`SelfContained` win-x64
publish). Target `net10.0-windows` (DPAPI is Windows-only). It must never
reference Application/Infrastructure/Domain. An architecture test enforces this.

```
IndyPOS.Vault/
  SecretProtector.cs    // static: Protect(key, plaintext) / Unprotect(key, value) / IsProtected(value)
```

#### Contract

- **Scope:** `DataProtectionScope.LocalMachine` — the installer (running as
  admin) encrypts; the StoreHub service (running as LocalSystem) decrypts.
  Different accounts, same machine → machine scope is the only scope that works.
- **Entropy:** per-key, `SHA256("IndyPOS:" + key)` (Decision 1). Binds each
  ciphertext to its config key, so a DB-password blob cannot be swapped into the
  JWT field (or vice versa) and still decrypt.
- **Marker:** protected values are written as `DPAPI:<base64-ciphertext>`.
  `IsProtected` is an **ordinal, case-sensitive** `StartsWith("DPAPI:")`.
  Unmarked values are returned untouched — making the dev path a true no-op.
- **Encoding:** plaintext ↔ bytes is **always `Encoding.UTF8`**, never
  `Encoding.Default` (locale-dependent).
- **Entropy/scope are part of the contract:** both sides must agree; changing
  either silently breaks decryption.

```csharp
public static class SecretProtector
{
    private const string Marker = "DPAPI:";

    // Throws if value is already protected (no double-wrap).
    public static string Protect(string key, string plaintext);   // -> "DPAPI:<base64>"

    // Unmarked -> returned as-is. Marked -> decrypt or throw (see Error Handling).
    public static string Unprotect(string key, string value);

    public static bool IsProtected(string value);                 // ordinal StartsWith(Marker)
}
```

### Installer side (encrypt)

`DatabaseSetup.CreateStoreHubConfigAsync` builds the connection string and JWT
secret. Change: wrap both with `SecretProtector.Protect(key, ...)` (passing the
exact config key as entropy input) before serializing, so the file on disk
contains `DPAPI:...` for both values, plaintext for everything else.

Encryption runs **on the target machine** (the installer runs there), which is
exactly where DPAPI machine scope needs it to happen.

**Decision 2a — drop `storehub.key`:** `GenerateJwtSecretAsync` no longer writes
a separate plaintext key file. The JWT secret is generated, protected, and
written only into `appsettings.json`. Re-runs regenerate a fresh secret (12h
token expiry makes this acceptable). Touch-points to update:
`scripts/verify-install.ps1` and `scripts/install-config.ps1` (which currently
reference `storehub.key`), plus `docs/operations/store-installation-guide.md`.

### StoreHub side (decrypt)

The two secrets are consumed very early in `Program.cs`:
- `ConnectionStrings:storehub-db` → `builder.AddNpgsqlDbContext("storehub-db")`
  (resolves via `GetConnectionString`, line ~52)
- `LocalToken:SecretKey` → bound via `GetSection(...).Get<LocalTokenOptions>()`
  (line ~98)

Add a `ConfigurationManager` extension invoked **immediately after
`CreateBuilder`**, before those consumers:

```csharp
builder.Configuration.UnprotectSecrets(
    "ConnectionStrings:storehub-db",
    "LocalToken:SecretKey");
```

`UnprotectSecrets`:
1. For each named key, reads the current value.
2. If `IsProtected`, decrypts (using the key as entropy input) and stages the
   plaintext into a dictionary.
3. Appends one `AddInMemoryCollection(dict)` source. With `ConfigurationManager`
   the appended source is evaluated **last**, so it wins over the JSON file for
   those keys. (It still loses to environment variables / command-line — which
   is correct: dev/Aspire inject an *unmarked* env var, so no in-memory entry is
   ever staged for it and the no-op holds.)
4. Absent or unmarked keys stage nothing.

**Home: StoreHub, not ServiceDefaults.** ServiceDefaults targets `net10.0`
(platform-neutral, shared with CloudApi); referencing the Windows-only Vault
would force a `net10.0-windows` TFM onto it. StoreHub is already
`net10.0-windows`, so it is the correct host. `StoreHub → Vault` is a leafward
reference (no layering violation).

We deliberately do **not** build a custom `IConfigurationProvider` — two known
keys make that over-engineering (YAGNI).

## Data Flow

```
INSTALL (admin):
  generate secret -> SecretProtector.Protect(key, secret) -> "DPAPI:..." -> appsettings.json

STARTUP (LocalSystem service):
  read appsettings.json -> UnprotectSecrets -> in-memory plaintext override (wins over JSON)
    -> GetConnectionString / Get<LocalTokenOptions> consume plaintext (unchanged)

DEV (Aspire):
  connection string injected as env var, unmarked -> UnprotectSecrets stages nothing -> no-op
```

## Error Handling

`UnprotectSecrets` owns the failure path (it knows the key name):

- **Decrypt fails** — `CryptographicException` (wrong machine, corrupt, tampered
  ciphertext, or wrong entropy) OR `FormatException` (`DPAPI:` marker followed by
  non-base64). `UnprotectSecrets` catches both, **logs the offending key name +
  probable-cause guidance** ("could not decrypt key X; this config was likely
  created on a different machine, or the value is corrupt"), and rethrows a
  wrapped exception that crashes startup. It must **never** fall back to the raw
  ciphertext and must **never** log the secret value.
- **Logging caveat:** this runs right after `CreateBuilder`, before the logging
  pipeline is fully built. The thrown message may surface only to stderr / the
  Windows Event Log, not the app's configured sinks. The message must therefore
  be self-contained and human-actionable. VM validation confirms where it lands.
- **Value unmarked:** returned untouched (intended dev path).
- **`Protect` on an already-marked value:** throws (no double-wrap). The
  installer only ever feeds freshly generated plaintext, so this is a guard, not
  a normal path.
- **Null/empty:** `Protect`/`Unprotect` guard null; an empty/whitespace config
  value stages nothing (no-op).

## Testing Strategy

Framework: **xUnit** (`[Fact]`/`[Theory]`+`[InlineData]`) to match the existing
solution. Naming: `Method_Condition_ShouldExpectedBehavior`. Windows-only via
`[SupportedOSPlatform("windows")]`; Windows CI is a hard assumption.

### `IndyPOS.Vault.Tests` (unit)
- `Protect_ThenUnprotect_ShouldRoundTripVariousPayloads` (`[InlineData]`:
  connection-string shape, 64-byte base64 key, unicode, empty, ~10KB)
- `Unprotect_WithUnmarkedValue_ShouldReturnInputUnchanged`
- `IsProtected_WithVariousValues_ShouldReturnExpected` (`DPAPI:`, `dpapi:`,
  ` DPAPI:`, plaintext-containing-substring → only exact ordinal prefix is true)
- `Protect_ShouldProduceMarkerPrefixedValue`
- `Protect_OnAlreadyProtectedValue_ShouldThrow`
- `Protect_WithNullOrEmptyInput_ShouldBehaveAsSpecified`
- `Unprotect_WithMarkerAndNonBase64Payload_ShouldThrowFormatException`
- `Unprotect_WithMarkerAndValidBase64ButNonDpapiBytes_ShouldThrowCryptographicException`
- `Unprotect_WithMarkerAndEmptyPayload_ShouldThrow`
- `Unprotect_WithForeignMachineCiphertext_ShouldThrowCryptographicException`
  (hard-coded foreign blob — exercises wrong-machine path without machine
  dependence)
- `Unprotect_WithWrongEntropyKey_ShouldThrow` (encrypt under key A, decrypt
  under key B → cross-field-swap protection)
- Architecture guard: `Vault_ShouldNotReferenceApplicationInfrastructureOrDomain`

### StoreHub config-extension tests
- `UnprotectSecrets_WithProtectedValue_ShouldExposePlaintextViaGetConnectionString`
- `UnprotectSecrets_WithProtectedValue_ShouldExposePlaintextViaLocalTokenOptionsBinding`
- `UnprotectSecrets_WithUnmarkedValue_ShouldLeaveConfigurationUnchanged`
- `UnprotectSecrets_WithMissingKey_ShouldNoOp`
- `UnprotectSecrets_WithEmptyValueForKey_ShouldNoOp`
- `UnprotectSecrets_WithOneKeyProtectedAndOnePlaintext_ShouldDecryptOnlyProtected`
- `UnprotectSecrets_WhenDecryptFails_ShouldThrowAtStartupNamingTheKey`
- `UnprotectSecrets_WhenDecryptFails_ShouldNotExposeRawCiphertext`
- `UnprotectSecrets_WhenDecryptFails_ShouldLogKeyAndProbableCause`

### Installer tests
- `CreateStoreHubConfig_ShouldProtectOnlyConnectionStringAndSecretKey`
- `CreateStoreHubConfig_ShouldLeaveNonSecretFieldsPlaintext`
- `GenerateJwtSecret_ShouldNotWritePlaintextKeyFile` (Decision 2a)

### Hyper-V end-to-end validation checklist
1. Clean-VM snapshot → run installer as admin → confirm `appsettings.json` shows
   `DPAPI:` on both secrets, plaintext elsewhere, and **no** `storehub.key`.
2. Start StoreHub service (LocalSystem) → `/health/ready` healthy → proves
   cross-account, same-machine machine-scope decrypt works.
3. Real POS login end-to-end (JWT signed with decrypted key + DB query) →
   validates *both* secrets.
4. **Negative:** copy `appsettings.json` to a second VM / reverted snapshot →
   StoreHub fails fast with a clear, logged, key-naming error and does not
   start. Capture where the error lands.
5. Re-run installer → fresh secrets, both marked, service restarts cleanly.
6. Dev/Aspire unaffected: `dotnet run` via AppHost with unmarked injected
   connection string → `UnprotectSecrets` no-op.

## Follow-ups (not in this spec)

- Break-glass operator DB access (decrypt-and-print / db-shell command).
- Postgres **superuser** password recovery story (installer doesn't persist it).
- Possible future consolidation of `DpapiSecretStorage` and `SecretProtector`
  if their lifecycles converge.
```
