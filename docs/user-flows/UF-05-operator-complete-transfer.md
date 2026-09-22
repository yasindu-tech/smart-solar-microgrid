# UF-05 · Operator Completes an Energy Transfer

Owner: Member 4 · Requirement: REQ-QRV · Components: C3 Android (prosumer + operator) → C1 API

---

## Actors
Prosumer (presents QR) and Grid Operator (scans, finalises).

## Preconditions
- Reservation status is `Approved` with a live `qrToken`.
- Operator is authenticated, online, and assigned to the reservation's node (BR-11).
- Camera permission granted on the operator device.

## Steps

| # | Where | Action | API | Rules |
|---|---|---|---|---|
| 1 | Prosumer app | Opens the approved booking, shows the QR | `GET /reservations/{id}/qr` | BR-02 |
| 2 | Operator app | Opens Scanner, scans the code, extracts the token string | — | app does **not** parse the payload |
| 3 | API | Recomputes HMAC signature | `POST /operator/verify-qr` | **BR-10** |
| 4 | API | Loads the reservation, compares stored token, checks status `Approved` | — | BR-10, BR-18 |
| 5 | API | Checks operator's node assignment | — | **BR-11** |
| 6 | API | Returns prosumer name, NIC, node, slot window, requested kWh. **Writes nothing** | — | — |
| 7 | Operator app | Shows the verification card; operator confirms the person matches | — | — |
| 8 | Operator app | Enters delivered kWh, taps **Finalise** | `POST /operator/finalize` | — |
| 9 | API | Re-runs all checks, sets `Completed`, stamps `completedAt`, `completedByOperatorId`, `deliveredKwh`, invalidates token | — | BR-10, BR-18, state machine |
| 10 | Prosumer app | On next refresh the booking appears in History as `Completed` | `GET /reservations/me` | — |
| 11 | Operator dashboard | Approved-future count drops by one | `GET /dashboard/operator` | BR-14 |

## Why verify and finalise are two calls
The operator must *see who and what* before committing. A single call would either write before the
human confirmed, or make the human confirm blind. Two calls also make the read-only nature of
verification demonstrable at the viva (AC-9 in REQ-QRV).

## Success
Reservation `Completed`, slot consumed, QR dead, both dashboards updated.

## Failure

| Condition | Error | Operator sees |
|---|---|---|
| Token tampered / not signed by us | `E4201` | "Invalid code." |
| Already completed | `E4202` | "This code has already been used." |
| Reservation cancelled or not approved | `E4203` | "This booking is not approved." |
| Reservation was modified after approval (stale token) | `E4203` | "This code is out of date — ask the prosumer to refresh." |
| Operator not assigned to the node | `E4303` | "You are not assigned to this node." |
| Device offline | network error | "Cannot verify offline. Reconnect and try again." **No offline path** (BR-14) |

## Related
REQ-QRV · REQ-VIEW · ADR-003 · BR-10, BR-11, BR-14, BR-18
