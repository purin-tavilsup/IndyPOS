# One client registry for store-to-cloud authentication (defect I0-E)

**Date:** 2026-08-20
**Status:** Approved (Pond, 2026-08-20)
**Epic:** I (Cloud infrastructure) — closes defect I0-E, a prerequisite for I4 (SyncWorker against
the real CloudApi)
**Branch:** `spec/cloud-store-client-registry`, stacked on `fix/cloudapi-tls-terminated-upstream`
(PR #89), because the OpenIddict tables only exist from PR #87 onward

---

## Why now

Store-to-cloud authentication does not work. A store that registers successfully still cannot obtain
a token, so nothing downstream of authentication — event sync, master-data pull, bulk migration — can
run against a real CloudApi. Task I4 is blocked on it, and I0 shipped a container whose primary
purpose it cannot yet serve.

The cause is two client registries where there should be one.

---

## What exists today

**Registration** (`RegisterStoreHandler`) writes a single `CloudStoreConfig` row:

- `ClientId` = `$"store_{command.StoreId}"`
- a 32-byte `RandomNumberGenerator` secret, BCrypt-hashed into `ClientSecretHash`
- the plaintext secret returned once, in `RegisterStoreResponse`

**Token issuance** (`TokenController.Exchange`) then does its own complete validation: look up
`StoreConfigs` by `ClientId`, reject when missing **or `!IsActive`**, `BCrypt.Verify` the secret,
stamp `LastAuthenticatedAtUtc`, build an identity with `Subject`/`Name`/`store_id` claims, and
`SignIn`.

Taken alone that is a coherent, self-contained credential system. But `AddOpenIddictServer` also
registers OpenIddict's own EF stores against the same context
(`AddCore().UseEntityFrameworkCore().UseDbContext<CloudDbContext>()`), which gives OpenIddict an
`OpenIddictApplications` table it treats as authoritative — and **nothing in `src/` ever writes a row
there**. OpenIddict's pipeline validates `client_id` against that empty table before passthrough ever
reaches the controller, so every request dies with `401 invalid_client`.

**Two clients depend on this working**, not one: `CloudTokenService` (StoreHub's sync path) and
`SqliteMigrationService` (the migration tool's bulk upload). Both POST `client_credentials` to
`/oauth/token`.

**There are no tests.** Nothing under `tests/` references `RegisterStore`, `CloudStoreConfig`, or
`TokenController`. The sibling cloud handlers (`IngestEventsCommandHandler`,
`CreateCloudUserCommandHandler`) *are* tested, because they live in `IndyPOS.Application` behind
repository abstractions. `RegisterStoreHandler` and `TokenController` live in CloudApi and depend on
`CloudDbContext` directly, which is why they are not.

---

## The measured constraint

The obvious alternative — keep `StoreConfigs` as the only registry and teach OpenIddict to consult it
— was probed rather than argued about, because an earlier revision of the I0 spec got this exact area
wrong by reasoning.

With the EF stores left active (**not** degraded mode), all six handlers that consult the application
store were removed:

```csharp
options.RemoveEventHandler(OpenIddictServerHandlers.ValidateClientId.Descriptor);
options.RemoveEventHandler(OpenIddictServerHandlers.ValidateClientType.Descriptor);
options.RemoveEventHandler(OpenIddictServerHandlers.ValidateClientSecret.Descriptor);
options.RemoveEventHandler(OpenIddictServerHandlers.Exchange.ValidateEndpointPermissions.Descriptor);
options.RemoveEventHandler(OpenIddictServerHandlers.Exchange.ValidateGrantTypePermissions.Descriptor);
options.RemoveEventHandler(OpenIddictServerHandlers.Exchange.ValidateScopePermissions.Descriptor);
```

That worked as far as it went: the request reached `TokenController`, which validated against
`StoreConfigs` and called `SignIn`. Then OpenIddict threw from deeper in the pipeline:

```
System.InvalidOperationException: The application entry cannot be found in the database.
   at OpenIddict.Server.OpenIddictServerHandlers.PrepareAccessTokenPrincipal.HandleAsync(ProcessSignInContext context)
```

**Token generation requires the application row, not merely validation.** Persistence links each
issued token to an application, so the choice is not stylistic:

| Requirement | What it forces |
|---|---|
| Persisted, revocable tokens | An `OpenIddictApplications` row per store — mandatory |
| `StoreConfigs` as the only registry | Degraded mode, no stores, therefore no persistence and no revocation |

Epic I needs a deregistered store to actually lose access, so revocability decides it. The
application row is non-negotiable — which makes a *second* hashed secret in `StoreConfigs` pure
duplication.

---

## Design

**OpenIddict's application store owns the client secret. `CloudStoreConfig` owns the store.**

### Schema

`ClientSecretHash` is removed from `CloudStoreConfig`. `ClientId` **stays**: it is a public
identifier rather than a secret, it is the join key from a token request back to store metadata, and
its unique index already exists.

Removal is a new migration, not an edit of the initial one. The repo's forward-only gate
(`docs/operations/upgrade-procedure.md`) forbids drops because an upgrade rolls back binaries but not
schema — restored binaries must still be able to INSERT. **That gate does not bind here:** CloudApi
has never been released, so there are no previous binaries to roll back to. This is stated explicitly
so the migration does not read as a violation to a future reviewer.

### Components

One new abstraction, in `IndyPOS.Application` beside the other cloud abstractions:

```csharp
public interface IStoreClientCredentialStore
{
    Task CreateAsync(string clientId, string clientSecret, string displayName, CancellationToken cancellationToken = default);
    Task RevokeAsync(string clientId, CancellationToken cancellationToken = default);
}
```

Implemented in CloudApi over `IOpenIddictApplicationManager`. `CreateAsync` builds an
`OpenIddictApplicationDescriptor` with:

- `ClientId`, `ClientSecret` (plaintext in, hashed by the manager), `DisplayName`
- `ClientType` = confidential
- `Permissions.Endpoints.Token`
- `Permissions.GrantTypes.ClientCredentials`
- `Permissions.Prefixes.Scope + "sync.write"` and `+ "master.read"`

Those permission entries are load-bearing: without them the `ValidateEndpointPermissions`,
`ValidateGrantTypePermissions` and `ValidateScopePermissions` handlers reject the request even though
the client exists.

The abstraction exists so registration is testable without a live OpenIddict, and so the handler
follows the same shape as its tested siblings rather than reaching for infrastructure directly.
`RevokeAsync` is included because it is the capability this whole redesign is chosen *for*; it is
wired but not yet called (see Out of scope).

### Registration flow

`RegisterStoreHandler` writes the `CloudStoreConfig` row and creates the OpenIddict application
**inside one transaction**. A store with no credentials, and credentials with no store, are both
broken states that a partial failure would leave behind. Same all-or-nothing posture as
`MigrateAllAsync`.

The generated secret, the `ClientId` convention and the return contract are unchanged, so
`RegisterStoreResponse` and the admin endpoint keep their shape. `GenerateClientSecret` stays;
the BCrypt hashing goes.

### Token issuance

`TokenController.Exchange` loses its secret verification — OpenIddict has already validated
`client_id` and `client_secret` before passthrough hands over. What it keeps:

- the `StoreConfigs` lookup by `ClientId`, for `StoreName` and `store_id`
- **the `IsActive` check.** ⚠️ This is the trap in this change: `IsActive` currently rides in the
  same condition as the not-found check, immediately above the secret verify. OpenIddict knows
  nothing about `IsActive`, so if that check is deleted alongside the BCrypt call, a deactivated
  store keeps getting tokens. The check must survive, and a test must pin it.
- the `LastAuthenticatedAtUtc` stamp and the claims/`SignIn` block, unchanged

---

## Testing

**New project: `tests/IndyPOS.CloudApi.Tests`.** CloudApi has no tests at all today and Epic I keeps
adding to it, so the project pays for itself beyond this change. xUnit + FluentAssertions, matching
the rest of the repo (not MSTest; `[DataRow]` does not exist here).

Registration, against EF InMemory plus a fake `IStoreClientCredentialStore`:

- writes the `CloudStoreConfig` row **and** creates the client, with the expected `ClientId`
- requests exactly the three permission groups above
- returns the plaintext secret once, and never persists it
- rejects a duplicate `StoreId` as it does today

Token issuance, same harness:

- an inactive store is rejected — the `IsActive` regression guard
- an unknown `ClientId` is rejected

⚠️ **EF InMemory ignores transactions.** It will happily report success for a partial write, so the
all-or-nothing guarantee **cannot** be demonstrated with it. That claim gets verified by running
against a real PostgreSQL, and the limitation is recorded here so a future reader does not mistake a
green InMemory test for proof.

**Verified by running**, the standard this epic has used throughout: register a store through
`POST /admin/stores/register` against a real PostgreSQL, then exchange the returned credentials at
`/oauth/token` for a real token, then confirm `INSERT INTO "OpenIddictTokens"` happened. That end-to-end
pass is what finally closes I0-E, and it is the first time store-to-cloud auth will have worked.

---

## Out of scope

- **Calling `RevokeAsync`.** Deactivating a store should revoke its client, and the capability lands
  here, but the deactivation flow itself belongs with I4's sync work. `IsActive` continues to gate
  token issuance in the meantime, so a deactivated store cannot authenticate either way.
- **Rotating an existing store's secret.** No store has working credentials yet, so there is nothing
  to rotate.
- **The TLS/ID2083 transport question** — settled separately in PR #89.
- **`OpenIddict.AspNetCore` / `OpenIddict.EntityFrameworkCore` floating on `6.*`.** Both hosts pin a
  floating major range, and the local package cache already holds 6.4.0 *and* 7.2.0 — whose handler
  layout differs (`ValidateClientId` is a top-level nested type in 6.4.0). A restore can therefore
  move this code onto a different API shape silently. Worth pinning, in its own PR.

---

## Rejected alternatives

**Mirror the credentials into both stores (option A).** Smallest diff: keep `ClientSecretHash` and
also create an OpenIddict application. Rejected because it makes two hashed copies of one secret
permanent — rotation must touch both, and they can drift. The spec for I0 already warns that
translation layers are where the payment-mapping class of bug breeds.

**Keep `StoreConfigs` authoritative via custom handlers (option B).** Rejected on measurement, not
taste: removing the six store-consulting validation handlers still fails in
`PrepareAccessTokenPrincipal`, because token generation needs the application row. Making it work
requires degraded mode, which forfeits persistence and therefore the revocation this design is chosen
for.

**Amending PR #87's initial migration so the column never exists.** Tidier history, and legitimate
since nothing shipped — but it means force-pushing an already-reviewed PR and re-reviewing it. Not
worth the disruption for a column that a follow-up migration removes just as completely.

**Keeping the column and leaving it unwritten.** Zero risk now, but it leaves a secret-shaped column
in a security-sensitive table, which is exactly the sort of thing that gets quietly repopulated later.
