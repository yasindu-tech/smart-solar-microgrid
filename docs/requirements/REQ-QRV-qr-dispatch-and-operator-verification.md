# REQ-QRV · QR Dispatch and Operator Verification

Status: **Agreed** · Owner: **Member 4** · Components: C1 API, C3 Android
Rubric: *Grid Operator Verification and Map Features — Read QR code and update the job as done (2)* · *Service Integration — QR code scanning (2)*

---

## 1. Intent

Once a reservation is approved, the prosumer's phone holds a **secure transaction QR code**. At the
node, the operator scans it, the **server** (not the phone) verifies it, the operator sees the booking
details, confirms, and the transfer is finalised. The point of the requirement is that the phone can
never authorise anything on its own.

## 2. Actors and permissions

| Action | Prosumer | Operator | Backoffice |
|---|:--:|:--:|:--:|
| Receive/display QR for own approved reservation | ✅ | ✖ | ✖ |
| Scan a QR | ✖ | ✅ mobile | ✖ |
| Verify a token (`/operator/verify-qr`) | ✖ | ✅ | ✅ |
| Finalise transfer (`/operator/finalize`) | ✖ | ✅ (assigned node) | ✖ |

## 3. Preconditions

- Reservation status is `Approved` and `qrToken` is present.
- Operator is authenticated and the reservation's `nodeId` is in their `assignedNodeIds` (BR-11).

## 4. Main flow

1. **Issue** — on approval, the API generates `qrToken` (ADR-003): a compact signed payload
   `{ reservationId, nic, nodeId, slotStartTime, nonce, iat }` signed HMAC-SHA256 with a server-side
   secret, base64url-encoded. Stored on the reservation.
2. **Display** — the prosumer's app calls `GET /reservations/{id}/qr` and renders the token as a QR
   image locally (ZXing). The token is displayed only; nothing is computed from it on the device.
3. **Scan** — the operator opens Scanner in the Android app and scans. The app extracts the token
   string. It does **not** parse or trust the contents.
4. **Verify** — app calls `POST /operator/verify-qr { qrToken }`. The API:
   - recomputes the signature → mismatch = `E4201`
   - finds the reservation by `reservationId` → missing = `E4404`
   - checks the stored `qrToken` equals the presented one → mismatch (stale token) = `E4203`
   - checks status is `Approved` → otherwise `E4203` (already `Completed` → `E4202`)
   - checks operator's node assignment → `E4303`
   - returns booking details: prosumer name, NIC, node, slot window, requested kWh, status.
   **This call writes nothing.**
5. **Confirm** — the operator sees the details on screen and taps *Finalise*, entering the actually
   delivered kWh.
6. **Finalise** — `POST /operator/finalize { reservationId, deliveredKwh }`. The API re-runs the
   verification checks, sets `status: "Completed"`, stamps `completedAt`, `completedByOperatorId`,
   `deliveredKwh`, and **invalidates the token** (BR-18).
7. The prosumer's booking moves to history on their next refresh.

## 5. Business rules

**BR-10** (server-side verification) · **BR-11** (node assignment) · **BR-18** (single use) · BR-14

## 6. Acceptance criteria

| ID | Given | When | Then |
|---|---|---|---|
| AC-1 | An approved reservation | prosumer opens it | A scannable QR is displayed, with the reservation ID visible as text underneath |
| AC-2 | That QR | operator scans it | The operator sees prosumer name, NIC, node, slot time and requested kWh — **fetched from the server**, not decoded from the QR |
| AC-3 | The operator | taps Finalise with delivered kWh | Status `Completed`, `completedByOperatorId` = that operator, prosumer's list moves it to history |
| AC-4 | The same QR | scanned again after completion | `E4202` "already used"; no state change |
| AC-5 | A QR whose payload was edited by hand | scanned | `E4201` signature invalid |
| AC-6 | A QR for a reservation at node ND-002 | scanned by an operator assigned only to ND-001 | `E4303` |
| AC-7 | A reservation that was updated after approval | old QR scanned | `E4203` — the stored token no longer matches |
| AC-8 | A reservation that was cancelled after approval | its QR scanned | `E4203` — status is not `Approved` |
| AC-9 | `verify-qr` called | — | Database is unchanged (verify is read-only). Provable by calling it twice and getting the same result |
| AC-10 | Camera permission denied on the operator device | scanner opened | A clear message with a button to open app settings; no crash |

## 7. Edge cases

| ID | Case | Expected |
|---|---|---|
| EC-1 | Operator's phone is offline at the node | Scan is captured but verification fails with a network error. **No offline approval path** — BR-14. Show "cannot verify offline, retry when connected" |
| EC-2 | Prosumer shows a screenshot of the QR taken earlier | Works **if** the token is still the stored one and the reservation is still `Approved`. That is acceptable: the token is the credential, and it is single-use |
| EC-3 | QR scanned before the slot start time | Allowed in v1 — operators sometimes start early. Document as a deliberate choice; if the group wants a window check, add BR-21 and an AC |
| EC-4 | `deliveredKwh` greater than `requestedKwh` | Allowed, stored as-is. Flagged in the operator UI with a warning, not blocked |
| EC-5 | Network drops between verify and finalize | Finalize re-runs every check, so a partial state is impossible. Retry is safe |
| EC-6 | Two operators scan the same QR within seconds | First `finalize` wins; second gets `E4202` |
| EC-7 | Very long token makes a dense, hard-to-scan QR | Keep the payload minimal (ADR-003 §3) — reservationId + nonce + signature only, ~120 chars |

## 8. Data touched

`EnergyReservation` (read `qrToken`/status; write status, `completedAt`, `completedByOperatorId`,
`deliveredKwh`), `Users` (read `assignedNodeIds`), `Prosumers` (read name for display).

## 9. API surface

`api-contract.md` §7 — `POST /operator/verify-qr`, `POST /operator/finalize`,
plus `GET /reservations/{id}/qr` from §6.

## 10. UI surface

- **Android (prosumer)**: booking detail → QR panel, with status badge and "show at the node" hint.
- **Android (operator)**: Scanner activity (camera preview) → verification result card → Finalise form
  with delivered kWh → success screen.

## 11. Out of scope

Offline verification, NFC, token expiry independent of the slot, receipts, push notification to the
prosumer on completion.

## 12. Open questions / decisions

- Resolved: HMAC-signed compact token, verified server-side → ADR-003.
- EC-3 (scanning before the window) is deliberately permissive. Be ready to say so at the viva.
