# Operator Access and Remote Manager Approval — Design Proposal

Status: **Proposed for review** (discussion captured 2026-10-21). No implementation is implied.

## Context and goals

IndyPOS tills are shared, **always-running** terminals. We need to attribute actions to the employee using the till without forcing full application sign-in on every sale. Keep the till usable offline: its StoreHub API and local PostgreSQL must remain the authority for local operations while CloudApi supports remote workflows. Current stack: WinForms (.NET 10), future Avalonia UI, StoreHub JWT/BCrypt/role capabilities, CloudApi and outbox synchronization.

Goals:
- Fast employee identification and operator switching.
- Correct attribution of sales, refunds, discounts, cash-drawer actions and approvals.
- Least-privilege authorization with no repeated authentication for normal permitted actions.
- Manager approval of **one specific restricted operation** from a **native iOS/Android app**, including when the manager is off-site.
- No escalation of the cashier's role or standing permissions.
- Preserve offline-first operation; define an explicit policy for unavailable remote approvals.
- Keep attendance/Clock In/Out separate from POS operator sessions, with an option to integrate a future workforce product.

## Confirmed product decisions

| Area | Decision |
| --- | --- |
| POS lifecycle | Application and local services remain running; only operator state changes |
| Primary operator sign-in/out | Employee NFC card / key fob via reader |
| Backup sign-in | Username and password |
| Operator model | Persistent operator; no extra taps for operations already permitted |
| Inactivity | Configurable auto-lock of operator (initial **proposal**: 5 minutes; validate in pilot) |
| Operator switching | Another employee presents their credential; clearly show active operator |
| Restricted operations | Evaluate policy by action, resource, store, operator and context |
| Manager approval | **Option A: per-operation approval**, single-use and narrowly scoped |
| Manager UI | **Native iOS and Android app** (not PWA) |
| Manager notifications | APNs/FCM push as an alert; actual decision is made through authenticated backend API |
| Attendance | Independent of operator session; auto-lock is never Clock Out |

### NFC security note

Do **not** treat the NFC card UID alone as strong authentication. Evaluate reader/card compatibility and cryptographic challenge-response credentials, plus PIN or equivalent assurance for sensitive actions. Provision, revoke and replace lost cards. Username/password fallback requires secure password hashing, rate limits and appropriate local offline handling.

## User journeys

### 1. Start / switch / lock operator
1. POS is open and ready; if no operator is active, operations requiring attribution are blocked.
2. Employee taps enrolled NFC card; StoreHub resolves the credential and establishes a local operator session.
3. Display employee name/role and operator status persistently.
4. Ordinary allowed sales proceed without more taps.
5. A different employee taps to switch operator; record an operator-switch event.
6. Tapping the current employee's card again may explicitly lock/sign out the operator (confirm gesture and accidental-tap behavior in pilot).
7. Inactivity locks the operator without closing POS, deleting a basket or clocking out. Background sync must not reset inactivity. Provide a prominent manual Lock/Switch action.
8. Password fallback is available when a card is unavailable.

### 2. Restricted operation / remote approval
1. Cashier attempts an action such as a refund beyond their limit.
2. StoreHub evaluates authorization and creates a pending approval request with immutable operation parameters (store, terminal, cashier, transaction, amount, currency, reason, expiry, request ID).
3. If connectivity exists, the request is delivered to CloudApi; CloudApi determines eligible managers and sends APNs/FCM notifications.
4. Manager opens the native app, authenticates, reviews full details and explicitly approves or rejects; optionally step-up with platform biometric/passkey according to risk.
5. CloudApi validates manager role, store scope, request status, expiry and the original operation details. The decision reaches StoreHub through a reliable authenticated channel (SignalR may improve latency; polling/reconciliation is required as fallback).
6. StoreHub verifies a **one-time, transaction-bound grant** before executing the exact operation. A grant cannot authorize a different refund, amount, terminal, operator or action.
7. Consume the grant atomically with operation execution (or use a durable idempotent execution state machine where a database transaction cannot span boundaries). Record audit events for request, decision and execution.
8. Current operator remains the cashier; manager approval never switches operator or grants a temporary manager role.

Example: cashier requests refund THB 2,500; manager approves **this refund only**. Changing the amount or transaction invalidates the approval.

### 3. Offline / failures
- Ordinary authorized sales continue through StoreHub and local PostgreSQL when CloudApi is unreachable.
- Remote approval cannot be assumed successful while offline; default to **deny/defer restricted action** or an explicitly configured **local manager approval** path using a strong credential.
- Never bypass authorization simply because a push or SignalR connection failed.
- Push delivery is best-effort: native app also lists pending approvals and refreshes via API.
- Requests expire (initial proposal: 2 minutes); late or repeated responses cannot execute.
- Multiple managers: first valid decision wins using concurrency-safe state transitions.
- On operator switch, changed request details, cancellation or expiry, invalidate pending grants.
- Preserve draft basket on auto-lock; require re-identification before continuing sensitive actions.
- Define a clear recovery path for StoreHub restart, CloudApi reconnect and duplicate messages.

## Architecture fit

```text
NFC reader / password
         |
WinForms POS (future Avalonia) -- local operator UI / permission prompts
         |
StoreHub API (.NET) -- local operator sessions, credential checks,
         |             authorization, operation execution, local audit
Local PostgreSQL
         |
    sync / secure API (when online)
         |
CloudApi -- approval requests, manager/store authorization,
         |    notification dispatch, decision delivery
         +---- APNs / FCM ----> Native Manager App (iOS / Android)
         |
         +---- authenticated status / decision ----> StoreHub
```

Do not make every ordinary POS transaction depend on CloudApi. Existing StoreHub authentication and role capabilities should be extended rather than replaced blindly. Treat the Manager App framework (e.g. .NET MAUI) as **under evaluation**, not a decided technology. Prototype real-device push, authentication and lifecycle behavior before selecting it.

### Suggested conceptual components

- `OperatorSessionManager`: terminal-specific operator state, inactivity, switching, lock.
- `EmployeeCredentialService`: enrollment, NFC verification, revocation, password fallback.
- `AuthorizationPolicyService`: local policy evaluation for operation/resource/context.
- `ApprovalRequestService`: immutable requests, expiry, status transitions, idempotency.
- `ApprovalDecisionService`: server-side manager eligibility, authenticated decision, concurrency.
- `AuthorizationGrantService`: tightly scoped, single-use, expiring execution authorization.
- `NotificationDispatcher`: APNs/FCM dispatch; never a source of authorization truth.
- `AuditTrail`: actor, approver, terminal, action, resource, timestamps, before/after where relevant.

Conceptual records: `Employee`, `EmployeeCredential`, `OperatorSession`, `OperatorEvent`, `ApprovalRequest`, `ApprovalDecision`, `AuthorizationGrant`, `AuditEvent`. Align names and keys with existing entities before implementation.

## Attribution and audit

Track **CreatedByEmployeeId**, **CompletedByEmployeeId** and per-action operator/approver events rather than rewriting historical ownership when someone switches mid-transaction. For an in-progress basket, require a clear takeover/continue flow and preserve previous attribution. Audit sensitive actions, denied attempts, credential changes and approval decisions. Do not equate a card tap with proof of physical presence for payroll.

## Security and multi-store rules

- Scope credentials, sessions and manager approvals to the appropriate store/organization. Cross-store managers only approve stores explicitly assigned to them.
- Keep authorization enforcement in StoreHub/backend; UI button visibility is not a security boundary.
- Do not send reusable authorization secrets in push payloads. Push only references a pending request.
- Authenticate the native app using standards-based flows (OIDC + PKCE or passkeys where appropriate), secure OS credential storage and revocable device sessions.
- Protect approval requests against replay, tampering, duplicate delivery and time-of-check/time-of-use races.
- Log who requested, who approved and who actually executed. Ensure an approved operation is still valid at execution.
- Require appropriate manager re-authentication for high-risk decisions.
- Define offline credential cache expiry, revocation freshness and local manager override policy explicitly.

## Proposed delivery plan

### Phase 0 — Discovery / spike
- Inspect existing StoreHub identity, JWT, BCrypt, role capabilities and transaction attribution.
- Evaluate PC/SC readers, secure NFC cards, device drivers and enrollment workflow on actual tills.
- Test UX: tap, switch, lock, password fallback, cashier-to-manager escalation.
- Validate native mobile stack on iOS/Android for APNs/FCM, deep links and biometric step-up.
- Decide store/employee ownership and integration boundaries with future workforce attendance.

### Phase 1 — Operator access
- Local credential enrollment/revocation and password fallback.
- Operator sessions, NFC switch, inactivity auto-lock, manual lock and UI indicator.
- Enforce permission policies in StoreHub and add reliable actor attribution.
- Tests for session transitions, offline behavior, card loss, basket takeover and privilege checks.

### Phase 2 — Local approval foundation
- Request/decision/grant domain model and audit events.
- Local manager approval path with stronger authentication.
- One-time grant consumption and idempotency tests.
- Explicit fail-closed behavior for restricted actions.

### Phase 3 — Remote native approval
- Cloud approval routing, APNs/FCM, native Manager App approval inbox/details.
- Authenticated approve/reject endpoints, expiry, multiple-manager concurrency.
- SignalR or equivalent live updates plus polling/reconciliation fallback.
- Integration and security tests across disconnect/reconnect, duplicate messages, cancellation and changed amounts.

### Phase 4 — Workforce integration (separate scope)
- Reuse employee identity where appropriate; define Clock In/Out explicitly.
- Attendance policy must not infer clock-out from POS auto-lock or switching.

## Open questions for implementation review

1. Which NFC card/reader model supports robust cryptographic authentication and affordable deployment?
2. Should a second tap by the same employee lock immediately, or require a visible confirmation?
3. What inactivity timeout and in-progress transaction behavior work best per store?
4. Which operations require manager approval, and what limits differ by store/role?
5. Which manager(s) receive requests and what escalation happens when nobody responds?
6. What local offline approval assurance is acceptable, and can approvals be deferred?
7. Should cashier operations wait synchronously for approval or save as pending drafts?
8. Which mobile framework is best after real-device evaluation?
9. How should existing user/role entities and StoreHub auth be evolved without breaking legacy flows?

## Acceptance criteria (initial)

- POS stays open across operator sign-in/out/lock.
- An identified cashier performs ordinary permitted actions without repeated NFC taps.
- Inactivity lock never clocks an employee out or destroys a draft.
- A cashier cannot execute a restricted operation without an applicable permission or valid specific grant.
- A manager can approve/reject from native iOS/Android; decision is audited.
- An approval cannot be replayed, reused, redirected to another operation or treated as role elevation.
- Offline ordinary sales remain usable; remote approval outages do not silently bypass controls.
- Every sensitive transaction can identify the cashier, approving manager (if any) and execution event.
