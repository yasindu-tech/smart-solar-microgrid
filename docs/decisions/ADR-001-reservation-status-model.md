# ADR-001 · Reservation Status Model and Lazy Expiry

Status: **Accepted** · Date: 2026-09-13 · Deciders: whole group · Supersedes: —

## Context

The assignment describes creating, updating and cancelling reservations, and finalising an energy
transfer after QR verification. It does not name the statuses. Four developers writing four screens
against an unnamed status set will produce four different vocabularies, and the API will end up with
string comparisons against `"cancel"`, `"CANCELLED"` and `"Cancel"` in the same codebase.

We also needed to decide what happens to a booking nobody turns up for.

## Decision

**Five statuses, exactly these spellings:**

`Pending` · `Approved` · `Cancelled` · `Completed` · `Expired`

Transitions are defined once in `architecture/state-machines.md` §1 and enforced by a single
`AssertTransition(from, to)` helper in `ReservationService`. Any disallowed transition returns `E4105`.

**Expiry is lazy.** When a reservation in `Pending` or `Approved` is read and its `slotEndTime` has
passed, the service marks it `Expired` before returning it. There is no scheduled job.

## Alternatives considered

| Option | Why rejected |
|---|---|
| Three statuses (`Active`, `Cancelled`, `Completed`) | No way to distinguish "booked but not yet confirmed by an operator" from "confirmed with a QR issued". The rubric explicitly asks for a **pending reservations** count on the operator dashboard, so `Pending` must exist as a real state. |
| A boolean `isCancelled` + `isCompleted` | Two booleans make four combinations, two of which are nonsense. State machines beat flags. |
| A background service / hosted `IHostedService` for expiry | More moving parts to host on IIS, harder to demonstrate at a viva, and a timer that runs every minute is invisible to a marker. Lazy expiry is deterministic and provable on demand. |
| No `Expired` state — leave stale bookings `Approved` forever | The operator dashboard's "approved future reservations" count would be wrong, which is precisely what the rubric measures. |

## Consequences

**Good**
- One vocabulary across API, web, Android, database and report.
- Expiry is demoable: set a slot in the past, refresh the list, watch the status change.
- No hosting complexity on IIS.

**Bad / accepted**
- A reservation that is never read is never expired. Acceptable: nothing depends on the status of a
  record nobody looks at, and every dashboard query reads it.
- The lazy write happens inside a read path, which is mildly impure. Documented; contained to one method.

## Follow-ups
- `state-machines.md` is the normative table. This ADR only records *why*.
- If the group ever adds a "Rejected" status for approvals, it needs a new ADR, not an edit here.
