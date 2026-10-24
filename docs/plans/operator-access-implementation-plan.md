# IndyPOS Operator Access & Manager Approval — Implementation Plan

Status: Proposed plan / POC first. No application implementation approved by this document.
Updated: 2026-10-21
Companion: [Architecture and decisions](../architecture/operator-access-and-manager-approval.md) · [Hardware research](../research/nfc-rfid-hardware-evaluation.md)

## Confirmed product behavior
- **Offline-first is non-negotiable.** POS + StoreHub + local PostgreSQL continue core workflows without WAN/CloudApi.
- Shared terminal stays running; employee operator session is distinct from application lifetime and from workforce clock-in/out.
- Card/NFC is primary operator identification/login experience; password fallback is required. Actual credential assurance depends on chosen card and policy.
- Persistent cashier session with configurable inactivity auto-lock; explicit operator switching and basket handoff, preserving distinct creator/completer attribution.
- Restricted actions (refund, void, discount, overrides) require **approve each operation**, not temporary manager role elevation.
- Manager card + PIN is an occasional **local approval**, not manager login/operator switching. Cashier stays current operator.
- Native iOS/Android Manager App supports remote approvals when online; no cloud dependency for local approval.
- StoreHub validates identity, role/policy, request binding, expiry, replay protection and atomic single-use grant consumption; audit locally and sync via outbox.

## Architecture boundaries
- Desktop UI: capture tap, prompt PIN, show request/decision, retain current operator, show offline state.
- Local StoreHub: session/credential lifecycle, authorization policies, approval state machine, credential revocation, PIN verification and immutable audit/outbox.
- PostgreSQL: durable local session/credential/approval/audit records as needed; design migrations.
- CloudApi: optional remote coordination, push and manager device enrollment; never authoritative for offline local grants.
- Manager App: review transaction details, authenticated approve/reject; potential local QR/LAN option pending POC.

## Phase 0 — Discovery and threat model
- [ ] Review existing StoreHub auth, roles/capabilities, WinForms input, persistence and outbox contracts.
- [ ] Model attacker scenarios: stolen/cloned card, observed PIN, keyboard injection, lost phone, compromised POS, replay and stale revocation.
- [ ] Define operations requiring approval, risk tiers, limits, PIN policy, expiry, lockout and break-glass/recovery.
- [ ] Define UX rules: tap context distinguishes operator switch vs approval; no accidental session change.
- [ ] Document pilot shop constraints and device/OS support.

**Gate 0:** Agreed threat model and minimum acceptable authentication assurance for each operation.

## Phase 1 — Focused experiments (POCs)
### POC A: USB reader + card compatibility
- [ ] Integrate ACS ACR1552U-M1 using PC/SC in a minimal .NET 10 test app.
- [ ] Test tap/removal, reconnect, multiple readers, background kiosk input, WinForms and prospective Avalonia.
- [ ] Read UID from MIFARE Classic 1K for identification UX only; test unsupported/malformed cards.
- [ ] Prove secure APDU mutual authentication on real DESFire EV2/EV3 or alternative secure card, if sourced.
- [ ] Test enrollment, per-card keying, replacement and revocation; never rely on UID for security.
- [ ] Verify genuine hardware, driver support, card suppliers, landed cost and lead times in Thailand.

**Gate A:** Reliable reader operation, explicit security assurance per card type, viable procurement.

### POC B: Local offline authorization protocol
- [ ] Implement disposable vertical slice: cashier session -> restricted refund request -> manager card + PIN -> single-use local approval.
- [ ] Disconnect WAN throughout test; StoreHub and local DB remain available.
- [ ] Bind approval to operation, transaction, amount/details, cashier, manager, expiry, nonce and version/hash.
- [ ] Enforce PIN throttling and lockout; ensure atomic consumption, replay/duplicate prevention and no bypass.
- [ ] Verify cashier remains current operator; audit records requester, approver, decision and final action.
- [ ] Test card removal, timeouts, canceled basket, edited amount, concurrent managers and terminal restart.

**Gate B:** Offline approval is secure within agreed threat model, deterministic and auditable.

### POC C: Native mobile feasibility
- [ ] Compare .NET MAUI native iOS/Android proof of concept on real devices with alternatives before committing framework.
- [ ] Verify push, foreground/background handling, deep links, biometric reauthentication and pending inbox.
- [ ] Compare local LAN QR approval with phone-signed offline QR challenge/response; validate device pairing, keys, replay defense and failure UX.
- [ ] Prototype CloudApi decision delivery to StoreHub (SignalR with polling fallback), timeouts and idempotency.

**Gate C:** Mobile path feasible on target devices; cloud absence never blocks local manager approval.

## Phase 2 — Operator access MVP
- [ ] Define EmployeeCredential, card enrollment/revocation, local credential sync/freshness and PIN/password fallback.
- [ ] Build device abstraction (e.g. ICardReader + ICredentialAuthenticator) independent of specific reader/card.
- [ ] Implement operator login/logout/switch, session persistence and configurable auto-lock.
- [ ] Preserve unfinished basket and attribution across handoff.
- [ ] Add authorization tests for least-privilege and offline flows.

**Exit:** Cashiers can work across WAN outage, switch safely and recover from reader failure using password.

## Phase 3 — Local manager approval MVP
- [ ] Approval request UI with complete operation details.
- [ ] Card + PIN manager authentication in approval context, without changing current operator.
- [ ] StoreHub policy validation, expiry, transaction-bound single-use grants, atomic consume.
- [ ] Offline deny/defer when no eligible local approver; never silently bypass.
- [ ] Durable audit, outbox and restart recovery; concurrency and idempotency tests.

**Exit:** Offline refund/void approval meets Gate B in integrated POS, with complete audit.

## Phase 4 — Remote approval (online enhancement)
- [ ] Manager device registration and revocation; secure native login and app authentication.
- [ ] CloudApi request routing, APNs/FCM notifications, manager inbox and approve/reject.
- [ ] StoreHub verification of remote decision and local single-use grant consumption.
- [ ] Handle concurrent managers, expired requests, modified baskets, lost push, duplicate delivery and offline transition.
- [ ] Optional local QR mobile approval only if POC C validates benefit and security.

**Exit:** Remote approval works when connected; WAN failure leaves local workflows unchanged.

## Phase 5 — Pilot and rollout
- [ ] Pilot in representative shops: Rungrat, MimyMart, MimyShop and Esther's Day, with separate organization/employer assumptions.
- [ ] Validate cashier speed, manager approval latency, fallback usability and training.
- [ ] Run repeated WAN outage and recovery drills; measure lost/duplicated approvals and outbox delivery.
- [ ] Verify spare readers/cards, card replacement and Thai procurement costs.
- [ ] Roll out only after security, reliability, supply and UX gates pass.

## Non-negotiable acceptance tests
1. With WAN disconnected, cashier login (using locally provisioned valid credentials), sales, operator switch/lock, local approval, audit and outbox all work.
2. Manager card tap during approval **never** changes current cashier or elevates their role.
3. One grant authorizes exactly one unchanged operation; retries, replay, expiration or changed amount cannot reuse it.
4. Cloned/UID-only cards cannot independently authorize restricted operations.
5. Failed remote approval never blocks normal sales or bypasses local restrictions.
6. Missing/revoked credentials, lockout and recovery behave according to explicit offline policy.
7. Reader disconnection does not crash POS; password fallback works.
8. Audit distinguishes requester, approver and completing employee; cloud sync is eventually consistent and idempotent.

## Decisions still open
- Production card assurance: DESFire EV2/EV3 vs ACS secure smart card vs RFID UID + PIN under bounded risk.
- Specific reader procurement and supplier authenticity/availability.
- Local QR/LAN manager approval scope; offline QR without LAN is a separate security design.
- Mobile framework, PIN policy, timeouts, transaction limits, and credential revocation freshness.

**No hardware purchases, application changes or merge are authorized by this plan.**
