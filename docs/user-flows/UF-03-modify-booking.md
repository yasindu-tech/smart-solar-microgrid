# UF-03 · Modify a Booking

Owner: Member 3 · Requirement: REQ-RESV §4.2 · Components: C3 Android (or C2 Web) → C1 API

---

## Actor
Prosumer (own reservation), or Grid Operator / Backoffice on their behalf.

## Preconditions
- Reservation status is `Pending` or `Approved`.
- Caller is the owner, or has role Operator/Backoffice (BR-02).
- **At least 12 hours remain before `slotStartTime`** (BR-06).

## Rule

> Updates require at least **12 hours'** notice before the slot start time.
> Measured server-side, `slotStartTime − DateTime.UtcNow >= 12h`. Boundary is inclusive.
> The **new** slot must also satisfy this, and must still fall within 7 days of the
> original `createdAt` (BR-05).

## Steps

| # | Where | Action | API | Rules |
|---|---|---|---|---|
| 1 | Client | Opens booking detail. Modify is **disabled with a hint** when <12h remain | — | presentation only |
| 2 | Client | Picks a new slot and/or new kWh | `GET /nodes/{id}/slots` | — |
| 3 | API | Checks status transition allowed | `PUT /reservations/{id}` | state machine |
| 4 | API | Checks ownership | — | BR-02 |
| 5 | API | Checks **12-hour notice on the current slot** | — | **BR-06** |
| 6 | API | Re-validates the new slot: exists, active, future, ≥12h away, within 7 days of `createdAt`, has capacity | — | BR-05, BR-08, BR-15, BR-16 |
| 7 | Mongo | Old slot `availableCapacity` +1, new slot −1, `slotStartTime` updated | — | BR-15 |
| 8 | API | If status was `Approved`: **invalidate the old QR token and issue a new one** | — | **BR-18** |
| 9 | Client | **Summary screen** showing the updated booking | — | rubric: 2 marks |

## Success
Reservation updated, capacity moved between slots, fresh QR if approved, summary displayed.

## Failure paths

| Condition | Error | Message |
|---|---|---|
| Less than 12h to current slot start | `E4103` | "Changes need at least 12 hours' notice." |
| New slot is less than 12h away | `E4103` | same |
| New slot outside the 7-day horizon | `E4101` | "Reservations must be within 7 days of booking." |
| New slot full | `E4106` | "That slot is fully booked." |
| Reservation already Cancelled/Completed/Expired | `E4105` | "This booking can no longer be changed." |
| Not the owner | `E4304` | "You can only change your own bookings." |

## Note for the viva
A Backoffice or Operator acting on the prosumer's behalf is **also** bound by the 12-hour rule.
There is no staff override. This was a deliberate group decision — see `BR-register.md` boundary table.

## Related
REQ-RESV · UF-04 · BR-06, BR-05, BR-18 · ADR-003
