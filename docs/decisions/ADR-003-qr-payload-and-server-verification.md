# ADR-003 · QR Payload Format and Server-Side Verification

Status: **Accepted** · Date: 2026-09-13 · Deciders: whole group

## Context

The assignment says the mobile app "generates a secure transaction QR code" and that the operator
"scans the prosumer's transaction QR code, verifies server data, and finalises energy transfer
business logic". The word doing the work is **secure**. A QR code that simply contains a reservation
ID is a URL anyone can guess and type.

We also have a hard architectural constraint: no business logic in the clients (FAT service). So the
phone may *render* a credential but must never *evaluate* one.

## Decision

### 1. The API issues the token, not the phone

On approval, the **API** generates the token and stores it on the reservation. The prosumer app fetches
it (`GET /reservations/{id}/qr`) and renders it as a QR image with ZXing. The phone generates the
*picture*, not the *credential*.

### 2. Payload

```
base64url( {"r":"RS-0107","n":"<nonce>","t":<issuedAtUnix>} ) + "." + base64url( HMAC-SHA256(payload, serverSecret) )
```

- `r` — reservation ID
- `n` — a random 16-byte nonce, regenerated on every issue
- `t` — issued-at
- Signature — HMAC-SHA256 with a secret held only in `appsettings.json` on the server

Roughly 120 characters, so the QR stays low-density and scans reliably on a cheap camera.

### 3. Verification is entirely server-side

`POST /operator/verify-qr` recomputes the signature, loads the reservation, and checks that the
presented token **equals the stored token** and the status is `Approved`. The operator app sends the
raw scanned string and does no parsing. If the API is unreachable, there is no verification — and
therefore no transfer (BR-14, no offline path).

### 4. Single use

Finalising sets `Completed` and the stored token no longer matches any valid state (BR-18). Re-scanning
returns `E4202`.

### 5. Re-issued on update

Updating an `Approved` reservation generates a **new** nonce and token. The old QR, including a
screenshot of it, stops working (`E4203`). This is why UF-03 step 8 exists.

## Alternatives considered

| Option | Why rejected |
|---|---|
| QR contains the plain reservation ID | Not "secure" in any sense. An operator could finalise any booking by typing an ID. Fails the requirement's wording and is an easy viva kill-shot. |
| QR contains a full JWT | Works, but is 300–500 characters, produces a dense QR that scans badly in poor light, and duplicates auth machinery for a single-purpose credential. |
| Phone generates the token and signs it | Requires a secret on the device. A secret in an APK is not a secret. Also puts logic in the client, violating the FAT-service constraint. |
| Store only a hash of the token server-side | Marginal benefit here (the token is short-lived and single-use) for real added complexity. Storing the token lets us compare directly and detect stale tokens cleanly (`E4203`). |
| Add an independent expiry to the token | The reservation's slot window already bounds it, and the status check does the real work. One fewer thing to explain. |

## Consequences

**Good**
- A tampered or fabricated QR fails on signature alone — demonstrable at the viva by editing one
  character of the string and scanning.
- Stale QR after modification is detected, which is a subtle case most groups will miss.
- Verification is read-only, so an accidental double scan cannot corrupt state.

**Bad / accepted**
- The signing secret lives in `appsettings.json`. Real systems use a key vault. **Say this in the
  report's challenges section** rather than pretending it is production-grade.
- A screenshot of a valid QR works until the reservation changes or completes. Acceptable: the token is
  the credential, it is single-use, and the operator sees the prosumer's name and NIC on screen before
  finalising (REQ-QRV step 7).

## Follow-ups
- Secret must not be committed. Add `appsettings.Development.json` to `.gitignore` and document the
  required key in the README deployment steps.
