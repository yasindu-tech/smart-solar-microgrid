# D3 · Reservation & Scheduling Engine

Status: **Agreed** · Owner: **Member 3** · Individual marks: **15 / 65**

## Goal

Let a prosumer claim a transfer slot and change their mind within reason, while
keeping the two promises the whole project is graded hardest on: **no
reservation further than 7 days out, no change or cancellation inside 12
hours' notice.** Give the prosumer a truthful, searchable record of every
booking they have ever made.

## Scope, as agreed

**Web application** — Operators can manage, update, or cancel power trading
reservations on behalf of prosumers.

**Mobile application** — Prosumers book energy drop-off or charging slots. The
mobile interface includes dashboards to filter and search historical, pending,
and active bookings.

**Web Service & DB** — The C# "FAT Service" architecture enforces all
scheduling constraints, ensuring reservations are made within a 7-day window
and that any modifications or cancellations occur with at least 12 hours'
notice.

## Requirements owned

| Doc | Covers |
|---|---|
| `requirements/REQ-RESV-reservation-management.md` | Create, update, cancel, approve — the full state machine and both time rules |
| `requirements/REQ-SLOT-energy-slot-management.md` §4.3 (capacity accounting) | Slot capacity consumed/restored on approve/cancel — BR-15 |
| `requirements/REQ-VIEW-dashboards-views-and-maps.md` §2.1–2.2 | Prosumer dashboard counts, current/upcoming bookings, history, search, filter |

## User flows owned

`user-flows/UF-02-book-energy-slot.md` · `user-flows/UF-03-modify-booking.md` ·
`user-flows/UF-04-cancel-booking.md`

## Business rules owned

**BR-05** (7-day booking horizon) · **BR-06** (12h update notice) · **BR-07**
(12h cancel notice) · **BR-08** (no past slots) · **BR-15** (slot capacity) · **BR-16**
(referenced entities must exist and be active) · **BR-18** (QR reissue/invalidate
on update — the reservation-side trigger; D4 owns the token's format)

## Data owned

`EnergyReservation` collection · SQLite `reservation_cache` table (schema
shared with D1/D4 in the same SQLite file)

## Technical tiers this domain has code in

| Tier | What lives here |
|---|---|
| C1 Web API | `ReservationsController`, `ReservationService`, `ReservationRules`, `ReservationStateMachine` |
| C2 Web app | `features/reservations/` — operator-facing reservation table, create-on-behalf, approve/modify/cancel actions |
| C3 Android | `ui/prosumer/` — node/slot picker, kWh input, confirm, summary screen, dashboard, booking history with search and filter |

## The two rules that carry the most weight — do not conflate them

```
BR-05  slot start <= reservation.createdAt + 7 days     (anchored on creation)
BR-06  slot start -  now >= 12 hours                     (anchored on now)
BR-07  slot start -  now >= 12 hours                     (anchored on now)
```

Both boundaries are inclusive. See `backend/.../Services/ReservationRules.cs`,
already written and unit-tested at exactly these boundaries.

## Definition of done

Everything in `project-config.md` §8, plus:

- Every create, update and cancel ends on a **summary screen** before
  returning to the list (rubric: 2 marks, easy to forget under time pressure).
- Dashboard counts and history are visibly wrong the moment they are computed
  client-side instead of read from `GET /dashboard/prosumer` — this is the
  rubric's Poor-band trap ("values are hard-coded"). Run `REQ-VIEW` AC-13
  literally: inspect the code, confirm the number came from a response field.
- The exact-boundary cases (7.0 days, 12.0 hours) are demonstrated, not just
  the obviously-fine and obviously-refused cases.

## Viva questions this domain should be able to answer cold

1. Why are BR-05 and BR-06/07 anchored on different timestamps, and what goes
   wrong if you use one helper for both? → `business-rules/BR-register.md`
   "Reading the two time rules correctly"
2. What happens to a reservation's QR if it's updated after approval? → BR-18,
   `ADR-003` §5
3. Why is capacity re-checked at *approval*, not at creation? → `REQ-SLOT` EC-1
