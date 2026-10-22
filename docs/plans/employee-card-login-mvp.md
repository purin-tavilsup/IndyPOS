# Employee Card Login MVP — IndyPOS

Status: **Implementation planning / proposed** (2026-10-22). This is a focused slice of [Operator Access & Manager Approval](operator-access-implementation-plan.md). No code changes or hardware purchases are made by this plan.

## Goal
Enable fast, reliable employee login, lock and operator switching at shared IndyPOS tills using an inexpensive, locally available **HIP 13.56 MHz MIFARE 1K** card, while preserving an **offline-first** experience and a username/password fallback. Do **not** block this MVP on DESFire EV3, mobile approval, or the independent Security Identity Platform.

## Scope and decisions
- **In scope:** reader integration; card enrollment, reassignment controls and revocation; UID-based *low-assurance* login; password fallback; operator session start/switch/lock; cashier attribution; basic audit; offline operation; reader failure handling; tests and pilot.
- **Out of scope:** manager PIN setup/verification; manager approval grants; DESFire cryptography; mobile credentials; remote approvals; attendance/clock-in/out; production hardware manufacturing.
- **Reader candidate:** ACS ACR1552U-M1 USB NFC reader using PC/SC; **compatibility must be proven** with actual HIP cards and reader firmware/drivers. The seller's 13.56 MHz claim alone is not proof.
- **Card candidate:** HIP MIFARE 1K, 13.56 MHz. Confirm exact chip, UID format, authenticity, supply and unit price from samples. The marketplace listing displayed THB 84 with an ambiguous 1-/5-card selection; **do not assume per-card price**.
- **Threat model:** UID is a public/clonable identifier, **not proof of secure card possession**. This temporary login is permitted only as a consciously accepted *low-assurance* cashier convenience method; role-based authorization and sensitive operations remain enforced server-side. Do not use UID alone for manager approval, privileged settings, refunds or high-risk operations.
- **Architecture:** POS desktop handles UI and card events; StoreHub authenticates locally and owns operator sessions, credentials, authorization and audit; PostgreSQL persists required state. CloudApi is never on the critical path.
- **Future migration:** keep a credential-type/assurance abstraction; DESFire or an independent identity SDK should be integrated later through an adapter without changing employee IDs, authorization policies or sales attribution.

## Proposed user journeys
### Enrollment
1. Authorized admin opens Employee > Credentials > Enroll Card.
2. Reader detects card, normalizes UID as **bytes** (display as canonical hex, preserving length and leading zeroes), records reader/card metadata if available.
3. StoreHub verifies card identifier is not already active for a different employee in the relevant credential namespace; do not silently reassign.
4. Admin confirms employee and card label; StoreHub creates credential and audit event.
5. Lost/replaced cards are revoked and replaced with a new credential; historical audit references remain intact.

### Login and switching
1. POS remains running. When no operator is active, require employee identification before attributed work.
2. Tap enrolled card. Reader adapter sends UID evidence to StoreHub over authenticated local API; StoreHub checks active credential, store scope, employee status and policy.
3. On success, establish terminal-scoped operator session and show employee identity prominently.
4. Another employee taps: preserve unfinished basket and require an explicit handoff/confirmation when needed; audit old and new operator. Do not rewrite prior line/action attribution.
5. Tapping same card again **must not silently log out** until UX behavior is explicitly chosen and tested; provide manual Lock/Switch.
6. Configurable inactivity lock does not close POS, discard basket or clock out of workforce attendance.
7. Password fallback uses existing StoreHub password verification, rate limiting and appropriate local credential availability.

### Failure cases
- Unknown/revoked card: no login; generic user-facing error; audit appropriately.
- Reader disconnected or driver unavailable: show degraded state, allow password login; no POS crash.
- WAN offline: all above continue via StoreHub/local DB.
- StoreHub unavailable: fail closed for login; do not grant a new operator session from an unverified client-side UID.
- Duplicate rapid tap: debounce locally and ensure idempotent session transitions server-side.
- Card cloning risk: disclose accepted risk, apply least privilege and monitoring; optionally require password/PIN for higher-risk employee contexts.

## Proposed technical boundaries (align with existing repo before coding)
- `ICardReader`: reader lifecycle, card-present/card-removed notifications, reader diagnostics; avoid UI-specific PC/SC calls.
- `ICredentialAuthenticator`: takes evidence and returns identity + assurance; implementation `MifareUidAuthenticator` is explicitly low-assurance.
- `EmployeeCredential`: employee FK, credential type, canonical identifier/lookup value, status, enrollment/revocation timestamps and actor, optional version; define store/org uniqueness based on existing tenant model.
- `OperatorSession`: terminal ID, employee ID, authenticated method/assurance, start, lock, end, last activity; do not equate this with JWT lifetime or attendance.
- `OperatorEvent` / audit: enrollment, revocation, login, failed login, switch, lock, fallback and takeover; no secrets or raw PIN/password in logs.
- **Important:** existing StoreHub JWT/BCrypt and authorization mechanisms should be inspected and reused where appropriate; do not invent a parallel identity stack.

## Implementation backlog
### M0 — Repository discovery and UX/security decisions
- [ ] Inspect current employee/user/role schema, login APIs, StoreHub auth/JWT, WinForms session handling, basket attribution and local outbox.
- [ ] Decide terminal identity, session persistence across app restart, inactivity timeout, same-card repeat-tap behavior and basket handoff confirmation.
- [ ] Define low-assurance policy (which cashier capabilities permitted), credential scope, multi-store uniqueness and offline revocation freshness.
- [ ] Confirm password fallback works fully offline and is adequately rate limited.

**Gate M0:** Existing-system integration design and accepted UID threat model reviewed.

### M1 — Hardware / .NET 10 PC/SC spike
- [ ] Obtain a few HIP MIFARE 1K sample cards and the reader; verify actual chip family and UID length.
- [ ] Prototype PC/SC enumeration, UID read (reader-supported pseudo-APDU only after manual verification), tap/removal, rapid taps, reconnect, multiple readers and startup without reader.
- [ ] Test Windows deployment, USB unplug/replug and behavior while WinForms lacks keyboard focus.
- [ ] Confirm read reliability and actual Thai sourcing; document driver versions, model, price and supplier.

**Gate M1:** Repeatable reads and recovery on target till hardware; no unsupported assumption about reader commands.

### M2 — StoreHub credential + session vertical slice
- [ ] Add migrations/entities only after comparing existing schema; enforce active credential uniqueness and employee/store scope.
- [ ] Implement privileged enrollment/revoke APIs, login-by-card endpoint and terminal-scoped operator session state.
- [ ] Implement server-side authorization, session concurrency/idempotency, audit and outbox as needed.
- [ ] Ensure no sensitive capability can be authorized by UID evidence alone.

**Gate M2:** Enrollment, card login and revocation work with WAN disconnected; unauthorized cards fail closed.

### M3 — POS UI and operator lifecycle
- [ ] Build reader service and card event routing independent of WinForms to support future Avalonia.
- [ ] Add enrollment UX, active operator indicator, login, switch, manual lock and password fallback.
- [ ] Handle unfinished baskets and actor attribution; avoid accidental login on duplicate tap.
- [ ] Inactivity lock and recovery with user-facing reader/error states.

**Gate M3:** Cashier can complete normal sales offline, switch operators and recover from reader failure without losing draft work.

### M4 — Testing, pilot and rollout
- [ ] Unit/integration tests for UID normalization, duplicate card, revoked card, disabled employee, wrong store, replayed event, repeated taps and concurrency.
- [ ] End-to-end offline test: WAN down, StoreHub + local DB up; enrollment, login, switch, lock, password fallback, sales attribution and sync after reconnect.
- [ ] Confirm no manager approval/privileged operation can proceed with UID alone.
- [ ] Pilot in one shop, then broaden to Rungrat, MimyMart, MimyShop and Esther's Day as appropriate; do not assume common employer or credential sharing.
- [ ] Collect tap success rate, time-to-login, operator-switch errors, fallback frequency and reader recovery failures.

**Gate M4:** Documented offline/security/UX test evidence and acceptable pilot metrics before wider rollout.

## Acceptance criteria
1. A valid enrolled HIP card can start a cashier operator session through StoreHub without WAN.
2. Revoked, unknown, cross-scope or disabled-employee credentials cannot start a session.
3. POS stays open; switching does not lose the basket or overwrite existing attribution.
4. Password fallback remains usable offline when reader is missing.
5. Repeated tap/disconnect/restart do not produce unauthorized session transitions.
6. Audit captures actor, credential method/assurance, terminal, session and relevant transitions.
7. UID-only credential cannot approve manager actions or bypass role/transaction authorization.
8. DESFire integration can be introduced later behind a credential adapter without changing employee identities.

## Open decisions for next planning session
- Should cashiers be allowed UID-only login in production, or require PIN for initial login/after inactivity?
- What is the precise operator session/terminal ownership model in current code?
- Same-card tap behavior and basket takeover UX?
- Default inactivity lock timeout?
- Card enrollment authority, cross-store sharing and credential recovery?
- How will credential revocation and offline staleness be handled across multiple StoreHubs?
- Which chip/UID behavior does the actual HIP card expose through ACR1552U-M1?

## Relationship to other work
This plan intentionally **does not implement** Manager PIN or approval yet. See [broader plan](operator-access-implementation-plan.md) for those phases and [hardware evaluation](../research/nfc-rfid-hardware-evaluation.md) for secure-card candidates. The independent Security Identity Platform is a future integration, not an MVP dependency.
