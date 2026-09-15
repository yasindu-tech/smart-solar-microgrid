# D4 · Fulfillment & Operational Verification

Status: **Agreed** · Owner: **Member 4** · Individual marks: **13 / 65**

## Goal

Turn an approved reservation into a real, verified energy transfer — and make
sure an operator always knows what is coming today. The credential that
closes a booking must be **impossible to forge and impossible to reuse**,
because it is the one place in the system where the phone hands over
something the operator physically acts on.

## Scope, as agreed

**Web application** — Provides high-level operational dashboards displaying
current bookings, pending reservations, and counts of approved future slots.

**Mobile application** — After a reservation is approved, the Prosumer's app
displays a secure transaction QR code. Grid Operators use their mobile app
mode to physically scan this QR code on-site.

**Web Service & DB** — The centralised API receives the scanned QR payload,
verifies the data against the server to prevent fraud, and executes the final
business logic to mark the physical energy transfer as complete.

## Requirements owned

| Doc | Covers |
|---|---|
| `requirements/REQ-QRV-qr-dispatch-and-operator-verification.md` | QR issue, display, scan, server verification, finalisation |
| `requirements/REQ-VIEW-dashboards-views-and-maps.md` §2.3 | Operator dashboard — pending count, approved-future count, today's list |

## User flows owned

`user-flows/UF-05-operator-complete-transfer.md`

## Business rules owned

**BR-10** (server-side QR verification only) · **BR-11** (operator node
assignment) · BR-14 (no offline decisions — this domain is where it matters
most, since verification is the one action that must never be trusted to a
device) · BR-18 (single-use QR — the enforcement side; D3 owns the trigger)

## Data owned

Writes `EnergyReservation.status → Completed`, `completedAt`,
`completedByOperatorId`, `deliveredKwh`, `qrToken` lifecycle · reads
`Users.assignedNodeIds`

## Technical tiers this domain has code in

| Tier | What lives here |
|---|---|
| C1 Web API | `OperatorController`, `DashboardController` (shared with D1/D3 — one file, per-endpoint ownership, see `domains/README.md`), `QrTokenService`, `DashboardService` |
| C2 Web app | Operator dashboard widgets in `features/reservations/` (shared with D3) |
| C3 Android | `ui/operator/` — scanner, verification result card, finalise form, success screen |

## Why verify and finalise are two separate API calls

The operator must *see who and what* before committing anything. A single
call would either write before a human confirmed, or force the human to
confirm blind. It also makes the read-only nature of verification
demonstrable at the viva: call `verify-qr` twice, show the database is
unchanged both times. See `decisions/ADR-003` and `REQ-QRV` AC-9.

## Definition of done

Everything in `project-config.md` §8, plus:

- Editing one character of a scanned token produces `E4201`, not a crash —
  this is the cheapest, most convincing security demo in the whole project.
- Re-scanning a completed reservation's QR returns `E4202`. Scanning it at
  the wrong operator's assigned node returns `E4303`.
- Operator dashboard counts are scoped to that operator's `assignedNodeIds`,
  not system-wide — test this with two operators, two different hubs.
- No verification path exists that works without a network call. If the
  device is offline, the UI says so and stops; it does not fall back to
  anything local (BR-14).

## Viva questions this domain should be able to answer cold

1. Why does the app never parse the QR payload itself? → `ADR-003` §3, §4 —
   the signing secret cannot live on a device
2. What happens if a prosumer screenshots their QR and the booking is later
   modified? → `ADR-003` consequences, `REQ-QRV` AC-7
3. Show me a line of code that would let this be bypassed if it were wrong,
   and how you tested that it isn't.
