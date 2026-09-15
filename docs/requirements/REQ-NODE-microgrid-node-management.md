# REQ-NODE · Microgrid Node Management

Status: **Agreed** · Owner: **Member 2** · Components: C1 API, C2 Web, C4 Mongo
Rubric: *Web Application Features — Microgrid Node Management (5)* · feeds *Map Features (5)*

---

## 1. Intent

Backoffice registers solar grid hubs with their GPS location, capacity and battery storage slots, keeps
their operating schedules current, and deactivates them — but **never while energy is still booked
there**. This is the data the mobile map and the whole booking flow depend on.

## 2. Actors and permissions

| Action | Backoffice | Operator | Prosumer |
|---|:--:|:--:|:--:|
| Create node | ✅ | ✖ | ✖ |
| Edit node details (name, capacity, battery slots, location) | ✅ | ✖ | ✖ |
| Update schedule / availability | ✅ | ✅ | ✖ |
| Deactivate / reactivate node | ✅ | ✖ | ✖ |
| View node list and detail | ✅ | ✅ | ✅ (active only) |

## 3. Preconditions

Caller authenticated. Create/deactivate requires role `Backoffice` (BR-01).

## 4. Main flows

### 4.1 Create a node
1. Backoffice opens **Nodes → Add node**.
2. Enters name, latitude, longitude, address line, total capacity (kW/h), battery slot count, type,
   and a weekly schedule (day, open time, close time).
3. API validates: `nodeId` unique, latitude ∈ [-90, 90], longitude ∈ [-180, 180], capacity > 0,
   battery slots ≥ 1, every schedule entry has open < close.
4. Node written with `isActive: true`. It is now visible on the mobile map.

### 4.2 Update schedule / availability
Backoffice or the assigned Operator edits operating windows. Existing reservations outside the new
window are **not** auto-cancelled; the API returns a warning list of affected reservation IDs so the
operator can contact those prosumers (see EC-3).

### 4.3 Deactivate a node — the guarded one
1. Backoffice clicks Deactivate.
2. API counts reservations on this node with status `Pending` or `Approved` **and** `slotStartTime` in
   the future.
3. If count > 0 → **refuse** with `E4004` and return the blocking reservation IDs and count (BR-09).
4. If count = 0 → `isActive: false`. The node disappears from `nearby`, becomes unbookable (BR-20),
   and its historical reservations remain readable.

## 5. Business rules

BR-01 · **BR-09** · BR-19 · BR-20

## 6. Acceptance criteria

| ID | Given | When | Then |
|---|---|---|---|
| AC-1 | Backoffice | creates a node with valid Colombo coordinates | Node appears in the list and, within one refresh, as a pin on the Android map |
| AC-2 | Latitude `95.0` | submitted | `E4002`, `field: "latitude"`, nothing written |
| AC-3 | A node with **one** `Approved` future reservation | Backoffice deactivates it | `E4004`; the response names the blocking reservation; the node stays `Active` |
| AC-4 | A node whose only reservations are `Cancelled` and `Completed` | Backoffice deactivates it | Succeeds; `isActive:false` |
| AC-5 | A node with a `Pending` reservation that starts **in the past** | Backoffice deactivates it | Succeeds — past reservations are not "active" (see glossary) |
| AC-6 | A deactivated node | a prosumer calls `/nodes/nearby` | The node is absent from the result |
| AC-7 | A deactivated node | a prosumer attempts to book a slot there | `E4404` / `E4004` — booking refused |
| AC-8 | An Operator | attempts `PATCH /nodes/{id}/deactivate` | `E4303` |
| AC-9 | A node with 4 approved future reservations | Backoffice sets `batterySlotCount` to 2 | `E4003` (BR-19) |
| AC-10 | Backoffice | reactivates a deactivated node | It reappears in `nearby` and is bookable again |

## 7. Edge cases

| ID | Case | Expected |
|---|---|---|
| EC-1 | Two nodes at identical coordinates | Allowed; map shows overlapping pins. Not worth solving in 17 days |
| EC-2 | Schedule with overlapping windows on the same day | Refused with `E4002` — ambiguous availability |
| EC-3 | Schedule narrowed so an existing reservation now falls outside opening hours | Save succeeds, response includes `affectedReservationIds`; web shows a warning banner listing them. **No auto-cancel** — an auto-cancel would silently break the 12-hour promise |
| EC-4 | Node deleted rather than deactivated | There is no hard delete. `DELETE` is not exposed for nodes |
| EC-5 | Coordinates entered as strings with commas (`6,7729`) | Client normalises; API rejects non-numeric with `E4002` |
| EC-6 | Deactivation attempted concurrently with a new booking | The booking either lands first (then deactivation is refused) or after (then booking is refused because the node is inactive). Both orders are correct; no locking needed |

## 8. Data touched

`SolarStationInfo` (CRUD), `EnergyReservation` (read, for the BR-09 count), `EnergyBookingSlots` (read).

## 9. API surface

`api-contract.md` §4.

## 10. UI surface

Web: Nodes list (active filter, search), Add/Edit node form with a lat/long input, Schedule editor,
Deactivate with a confirmation that surfaces the `E4004` blocking list when refused.
Android: read-only — nodes appear as map pins and in the booking picker (see REQ-VIEW).

## 11. Out of scope

Map-based coordinate picking in the web form (type the numbers), node photos, per-node pricing,
real-time telemetry from the hardware.

## 12. Open questions

- Should an Operator be able to edit capacity, not just schedule? **Decided: no** — capacity is a
  physical property, Backoffice owns it. Keeps BR-01 clean.
