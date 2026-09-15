# Business Rules Register

Status: **Agreed**

Every invariant in the system, numbered once. A requirement doc cites a `BR-*`; it never restates the
rule in its own words, because two wordings drift.

**Enforcement column is the important one.** "API" means the rule is implemented in a service class in
C1 and violating it returns the listed error code. A rule with no API enforcement is not enforced.

---

| ID | Rule | Source | Enforced in | Error | Owner |
|---|---|---|---|---|---|
| **BR-01** | Only Backoffice users may access system administration functions (user management, prosumer administration, node creation/deactivation). Grid Operators get operational tools only. | Assignment §a | API (role attribute + service check) | `E4303` | M1 |
| **BR-02** | A prosumer may read and modify only their own profile and their own reservations. | Derived | API (owner check against token `sub`) | `E4304` | M1 |
| **BR-03** | A deactivated prosumer account can be reactivated **only by a Backoffice officer**. Operators and the prosumer themselves cannot. | Assignment §a | API | `E4303` | M1 |
| **BR-04** | A prosumer may request deactivation of their own account from the mobile app. On deactivation, all their `Pending`/`Approved` future reservations are automatically `Cancelled` and slot capacity restored. | Assignment §b + derived | API | — | M1 |
| **BR-05** | A reservation's slot start time must be **within 7 days** of the reservation creation date. | Assignment §a | API | `E4101` | M3 |
| **BR-06** | A reservation may be **updated** only if at least **12 hours** remain before the slot start time. | Assignment §a | API | `E4103` | M3 |
| **BR-07** | A reservation may be **cancelled** only if at least **12 hours** remain before the slot start time. | Assignment §a | API | `E4102` | M3 |
| **BR-08** | A reservation cannot be created for a slot whose start time is in the past. | Derived | API | `E4104` | M3 |
| **BR-09** | A microgrid node **cannot be deactivated while it has active reservations** — i.e. any reservation on that node with status `Pending` or `Approved` and a future slot start time. | Assignment §a | API | `E4004` | M2 |
| **BR-10** | An energy transfer may be finalised only after the operator's scanned QR token is **verified server-side**: signature valid, not previously used, and bound to a reservation currently in `Approved` status. | Assignment §b | API | `E4201` / `E4202` / `E4203` | M4 |
| **BR-11** | An operator may finalise a transfer only at a node in their `assignedNodeIds`. | Derived | API | `E4303` | M4 |
| **BR-12** | NIC is the primary key for a prosumer. It is immutable after registration and must be unique. | Assignment §a, §b | API + Mongo unique index | `E4001` | M1 |
| **BR-13** | All time-window rules are evaluated **server-side in UTC** against the server clock. A client-supplied timestamp is never used to decide a rule. | Derived | API | — | all |
| **BR-14** | Android may render from SQLite cache, but every action (create/update/cancel/verify/finalize) must call the API and re-render from the API response. Cache is never the decision source. | Architecture (FAT service) | Code review | — | all |
| **BR-15** | A reservation may be approved only while the slot has remaining `availableCapacity`. Approval decrements it; cancellation restores it. | Derived | API | `E4106` | M3 |
| **BR-16** | A reservation must reference an existing, active prosumer, an active node and an active slot at creation time. | Derived | API | `E4404` | M3 |
| **BR-17** | Only a prosumer with status `Active` may log in, create or modify reservations. `PendingActivation` and `Deactivated` are refused. | Assignment §a + derived | API | `E4302` | M1 |
| **BR-18** | A QR token is single-use. Once a reservation reaches `Completed`, the token is invalid. Updating an `Approved` reservation invalidates the old token and issues a new one. | Derived from ADR-003 | API | `E4202` | M4 |
| **BR-19** | Reducing a node's `batterySlotCount` or a slot's `totalCapacity` below the number of already-approved future reservations is refused. | Derived | API | `E4003` | M2 |
| **BR-20** | A deactivated node does not appear in `nearby`, is not bookable, and its historical reservations remain readable. | Derived | API | — | M2 |

---

## Reading the two time rules correctly

This is the single most common misreading in this assignment, so it is written out.

```
        created                                        slot start
           │                                                │
           ├────────────  must be ≤ 7 days  ────────────────┤        BR-05
           │                                                │
                                         ├──── ≥12h ────────┤        BR-06 / BR-07
                                        now
```

- **BR-05** is measured from `reservation.createdAt` to `reservation.slotStartTime`.
  It is checked **once, at creation**, and re-checked on update if the slot changes.
- **BR-06 / BR-07** are measured from `DateTime.UtcNow` to `reservation.slotStartTime`.
  They are checked **at the moment of the update/cancel request**.

They use different anchors. Writing one helper for both is how groups lose marks.

```csharp
// Guard: the requested slot must begin within the 7-day booking horizon (BR-05).
if (slot.StartTime > reservation.CreatedAt.AddDays(7))
    throw new DomainException("E4101");

// Guard: modifications require at least 12 hours' notice before the slot begins (BR-06/BR-07).
if (reservation.SlotStartTime - DateTime.UtcNow < TimeSpan.FromHours(12))
    throw new DomainException("E4103");
```

## Boundary decisions (agreed, do not re-litigate)

| Question | Answer |
|---|---|
| Is exactly 7.0 days allowed? | **Yes.** `<=` 7 days. |
| Is exactly 12.0 hours' notice allowed? | **Yes.** `>=` 12 hours. |
| 7 days from creation or from today? | **From creation** (`createdAt`). |
| Does the 12-hour rule apply to an operator cancelling on the prosumer's behalf? | **Yes.** The rule is about the node's planning, not who clicks. |
| Does the 12-hour rule apply to Backoffice? | **Yes.** No admin override — simpler to explain at the viva, and the assignment states no exception. |
| Does cancelling restore slot capacity? | **Yes**, immediately (BR-15). |
