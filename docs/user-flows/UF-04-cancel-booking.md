# UF-04 · Cancel a Booking

Owner: Member 3 · Requirement: REQ-RESV §4.3 · Components: C3 Android (or C2 Web) → C1 API

> This is the flow the knowledge base gets asked about most. It is written to be answerable without
> opening code.

---

## Actor
Prosumer (own booking), or Grid Operator / Backoffice on their behalf.

## Preconditions
- User is authenticated.
- The booking belongs to the prosumer (or the caller is staff) — BR-02.
- Booking status is `Pending` or `Approved` (not already terminal).

## Rule

> **Cancellation must be requested at least 12 hours before the scheduled slot start time.**
> `slotStartTime − DateTime.UtcNow >= 12 hours`, evaluated server-side in UTC.
> Exactly 12 hours is allowed. No override for Operators or Backoffice.

## Steps

| # | Where | Action | API | Rules |
|---|---|---|---|---|
| 1 | Client | Opens booking detail. Cancel is disabled with an explanatory hint when <12h remain | — | presentation only |
| 2 | Client | Taps Cancel → confirmation dialog naming the node and slot time | — | — |
| 3 | API | Status is not terminal | `PATCH /reservations/{id}/cancel` | state machine |
| 4 | API | Caller is owner or staff | — | BR-02 |
| 5 | API | **≥12 hours remain** | — | **BR-07** |
| 6 | Mongo | `status: Cancelled`, `cancelledAt`, `cancelledBy` stamped | — | — |
| 7 | Mongo | Slot `availableCapacity` restored (+1) | — | BR-15 |
| 8 | API | QR token invalidated | — | BR-18 |
| 9 | Client | **Cancellation summary screen** | — | rubric: 2 marks |
| 10 | Client | Dashboard counts re-fetched from the API | `GET /dashboard/prosumer` | BR-14 |

## Success
- Booking status → `Cancelled`
- Slot availability → restored
- QR → invalid
- Confirmation summary shown to the user
- The reservation moves out of "current" and into "history"
- Operator's pending/approved counts drop on their next refresh

## Failure

| Condition | Error | Message |
|---|---|---|
| Less than 12 hours remain | `E4102` | "Cancellation requires at least 12 hours' notice before the slot start time." |
| Already Cancelled / Completed / Expired | `E4105` | "This booking can no longer be cancelled." |
| Not the owner, not staff | `E4304` | "You can only cancel your own bookings." |
| Prosumer account deactivated | `E4302` | — (but see note) |

**Note on the last row:** a prosumer who *deactivates their own account* has their future bookings
auto-cancelled by BR-04, bypassing the 12-hour rule. That is an account action, not a booking action,
and it is deliberate. It is the one documented exception to BR-07 — be ready to say so at the viva.

## Related
- REQ-RESV (reservation management)
- REQ-VIEW (prosumer dashboard, operator dashboard — the counts that change)
- REQ-SLOT (capacity restoration)
- BR-02, BR-04, BR-07, BR-15, BR-18
- UF-03 (modify), UF-06 (deactivation)
