# Sync hardening: no stuck queue, store heartbeat, resend from the cloud

**Status:** Approved (Pond, 2026-10-23)
**Owner:** Pond
**Comes before:** cloud platform slice 1 (`2026-10-21-cloud-platform-and-identity-design.md`)

## Why

The cloud exists so that every sale a store makes ends up somewhere Pond can see and query, and
**no event is ever lost**. Offline-first comes first: the till never waits on the cloud. Cloud sync
comes second, but it is the reason for the whole cloud platform.

Reading `SyncWorker` before the cloud goes live showed that one bad event can quietly stop a store's
sync for good:

- When an outbox event reaches `MaxRetries` (5), `SyncWorker.HandleFailedEventAsync` logs
  "Leaving for manual review" and **returns without writing anything**. The event stays `Pending`,
  and its `NextRetryUtc` is already in the past.
- `OutboxRepository.GetPendingEventsAsync` picks the oldest 10 due `Pending` events. So the event is
  picked again **on every 5-second poll**, sent again, and fails again, with no backoff.
- Once **10** events are in that state they fill every batch, and **no newer event is sent again**.
  The till keeps selling, but the cloud falls further behind and nobody is told.
- "Manual review" cannot happen: nothing marks the event as stuck, and `GetFailedCountAsync` counts
  every retried `Pending` event, not the ones that gave up.
- The same limit also gives up on events that would have gone through: five attempts with 30 s
  doubling backoff is about 8 minutes, so a short internet outage puts good sales on the path above.

The causes that make an event fail for good are real and likely at rollout. The cloud answers 403
when an event names a different store than the token (a wrong `--store-id` given to the migrator,
which nothing checks today), and 400 for a malformed batch.

Pond also wants to see, from the cloud admin page, whether each store is OK, and to **resend** stuck
events from there once the cause is fixed.

This was found by reading the code, not by running it. Step 1 of the build is a test that shows it.

## What success looks like

- A bad event can never hold back the events behind it.
- An outage of any length (hours, a day) never turns a good sale into a failed one. It is sent when
  the cloud comes back.
- An event the cloud refuses is kept, with its payload, marked `Failed`, and visible to Pond.
- From the admin page Pond can see each store's state and press **Resend** for its failed events.
  The store picks the request up by itself, through any router, whenever it is next online.

## Goals

1. StoreHub tells a **temporary** failure from a **permanent** one, and handles each its own way.
2. A `Failed` outbox status: out of the queue, kept, countable, resendable.
3. The migrator refuses a `--store-id` that is not the installed StoreHub's store.
4. A **heartbeat** from StoreHub to CloudApi, so the cloud knows each store is up and syncing.
5. A **rejection log** in CloudApi, so the cloud knows what it refused even when the store cannot
   tell it.
6. **Resend from the cloud**, carried to the store in the heartbeat's reply.
7. The data and endpoints the admin page needs. The page itself is built in cloud slice 1.

## Non-goals

- Anything on the till screen. Store staff are not told about sync problems.
- Alerts, emails or push notifications. Pond reads the admin page.
- Opening hours per store, and any judgement of "should be open now".
- Health of the till app, the receipt printer or the cash drawer. "OK" means **StoreHub is up and
  syncing** (Pond, 2026-10-23).
- A history of heartbeats or uptime charts. Only the latest heartbeat per store is kept.
- Faster bulk catch-up (sending a batch in one request). It is a known gap (about 5,181
  GeneralHardware debt events take 43+ minutes at cutover) and stays on the pilot checklist.
- Changing how the cloud processes events after it has accepted them. Its inbox already retries
  with backoff (`EventProcessor`, `AddInboxRetrySchedule`).

## Anti-goals

- **Never lose an event.** Not by deleting it, not by marking it `Sent` when the cloud did not
  store it, not by a resend that drops it.
- **Never let the cloud slow down a sale.** The heartbeat and the sync stay in background workers.
- **Never let a cloud outage turn into `Failed` events.** Only an answer the cloud *gave* can make an
  event `Failed`.
- **Never let the cloud push into a store.** Stores sit behind routers and are often off. The store
  always asks; the cloud only answers.

## Constraints and facts

- Store PCs are **usually switched off at night** when the store closes (Pond, 2026-10-23). A store
  that is not seen overnight is normal.
- The cloud answers `POST /sync/events` with **200 and a result per event**. `Accepted: true` (with
  `Reason: "duplicate"` for one it already has) is success. `Accepted: false` comes only from an
  exception while storing it, so it is a cloud-side fault, and temporary.
- The cloud answers **400** for a malformed batch and **403** when an event's payload names a store
  other than the token's, or has no readable top-level string `StoreId` (not JSON, not an object, or
  no such field). These are the permanent answers known today.
- A failed token request (disabled store, bad secret, Auth down) shows up in StoreHub as "no token".
  The store cannot tell these apart from here, so it is treated as temporary. A store with broken
  credentials shows up on the admin page as not seen.
- After cloud slice 1, tokens come from IndyPOS.Cloud.Auth instead of CloudApi. The heartbeat uses
  the same store token as `/sync/events`, so it moves with it.
- `GET /sync/status` already returns, for the token's store, the cloud's total, processed and
  unprocessed event counts. The admin page can reuse that query.
- Database migrations are **forward-only** (CLAUDE.md release gate): additive, nullable or with a
  default. The new outbox status is a new string value in the existing `Status` column.

## How a failed send is handled (StoreHub)

| What happened | Kind | What StoreHub does |
|---|---|---|
| Network error, timeout, DNS failure | temporary | Retry with backoff, **no limit** |
| No token could be obtained | temporary | Same |
| 5xx, 429 | temporary | Same |
| 401 | temporary | Clear the token, retry once with a fresh one (as today), then back off |
| 200, event `Accepted: false` | temporary | Back off (the cloud failed to store it) |
| 200, event `Accepted: true` (incl. duplicate) | success | Mark `Sent` |
| 400 or 403 **marked as CloudApi's own refusal** | **permanent** | Mark `Failed`, keep the payload, record the cloud's reason |
| Any other 4xx, including an unmarked 400/403 (404, 408, 413, an auth-layer 403...) | temporary | Back off |

**Only CloudApi's own, deliberate refusals are permanent.** `/sync/events` marks them: a problem
details body carrying a known refusal code. A bare 4xx from anywhere else (a wrong base URL, a route
not deployed yet, the proxy, the auth layer) is treated like an outage. Otherwise one bad deploy or
URL change, likely around cloud slice 1's move to Auth and new hosting, would put every event in
every store into `Failed` within a poll. *(Claude's call, from the anti-goal that only an answer the
cloud gave may fail an event. Code and body shape are the builder's.)*

- **Backoff** starts at 30 s, doubles, and is **capped at 15 minutes**. With no attempt limit, an
  event retries at least four times an hour until the cloud is back. *(Claude's call.)*
- `MaxRetries` stops being a give-up limit. *(Whether the setting is removed or kept for something
  else is left to the builder; it must not move an event to `Failed`.)*
- `Failed` events are not picked up by `GetPendingEventsAsync`, so the events behind them keep
  flowing.
- The reason the cloud gave (status and message) is stored on the event, so Pond can see why it
  failed. *(Column name and size are the builder's.)*
- `GetFailedCountAsync` counts `Failed` events only.
- `SendEventAsync` returning `bool` cannot carry "temporary vs permanent". The builder changes the
  contract so it can. *(Shape left to the builder.)*

### Order after a skip

When a `Failed` event is skipped, newer events reach the cloud before it, and before its resend.
The cloud's mirrors are designed to tolerate this (newest-wins upserts, inbox retry when a needed
row is missing). The builder **verifies** this for each event type that has a dependency, such as a
ลงบัญชี repayment whose debt event failed. Any type that would be corrupted, rather than delayed,
by arriving early is brought back to Pond before building.

## Migrator store-id check

`IndyPOS.MigrationTool` takes `--store-id` and writes it into every migrated row and outbox event.
StoreHub's own store id is its configured `Store:Id` (`StoreIdentityService`). If the two differ,
every migrated event is refused by the cloud with 403. That used to stall the store for good; after
this work it fills the `Failed` list instead, which is visible but still wrong.

The authority that actually decides a 403 is the token's `store_id` claim, which comes from the
cloud's store registration. So the migrator checks both: *(Claude's call.)*

1. **Always:** `--store-id` must equal the StoreHub's **explicitly configured** `Store:Id`. The
   `STORE-{MachineName}` fallback in `StoreIdentityService` does not count: if `Store:Id` is not
   set, the migrator refuses and says so.
2. **When the cloud is reachable:** it gets a token with the `--client-id` / `--client-secret` it
   already takes, and `--store-id` must equal the token's `store_id`. If the cloud cannot be reached,
   it says so and carries on. Migration must not depend on the cloud, and a mismatch found later
   still shows up as `Failed` events on the admin page.

On any mismatch it refuses to start and prints both values. *(How it reads StoreHub's configuration
is the builder's.)*

## Heartbeat

- StoreHub sends a heartbeat to CloudApi **every 2 minutes** while it runs, in its own background
  worker, independent of the sync worker. *(Claude's call.)*
- It carries: StoreHub version, the number of `Pending` events, the number of `Failed` events, the
  age of the oldest `Pending` event, a **bounded list of the `Failed` events** (id, type, failed-at,
  reason; the oldest 50 is enough), and the ids of events resent since the last heartbeat (see
  Resend). *(Claude's call.)*
- **The store is the source of truth for its own `Failed` events.** The cloud shows what the latest
  heartbeat listed. The rejection log only fills the gap when the store cannot get a heartbeat
  through, and keeps the cloud's side of the story for diagnosis.
- This is a wire contract between versions: CloudApi must accept a heartbeat with fields it does not
  know, and StoreHub must accept a reply with fields it does not know.
- It uses the store token and is accepted only for the token's own store.
- CloudApi keeps **one row per store**, overwritten by each heartbeat, with the time it arrived.
- It never blocks or delays anything else in StoreHub. A failed heartbeat is logged and the next one
  is sent on schedule.

## Rejection log (CloudApi)

- When CloudApi refuses a sync request with 400 or 403, it records, per event it can identify: the
  store from the token, the event id, the event type, the status, the reason, and the time.
- It is what shows Pond a refused event even when the store's own heartbeat cannot get through.
- An unreadable batch that names no event is still logged once, with the store and the reason.
- Kept small: the same event refused again updates its existing entry (count and last time) rather
  than adding a row per attempt. *(Claude's call.)*
- **An entry is resolved** when that event is later accepted by `/sync/events`. Resolved entries
  stay for history but no longer count towards 🔴. *(Claude's call.)*

## Resend

1. On the admin page Pond presses **Resend** for a store's failed events: **all** (the store resends
   every event it has in `Failed`, whatever the cloud knows about) or **one** (by id, from the
   heartbeat's list or the rejection log). *(Claude's call.)*
2. CloudApi records the request for that store. A resend that has not been picked up yet is not
   duplicated by pressing again.
3. At the store's next heartbeat, the reply carries the request.
4. StoreHub moves those events from `Failed` back to `Pending`, with the attempt count and backoff
   reset, and reports their ids in the next heartbeat.
5. CloudApi marks the request done when the store confirms it. For "all", the confirmation is the
   heartbeat that lists the ids it moved.
6. If the cause was not fixed, the events fail again and return to `Failed`. Nothing loops: a resend
   happens only when Pond asks for one.

An event id the store does not have, or one that is not `Failed`, is ignored and reported back, not
treated as an error.

## Store status (shown on the admin page)

Worked out from the latest heartbeat, the rejection log and `/sync/status`'s counts:

| Status | Rule |
|---|---|
| 🟢 OK | Seen in the last 10 min, no failed events, oldest pending event under 30 min |
| 🟡 Behind | Seen in the last 10 min, oldest pending event 30 min or more |
| 🔴 Failed events | The latest heartbeat reports failed events, **or** an unresolved rejection newer than the latest heartbeat. Resend shown here |
| ⚪ Offline | Not seen for 10 min or more. Shown as "last seen <time>", in the store's time zone, in neutral grey, **not as an alarm** |

- 🔴 wins over the others: a store can be offline and still have failed events waiting.
- When the two sources disagree, the newer one wins: a heartbeat saying 0 failed clears 🔴 even if
  older rejection entries are unresolved (they stay visible as history). *(Claude's call.)*
- The thresholds are settings with these defaults. *(Claude's call.)*
- The page also shows the cloud's own unprocessed count from `/sync/status`, so an event that
  reached the cloud but is stuck in processing is visible too.

## Delivery

Three PRs, each merged before the next is planned:

1. **StoreHub stops the stall.** Failure classification, `Failed` status and reason, uncapped
   backoff, failed count, migrator store-id check. Stands alone and is the urgent part. Includes a
   regression test that fails on today's code: ten events the cloud refuses, and a newer one that
   must still be sent. Also marks CloudApi's own 400/403 refusals (see the table), and corrects the
   comment in `SyncEventsEndpoints` that says StoreHub treats any 2xx as sent and backs off "for
   manual review"; neither is true after this work.
2. **Heartbeat, rejection log and resend.** CloudApi endpoints and tables, the StoreHub heartbeat
   worker, and the resend round trip. Uses today's store token.
3. **Admin page section.** Built with cloud slice 1's Cloud.Web, on the endpoints from PR 2,
   behind an Admin person token.

## Proving it on the cloud before go-live

The DigitalOcean environment will be production, but until a real store is switched over it is
free to test on: seed test data, bring it up and down, break it on purpose (Pond, 2026-10-23).
Real stores only connect once there is enough confidence, after a reset to a clean state.

So, besides the automated tests, the following is run against the deployed cloud with a dev or test
StoreHub, once PR 2 and the admin page exist:

- **Outage:** stop CloudApi (and later Auth) for longer than the old 8-minute give-up, keep selling,
  bring it back. Every sale arrives; none is `Failed`; the store shows ⚪ then 🟡 then 🟢.
- **Bad deploy:** point the store at a wrong path or take the route away (a bare 404). Nothing
  becomes `Failed`; it catches up once fixed.
- **Real refusal:** send events for the wrong store. They become `Failed`, newer sales keep
  flowing, the page shows 🔴 with the reason, and the rejection log has them.
- **Resend:** fix the cause, press Resend (one, then all), and watch the events arrive and 🔴 clear.
- **Night:** switch the store PC off and on. ⚪ shows "last seen" without an alarm, then 🟢.

Before the first real store: wipe the seeded and test data, test stores and their clients, and the
rejection log and heartbeats, and re-register the real stores. *(The reset itself belongs in the
deployment runbook.)*

## Left to the builder

- Endpoint shapes and names, following `docs/architecture/api-conventions.md`.
- Table and column names, and how the reason text is stored and bounded.
- The new contract of `ICloudSyncClient.SendEventAsync`.
- What happens to the `MaxRetries` setting.
- How the migrator learns StoreHub's store id.
- How the resend request is keyed and expired if a store never comes back.
- Test layout, as long as every rule above has a test that would fail if it broke. Above all:
  "ten refused events do not block a newer one", "an outage never makes an event `Failed`", and
  "a resend never loses an event".
