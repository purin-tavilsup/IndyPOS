# Cloud platform and identity — design

Status: Approved (Pond, 2026-10-22)
Date: 2026-10-21
Author: Pond (with Claude)

## Why

The goal is to **ask about the stores from anywhere**: from home, on the road, from another store.
The admin and managers need answers from their phone or laptop, through Claude or ChatGPT (an MCP
server), a web dashboard, and a mobile app for alerts and approvals.

The cloud already collects sales, cash drawer days and ลงบัญชี debts, but nobody outside a store can
reach it: the cloud is not deployed, and there is no way for a *person* to sign in to it. Only store
machines can (OpenIddict client credentials).

Planning the deploy exposed a concrete gap. `POST /admin/stores/register` accepts only a
`StoreHubJwt` signed with the cloud's `LocalToken` secret, and in production nothing can mint one —
each installed StoreHub has its own random secret (`DatabaseSetup.GenerateJwtSecret`), correctly,
and no tool or runbook step exists. So on deploy day **no store could be registered**. Pond chose to
fix this properly — real admin sign-in — rather than with an interim command.

This document fixes the **platform shape and the identity model once**, so the later pieces follow
it, and details **only the first build slice**: the one the deploy needs.

## The pieces, and their order

| # | Piece | Unblocks |
|---|-------|----------|
| **1** | **This design** — platform shape, rules, identity. Build slice: **IndyPOS.Cloud.Auth + Cloud.Web admin** | The cloud deploy |
| 2 | Cloud.Web dashboard + person-facing read API + SignalR live updates | Live sales, store health |
| 3 | MCP server (Cloud.Mcp) | Asking from Claude / ChatGPT |
| 4 | Stock + products to the cloud | "Top 10 low stock", sales by product type |
| 5 | IndyPOS Tower (mobile: alerts + approvals) | Lands with #143's approval phases |

Overall order: **sign-in spike → #1 → cloud deploy (I1–I3) → store rollout (Epic 3) → #2 → #3 → #4**.

**The sign-in spike comes first** (Pond, 2026-10-21). Throwaway: a minimal MCP endpoint plus
OpenIddict with Client ID Metadata Documents (CIMD) and PKCE, reached through a Cloudflare tunnel,
proving that **both** the Claude app and ChatGPT can sign in. It runs before slice 1 because store
assignments (and PR #143 later) hang off the person id Auth issues: if the spike fails, WorkOS takes
over person tokens *before* any real account exists, not after. #5 follows
the operator-access design (PR #143). Each of #2–#5 gets its own design.

## Goals

- One identity for every **person** using the cloud: the admin and managers.
- Managers see **only the stores assigned to them**; some have all stores, some one.
- Sign-in from the Claude app, ChatGPT, a browser and a native mobile app, phone or laptop.
- Every app **independently deployable and scalable**.
- Fill the deploy gap: the admin can register stores in production.

## Non-goals

- **The till login.** Cashiers sign in at the till through StoreHub, offline, and that is being
  redesigned in PR #143 (NFC card, password fallback). Cashiers get **no cloud accounts**.
- Dashboards, the MCP, Tower and stock sync — pieces #2–#5, designed separately.
- Self sign-up, passwords, enterprise SSO (SAML, company directories).
- A separate "Owner" role. A Manager assigned to every store already sees everything.

## Anti-goals (a failure even if it works)

- **The till ever waits on the cloud, Auth or Google.** Offline-first is priority one.
- **Any app reads the database directly.** Data reaches apps only through an API.
- **A manager sees a store they are not assigned to**, through any app.
- **Passwords stored anywhere** in the cloud.
- **A screen built without thought for the person using it** (see UX below).

## Platform shape

```
 Claude / ChatGPT    Browser                StoreHub x3
        |               |                        |
+-------v------+ +------v-------+                |
| Cloud.Mcp    | | Cloud.Web    |                |
| (MCP, BFF)   | | (Blazor, BFF)|                |
+-------+------+ +------+-------+                |
        | user token    | user token             | store token
+-------v---------------v------------------------v---+
| CloudApi: REST API, business data                  |
+-------------------------+--------------------------+
                          |
                    +-----v------+
                    | PostgreSQL |
                    +------------+

Every token above is issued by:
+----------------------------------------------------+
| IndyPOS.Cloud.Auth: OpenIddict + Google sign-in    |
| own identity database                              |
+----------------------------------------------------+
```

IndyPOS Tower (the mobile app, piece #5) is one more user-token client of CloudApi, next to Cloud.Web
and Cloud.Mcp.

### The rules (decided by Pond)

1. **Each app is its own container**, deployed and scaled on its own: IndyPOS.Cloud.Auth, CloudApi,
   Cloud.Web, and later Cloud.Mcp.
2. **Apps never touch a database.** Cloud.Web, Cloud.Mcp and Tower get data only through CloudApi.
   Each **backend service owns its own data**: Auth owns identity, CloudApi owns business data.
3. **IndyPOS.Cloud.Auth is its own service** and issues **every** token: people (Web, MCP, Tower) and
   stores (client credentials). CloudApi becomes a pure resource server: it validates tokens and
   issues none.
4. **BFF.** Cloud.Web is server-rendered Blazor and holds tokens on its server; the browser holds
   only a cookie. Cloud.Mcp is the AI clients' BFF. CloudApi offers plain, reusable endpoints;
   **anything shaped for one screen or one tool lives in that app's BFF, never in CloudApi.**
5. **Identity in the token, data access in the API.** The token says who you are and your role.
   CloudApi decides which stores you can touch, on every request (see Identity).
6. **Live updates are signals, not data** (piece #2 builds this). CloudApi hosts a SignalR hub that
   sends tiny "store X: sales changed" signals to the groups of the stores a person is assigned to.
   The listener refetches through the normal API, so the store check stays in one place and a signal
   can never leak data. Cloud.Web's server holds the hub connection, not the browser. In Tower, push
   (APNs/FCM) gets attention while the app is closed and SignalR takes over while it is open.
7. **UX matters.** Every UI piece is designed for the person using it — managers on phones with patchy
   4G as much as the admin on a laptop. Screens get mockups Pond reacts to before they are built.

### Hosts and names

- The domain is **`exconeer.com`**, bought through Cloudflare Registrar, replacing `indypos.com` from
  the I1–I3 spec *(Pond)*. Caddy routes by host name: **`indypos.exconeer.com`** (Cloud.Web),
  **`indypos-auth.exconeer.com`** (Auth), **`indypos-api.exconeer.com`** (CloudApi), and later
  `indypos-mcp.exconeer.com`. Flat, not nested (`auth.indypos.exconeer.com`), because Cloudflare's
  free edge certificate covers only one level below the domain and the proxy stays on *(Pond)*. The
  `indypos-` prefix keeps the names clear of other products on the same domain, such as the
  workforce SaaS.
- IndyPOS.Cloud.Auth keeps its data in its **own database on the same managed PostgreSQL cluster** as
  CloudApi, so cost stays flat. Each service connects with its **own login role**, which cannot
  read the other's database, so rule 2 holds in the database too. *(Pond: shared first, split
  later.)*
- Project names: `IndyPOS.Cloud.Auth` and `IndyPOS.Cloud.Web`. CloudApi keeps its project name; Pond thinks
  of it as "the IndyPOS cloud API", and any rename is a separate tidy-up. Auth is `Cloud.`-prefixed
  because it serves only the cloud: till sign-in stays in StoreHub, offline. *(Pond.)*

### When to revisit the shape

Written down so a later split or addition is deliberate, not drift:

- **More than one instance** of CloudApi or Cloud.Web → SignalR needs a backplane (Redis) or sticky
  sessions.
- **The shared API gets awkward for one consumer** → that consumer grows its own BFF endpoints (rule 4).
- **Dashboard queries slow sync ingestion** → a read replica or a reporting store for CloudApi.
- **CloudApi load slows sign-in**, or Auth serves more than IndyPOS → Auth moves to its own
  PostgreSQL cluster.

## Identity

### Who gets an account

The **admin** (Pond) and **managers**. Nobody else. Cashiers stay till-only (non-goals).

### Sign-in

- **Google only**, through IndyPOS.Cloud.Auth (OpenIddict). No passwords stored anywhere. Managers all
  have Google accounts (Pond).
- Auth is built so **one person can link several sign-in methods**. Passkeys (built into .NET 10's
  ASP.NET Core Identity) and LINE Login are the expected additions; neither is in this design's
  build.
- **Why our own OpenIddict, not a hosted provider:** the security spec already names the cloud as the
  identity authority with OpenIddict, OpenIddict already runs here, one issuer serves machines and
  people, and store assignments are our data either way. **Fallback:** if the sign-in spike shows
  Claude and ChatGPT cannot sign in through it, WorkOS AuthKit issues person tokens instead, decided
  before slice 1 is built.

### Roles

**Admin** and **Manager**, carried in the token. A role change or deactivation takes effect within
one access-token lifetime (15 minutes, per the security spec).

### Store assignments

- A person ↔ store link, **owned by CloudApi**, next to the stores it points at.
- Checked on **every** person-facing request, so removing an assignment takes effect on the next
  request.
- Not in the token. Tokens go stale and grow; the API that owns the data guards it. (This is the
  common "identity ≠ permissions" practice.)
- An **Admin** sees every store and needs no assignments. *(Claude's call.)*

### Getting an account: invitation

1. In Cloud.Web the admin fills **one form**: name, email, role, stores.
2. Auth creates the person **at once**, in an **Invited** state, with a stable id. Cloud.Web then
   saves the store assignments in CloudApi against that id. The person exists first, assignments
   hang off it immediately, and activation comes last.
3. The admin **copies the invite link** and sends it, for example over LINE. Real invite emails can
   come later.
4. The person opens the link and signs in with Google. The account turns **Active**, bound to
   **whichever Google account they used**, not to the typed email, and their stores are already
   there.
5. A link is **single-use** and **expires after 2 days** *(Pond)*. It is not bound to the typed email,
   so a leaked link could be claimed by the wrong Google account; the short expiry limits that, and
   the People list shows which Google account claimed it so the admin can deactivate it. The admin can **re-send**
   (new link, old one dead) or **cancel** an invite.

Nobody can sign up on their own. A Google sign-in with no Active account and no valid invite is
refused.

### The first admin

Auth's server configuration names one Google email. Its first sign-in creates the person as Admin and
Active. It is the only account that skips an invite. *(Pond: "bootstrap via config is fine".)*

### Deactivating someone

In Cloud.Web, done by Auth: the person can no longer sign in and their refresh tokens are revoked.
Access ends within one access-token lifetime.

### Workforce SaaS (separate)

The planned workforce SaaS (attendance, shifts, payroll) has **its own stack and its own auth
service**, because it may become a commercial product (Pond, 2026-10-21). IndyPOS.Cloud.Auth is therefore
IndyPOS's alone: no Organization layer, no product-neutral naming. Integrating the two later (for
example IndyPOS trusting the workforce product's sign-in, or the reverse) is a future design and
needs nothing from this one beyond the stable person id.

### Link to store employees (not built)

PR #143 will need "this cloud person is this store employee", so an approval can name the manager who
gave it. The person's **stable id** is that hook. The link itself is #143's to define.

## Build slice 1: Auth + Cloud.Web admin

Everything below is what the cloud deploy needs.

### IndyPOS.Cloud.Auth (new service)

- OpenIddict server with Google as the external sign-in, the invitation flow, and the
  configured first admin.
- Authorization code + PKCE for person clients. Cloud.Web is the first one.
- **Store client credentials move here** from CloudApi: the store's OpenIddict application, the
  `/oauth/token` client-credentials grant, and the scopes `sync.write` and `master.read`.
- **Auth owns whether a store may get a token.** Today `TokenController` reads
  `StoreConfigs.IsActive` in CloudApi's database; Auth may not (rule 2). So the store's client carries
  its own enabled flag in Auth, and CloudApi sets it through the same Auth API it uses for
  register-store. Auth also takes over `LastAuthenticatedAtUtc`, since it is the one that sees the
  token request. The refusal of a disabled store keeps a test that would fail if it broke.
  *(Claude's call.)*
- **Deactivating a store is in this slice** (Cloud.Web action → CloudApi → Auth disables the client).
  Nothing can deactivate a store today, and it is the safety valve for a stolen till PC.
  *(Claude's call.)* **Deactivation is final: there is no re-activate.** A store that should come
  back is registered again like a new store, with a new client id and secret, so the old
  credentials never work again *(Pond)*.
- Its own Aspire resource and its own database, next to `cloud-db`.
- **Dev and tests never use real Google.** Development and the test suites point Auth at a
  stand-in OIDC provider (a mock OAuth2 server container in Aspire and in Testcontainers / CI)
  instead of Google, through configuration. Auth has **no** sign-in shortcut in its code, so there is
  no bypass to leave switched on. *(Claude's call.)*

### IndyPOS.Cloud.Web (new app)

- Blazor, server-rendered. Sign-in with Google through Auth.
- **Stores:** register a store, showing the client id and secret **once**, with a copy button.
- **People:** invite, list (with Invited / Active / Deactivated), re-send or cancel an invite,
  deactivate, change role.
- **Assignments:** pick stores per manager.
- Admin only. A Manager who signs in sees a friendly "nothing here yet" page until piece #2.
- Screens are mocked up for Pond before they are built.

### CloudApi (changed)

- **Stops issuing tokens.** It validates Auth's tokens (signed JWTs; access-token encryption is
  already off) for both stores and people.
- **Its OpenIddict tables are dropped**, not left dead. The forward-only gate protects a *released*
  schema, and the cloud has never been deployed (Pond, 2026-10-21); only local dev databases hold
  registered stores, and dev stores re-register on start (`DevStoreRegistration`). *(Claude's call.)*
- New **store assignment** data and the endpoints Cloud.Web needs to manage it.
- `POST /admin/stores/register` keeps its job, now behind an **Admin person token**. Register-store
  creates the store's client **in Auth** (through an Auth API that only CloudApi may call), so the
  client registry has one owner. *(How CloudApi talks to Auth here is left to the builder.)*
- **The `StoreHubJwt` scheme and its shared `LocalToken` secret are removed from CloudApi.** The
  `/admin/users` routes (cloud copies of till users, Epic S2) move behind an Admin person token too;
  they get no Cloud.Web screen in this slice.

### StoreHub (changed)

- Requests its token from **Auth** instead of CloudApi: a separate base address for the token call,
  because today one HttpClient serves both. No store is live yet, so this costs nothing in the field.

### Deploy (I1–I3 spec and runbook)

- Three containers behind Caddy: Auth, CloudApi, Cloud.Web.
- Runbook refresh: the domain (`exconeer.com` and the host names above, in the runbook,
  `deploy/cloud/.env.example` and the I1–I3 spec), today's table list (it still expects 13), "register a store" through Cloud.Web,
  the first-admin setting, and a **restore drill** before real data arrives.
- A deliberate decision recorded that the cloud keeps ลงบัญชี debtor names and staff names (PDPA).

## Left to the builder

- Endpoint shapes, following `docs/architecture/api-conventions.md`.
- How Auth stores people, invites and sign-in methods (ASP.NET Core Identity or OpenIddict's stores
  plus own tables).
- How CloudApi and Auth authenticate each other for register-store.
- What re-registering a deactivated store does to its existing cloud row and history (new store
  id, or the same id with a fresh client), as long as the old client stays dead.
- Refresh-token handling inside Cloud.Web.
- Test layout, as long as every rule above has a test that would fail if it broke — above all
  "a manager never sees an unassigned store" and "a refused invite never creates an account".

## References

- Security spec: `.planning/indypos-overhaul/security/indypos_security_design_spec.md`
- Cloud deploy: `docs/superpowers/specs/2026-09-19-cloud-deploy-i1-i3-design.md`,
  `docs/operations/cloud-deployment.md`
- Store client registry: `docs/superpowers/specs/2026-08-20-cloud-store-client-registry-design.md`
- Operator access and approvals: PR #143, `docs/architecture/operator-access-and-manager-approval.md`
- Store rollout: `.planning/indypos-overhaul/epic-3-store-rollout-plan.md`
