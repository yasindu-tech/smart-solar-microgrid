# REQ-SLOT · Energy Booking Slot Management

Status: **Agreed** · Owner: **Member 2** (creation/admin) with **Member 3** (capacity semantics)
Components: C1 API, C2 Web, C4 Mongo
Rubric: *Web Application Features — Slot Booking Management (5)* · *Grid Operator — update battery slot availability*

---

## 1. Intent

A node's bookable time windows. A slot is what a prosumer actually reserves; the node is only the
place. Operators keep availability accurate day to day; Backoffice sets the windows up.

## 2. Actors and permissions

| Action | Backoffice | Operator | Prosumer |
|---|:--:|:--:|:--:|
| Create slot | ✅ | ✅ (own nodes) | ✖ |
| Edit slot times / capacity | ✅ | ✅ (own nodes) | ✖ |
| Update `availableCapacity` | ✅ | ✅ | ✖ |
| Delete slot | ✅ | ✖ | ✖ |
| View slots | ✅ | ✅ | ✅ (active, future only) |

## 3. Preconditions

Parent node exists and is `Active`. Operator must have the node in `assignedNodeIds` (BR-11 analogue).

## 4. Main flows

### 4.1 Create slots
1. Operator/Backoffice picks a node and a date, enters start, end, total capacity, energy offered kWh.
2. API validates: `startTime < endTime`, start is in the future, the window falls inside the node's
   schedule for that day, and it does not overlap an existing slot at the same node.
3. Slot written with `availableCapacity = totalCapacity`.

### 4.2 Capacity accounting — the invariant
```
availableCapacity = totalCapacity − count(reservations on this slot with status ∈ {Pending, Approved})
```
This must hold after **every** operation. It is decremented on approve and restored on cancel/expire
(BR-15). Never recompute it in a client.

> Implementation note: the safest version is to **recompute from the reservation count** inside the
> same service call rather than incrementing a counter. Counter drift is the classic bug here and it
> will show up in the demo as a slot that looks full when it is not.

### 4.3 Edit a slot's time (propagation)
If a slot's `startTime` changes, every reservation referencing it must have `slotStartTime` updated,
because all time rules read the denormalised copy (`data-model.md` §2). The API does this in the same
operation and returns the affected reservation IDs.

## 5. Business rules

BR-15 · BR-19 · BR-08 · BR-20

## 6. Acceptance criteria

| ID | Given | When | Then |
|---|---|---|---|
| AC-1 | An active node | Operator creates a slot tomorrow 09:00–11:00, capacity 4 | Slot created, `availableCapacity` = 4, visible to prosumers |
| AC-2 | An existing slot 09:00–11:00 | Operator creates 10:00–12:00 at the same node | Refused, `E4002`, message names the overlapping slot |
| AC-3 | A slot with 4 capacity and 4 approved reservations | a prosumer tries to book | `E4106` |
| AC-4 | That same slot | one reservation is cancelled | `availableCapacity` becomes 1 and the next booking succeeds |
| AC-5 | A slot with 3 approved reservations | Operator sets `totalCapacity` to 2 | `E4003` (BR-19) |
| AC-6 | A slot whose start time is moved 2 hours later | saved | Every referencing reservation's `slotStartTime` is updated and the response lists their IDs |
| AC-7 | A slot outside the node's opening hours for that day | created | Refused with `E4002`. Opening hours are Asia/Colombo wall-clock, so the slot's UTC instants are converted to local time before the day-of-week and hour comparison — see `glossary.md` § Time vocabulary |
| AC-8 | A slot with reservations | Backoffice deletes it | `E4004` — refused |
| AC-9 | A prosumer | requests slots for a node | Only future, active slots with `availableCapacity > 0` are returned |
| AC-10 | A slot start time in the past | created | `E4104` |

## 7. Edge cases

| ID | Case | Expected |
|---|---|---|
| EC-1 | Two prosumers book the last capacity unit simultaneously | Both `Pending` is acceptable; capacity is enforced at **approval** (BR-15). Second approval gets `E4106`. Documented deliberately — avoids needing transactions in MongoDB |
| EC-2 | A slot's time is moved so a reservation now has <12h notice | Allowed — the node moved it, not the prosumer. The prosumer keeps their booking; `slotStartTime` is updated |
| EC-3 | A slot is moved so a reservation now violates the 7-day horizon | Allowed. BR-05 is a *creation-time* rule (see BR-register) |
| EC-4 | Operator updates availability on a node not assigned to them | `E4303` |
| EC-5 | Slot spanning midnight | Allowed if the node's schedule permits; store UTC instants, not times-of-day |
| EC-6 | `availableCapacity` set manually to a value inconsistent with reservations | Refused — the endpoint accepts a *total* change, and available is always derived |

## 8. Data touched

`EnergyBookingSlots` (CRUD), `SolarStationInfo` (read for schedule validation), `EnergyReservation`
(read for capacity, write for propagation).

## 9. API surface

`api-contract.md` §5.

## 10. UI surface

Web: per-node slot calendar/list for the next 7 days, Add slot form, bulk "generate slots for a day"
convenience action, availability inline editor. Android: read-only slot picker in the booking flow.

## 11. Out of scope

Recurring slot templates, dynamic pricing, waiting lists, auto-generating slots from the schedule
(a manual "generate for this day" button is enough).

## 12. Open questions

- EC-1's "approve-time capacity check" is a real design decision. If the group prefers create-time
  enforcement instead, write it up as an ADR before Day 6 — after that, changing it ripples into
  REQ-RESV and the state machine.
