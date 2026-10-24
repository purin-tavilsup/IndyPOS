# IndyPOS NFC / RFID Hardware Evaluation

Status: Research / POC candidate — **no production card standard selected**.
Updated: 2026-10-21

## Goals and constraints
- Offline-first: operator identification, local authentication, manager approval and audit must function with WAN disconnected.
- Low cost, easy replacement and reliable procurement for small Thai shops.
- Shared POS terminal; cashier/operator sessions are separate from application lifetime.
- Manager taps only occasionally to approve a **specific operation**, never to switch the cashier session or grant elevated role.
- StoreHub is the local authorization/enforcement authority; CloudApi enhances remote workflows but is not required for local operations.

## Hardware candidates

| Candidate | Interface | Notes | Decision |
| --- | --- | --- | --- |
| ACS ACR1552U-M1 (USB Type-A) | 13.56 MHz, PC/SC / CCID, APDU | Preferred reader POC candidate; verify exact card support and driver behavior with actual cards. Thailand OFM listing observed at THB 3,200 (snapshot, not verified live). | POC preferred |
| ACS ACR1252U | 13.56 MHz, PC/SC / CCID | Earlier candidate; Shopee offer observed at THB 2,212 after discounts (snapshot, authenticity unverified). | Alternative |
| Generic 125 kHz USB RFID reader | Often keyboard wedge | Cheap and common, but cannot read 13.56 MHz cards; keyboard injection/focus issues and clonable IDs. | Separate low-assurance UX POC only |

Sources to recheck before purchase:
- https://store.acs.com.hk/products/575/acr1552u-usb-nfc-reader-iv/smart-card-readers/
- https://www.ofm.co.th/product/acr1552u-m1-%E0%B9%80%E0%B8%84%E0%B8%A3%E0%B8%B7%E0%B9%88%E0%B8%AD%E0%B8%87%E0%B8%AD%E0%B9%88%E0%B8%B2%E0%B8%99-rfid-nfc-%E0%B9%81%E0%B8%A5%E0%B8%B0-ntag-p.YD18480
- https://store.acs.com.hk/smart-cards-category/

## Credential/card options

| Technology | Advantages | Risks / limitations | Proposed use |
| --- | --- | --- | --- |
| MIFARE Classic 1K (13.56 MHz) | Common in Thailand, inexpensive, reader-compatible candidate | Crypto-1 compromised; UID and card contents are clonable; writing a secret does **not** solve this | UX/integration POC; low-assurance identification only |
| NTAG21x | Common, inexpensive | UID is not proof of possession; not a secure authentication card | Reader UX experiments only |
| 125 kHz EM4100/TK4100 | Widely available | Clonable identifiers; different reader required | Low-assurance identification experiment |
| MIFARE DESFire EV2/EV3 | AES-based mutual authentication possible | Thai retail sourcing uncertain; provisioning and key management needed | Secure-card POC / production candidate |
| ACS ACOSJ Combi Java Card | Programmable crypto applet option | Applet development, tooling, provisioning complexity; reader compatibility to validate | Exploratory secure-card POC |
| ACS ACOS3 Contactless | Contactless smart card option | Older cryptography; validate command set/security model | Research only |
| ACS ACOS5-EVO Combi | PKI-oriented smart card | Middleware and provisioning complexity; verify contactless crypto access | Research only |

**Supply observation:** DESFire EV3 small-quantity availability in Thailand appeared limited in initial searches. This is not proof of a national shortage. Obtain supplier quotes, minimum order quantities, genuine chip provenance and lead times before standardizing.

## Mobile alternative
- Android HCE via ISO-DEP/APDU: possible POC, but not equivalent to MIFARE Classic emulation; device/OS testing required.
- iOS NFC card emulation: platform/region/entitlement restrictions; do not make a Thailand MVP depend on it.
- Cross-platform manager approval candidate: POS displays transaction-bound, short-lived QR challenge; native app reviews and signs or sends response to StoreHub over local LAN. Offline (no WAN) is feasible with paired credentials and LAN; fully disconnected challenge/response via signed QR requires a separate design and UX/security review.
- Remote manager approval remains CloudApi + native app when online; unavailable during WAN outage.

## Security rules
1. Do not treat a card UID or keyboard-wedge string as secure authentication.
2. For restricted actions, require a separately verified manager factor (e.g. locally validated PIN) with attempt throttling, lockout, secure PIN hashing, and clear threat-model limits. RFID + PIN remains weaker than a secure cryptographic card when PIN is exposed.
3. Bind grants to operation, transaction, amount/details, requesting operator, approving manager, expiry and nonce; single-use consumption enforced atomically by StoreHub.
4. No manager session takeover, persistent role elevation or bypass on network failure.
5. Keep role/permission mapping and revocation state in StoreHub; define offline credential freshness and revocation procedures.
6. Separate card enrollment/provisioning, authentication and authorization. Never store employee PII or authoritative permissions on a card.
7. Do not claim DESFire EV3 compatibility or secure writing solely from marketing-level ISO 14443 support; prove APDU, provisioning and mutual authentication with actual cards.
8. Log approval request, credential method/assurance, manager, decision and grant consumption locally; sync asynchronously.

## POC procurement (not purchase approval)
- 1 ACS ACR1552U-M1 from verified supplier.
- 5–10 inexpensive MIFARE Classic 1K cards for UX and reader integration only.
- 2–5 genuine DESFire EV2 or EV3 cards from a verifiable supplier for secure-authentication POC.
- Optional ACS smart-card samples if DESFire sourcing fails.
- Confirm card chip, factory state, order minimum, return/warranty, documentation, driver and delivery before ordering.

## Open questions / gates
- Can .NET 10 reliably enumerate and use PC/SC reader(s), detect tap/removal and recover from disconnect/reconnect?
- Can secure-card mutual authentication, enrollment, rotation and revocation work offline?
- Is card supply in Thailand repeatable and affordable at pilot and rollout scale?
- Is UID + PIN acceptable for any production manager action under the agreed threat model, or must sensitive operations require cryptographic possession?
- Does local QR manager approval offer a better cross-platform UX/cost trade-off?
- Which PIN policies, transaction limits and recovery procedures are acceptable to pilot shops?

Do not finalize hardware/card standard until security, offline and procurement gates pass.
