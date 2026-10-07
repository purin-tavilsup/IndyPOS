# StoreHub API conventions

Every StoreHub route follows these rules. A new route that cannot follow one names the rule it
breaks, and why, in a comment beside its `Map…` call.

## Paths

- **One root per resource.** `/payment-methods` serves the till and the admin. A wider view is a
  query, not a second root (`GET /payment-methods?include=all`).
- **Nouns, not verbs.** A command is a sub-resource it creates: `POST /sales`,
  `POST /pay-later/{id}/payments`, `POST /products/{id}/stock-adjustments`,
  `POST /sales/{id}/reprints`.
- **Typed keys share a slot.** `/sales/{id:guid}` and `/sales/{number:long}` sit on the same segment
  and the route constraint picks one.
- **Filters and paging are query parameters.** `GET /sales?from=…&to=…`.
- **`/reports` is for aggregates only.** A list of records is its resource's `GET`, not a report.
- **Left alone on purpose:** `POST /products/next-barcode` advances a counter, so it cannot be a
  safe `GET`, and `POST /barcodes` adds no clarity. `/reports/legacy/*` keeps the WinForms shapes
  until the Avalonia port drops them.

## Who did it

- **The user comes from the token, never the body.** A write that records a user reads
  `GetRequiredUserId()` and adds `RequireUserIdFilter`, so a token without a user id gets 401.

## Responses

- **A refused request** answers 400/404/409 with a Thai `{ "error": "..." }` the cashier can read.
- **A policy-level 401 or 403 has no body.** So does a check run inside a handler with
  `Results.Forbid()`.
- **A new sale** answers 201 with `Location: /sales/{id}`.

## Renaming or removing a route after go-live

Never rename or remove a shipped route in one step:

1. Add the new route.
2. Keep the old one as a deprecated alias for **one release.** It calls the same handler and returns
   a `Deprecation` header.
3. Move the till client to the new route.
4. Remove the alias in the next release.

The reason: the installer upgrades StoreHub before the till and never rolls the till back. A till
that cannot reach its route cannot ring up a sale, and offline-first is priority #1.

Before go-live a hard rename was safe, and the route tidy-up of October 2026 did exactly that. An
old path then answers 404, or 405 where another route still owns the path for a different verb
(`POST /sales/complete`, because of `GET /sales/{value}`).
