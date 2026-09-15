# REQ-RESV · Reservation Management (create, update, cancel)

Status: **Agreed** · Owner: **Member 3** · Components: C1 API, C2 Web, C3 Android, C4 Mongo
Rubric: *Web — Slot Booking Management (5)* · *Mobile Reservation Workflow — Create (3), Update (2), Cancel (2), Summary page after each action (2)*

**This is the highest-value document in the knowledge base.** The two headline rules of the
assignment live here, and both the web and mobile rubric bands reference them.

---

## 1. Intent

A prosumer reserves an energy transfer slot at a microgrid node, may change or cancel it within the
agreed notice period, and always sees a confirmation summary of what just happened. Operators and
Backoffice can perform the same operations on a prosumer's behalf.

## 2. Actors and permissions

| Action | Prosumer (own) | Operator | Backoffice |
|---|:--:|:--:|:--:|
| Create reservation | ✅ mobile | ✅ web | ✅ web |
| Update own reservation | ✅ | ✅ | ✅ |
| Cancel own reservation | ✅ | ✅ | ✅ |
| Approve reservation | ✖ | ✅ | ✅ |
| View any reservation | own only | ✅ | ✅ |

## 3. Preconditions

- Caller authenticated; prosumer status `Active` (BR-17).
- Target node `Active`, target slot active and future (BR-16, BR-20).
- For update/cancel: caller is the owner, or has role Operator/Backoffice (BR-02).

## 4. Main flows

### 4.1 Create (REQ-RESV-1)
1. Prosumer picks a node (from map or list), then a slot from the next 7 days, enters requested kWh.
2. Client sends `POST /reservations { nodeId, slotId, requestedKwh }`. **NIC comes from the token.**
3. API validates in this order and stops at the first failure:

   | Order | Check | Rule | Error |
   |---|---|---|---|
   | 1 | Prosumer exists and is `Active` | BR-17 | `E4302` |
   | 2 | Node exists and is `Active` | BR-16, BR-20 | `E4404` |
   | 3 | Slot exists, active, belongs to that node | BR-16 | `E4404` |
   | 4 | `slot.startTime` is in the future | BR-08 | `E4104` |
   | 5 | `slot.startTime <= now + 7 days` | **BR-05** | `E4101` |
   | 6 | `requestedKwh > 0` and `<= slot.energyOfferedKwh` | — | `E4002` |
   | 7 | Slot has remaining capacity | BR-15 | `E4106` |

4. Reservation written with `status: "Pending"`, `slotStartTime` denormalised, `createdAt` = now (UTC).
5. Response returns the full reservation. **Client shows a summary screen** with node, date, time,
   kWh, status and reservation ID.

### 4.2 Update (REQ-RESV-2)
1. Prosumer opens a `Pending`/`Approved` reservation and taps Modify.
2. They may change the **slot** and the **requested kWh**. They may not change node ownership or NIC.
3. API checks, in order:
   - Status is `Pending` or `Approved` (else `E4105`)
   - Caller is owner or staff (else `E4304`)
   - **`slotStartTime − now >= 12 hours`** → BR-06, else `E4103`
   - New slot satisfies checks 3–7 above, including **BR-05 against the original `createdAt`**
4. On success: old slot capacity restored, new slot capacity taken, `slotStartTime` updated.
   If the reservation was `Approved`, the **old QR token is invalidated and a new one issued** (BR-18).
5. Client shows the updated summary screen.

### 4.3 Cancel (REQ-RESV-3)
1. Prosumer (or operator) taps Cancel, confirms in a dialog.
2. API checks: status not terminal (`E4105`), owner/staff (`E4304`),
   **`slotStartTime − now >= 12 hours`** → BR-07, else `E4102`.
3. On success: `status: "Cancelled"`, `cancelledAt`, `cancelledBy` stamped, slot capacity restored,
   QR token invalidated.
4. Client shows a cancellation summary.

### 4.4 Approve (REQ-RESV-4)
Operator/Backoffice approves a `Pending` reservation. Capacity re-checked (BR-15), status → `Approved`,
QR token generated (ADR-003) and returned. The prosumer's app shows the QR on next refresh.

## 5. Business rules

**BR-05 (7-day)** · **BR-06 (12h update)** · **BR-07 (12h cancel)** · BR-02 · BR-08 · BR-13 · BR-15 ·
BR-16 · BR-17 · BR-18

Read `business-rules/BR-register.md` §"Reading the two time rules correctly" before writing this code.
The 7-day rule is anchored on `createdAt`; the 12-hour rules are anchored on `now`.

## 6. Acceptance criteria

| ID | Given | When | Then |
|---|---|---|---|
| AC-1 | Active prosumer, slot 3 days out with capacity | creates a reservation | `201`, status `Pending`, summary screen shows node, date/time, kWh, reservation ID |
| AC-2 | A slot **8 days** out | prosumer books it | `E4101`; message states the 7-day limit; nothing written |
| AC-3 | A slot **exactly 7 days** out (to the minute) | prosumer books it | **Succeeds** — boundary is inclusive |
| AC-4 | An `Approved` reservation starting in **13 hours** | prosumer cancels | Succeeds; slot capacity restored; QR invalid |
| AC-5 | An `Approved` reservation starting in **11 hours** | prosumer cancels | `E4102`; reservation unchanged; message states the 12-hour rule |
| AC-6 | An `Approved` reservation starting in **exactly 12 hours** | prosumer cancels | **Succeeds** — boundary is inclusive |
| AC-7 | A `Pending` reservation starting in 11 hours | prosumer updates the kWh | `E4103` |
| AC-8 | A reservation starting in 30 hours | prosumer moves it to a slot 2 days out | Succeeds; old slot capacity +1, new slot −1, `slotStartTime` updated |
| AC-9 | An `Approved` reservation | prosumer updates it | A **new** QR token is returned; scanning the old token later gives `E4201`/`E4203` |
| AC-10 | A `Cancelled` reservation | anyone tries to update or cancel it | `E4105` |
| AC-11 | Prosumer A | tries to cancel prosumer B's reservation | `E4304` |
| AC-12 | An operator | cancels a prosumer's reservation 20 hours ahead | Succeeds; `cancelledBy` records the operator's userId |
| AC-13 | An operator | cancels a reservation 6 hours ahead | `E4102` — the 12-hour rule has no staff override |
| AC-14 | A full slot | prosumer books | `E4106` |
| AC-15 | Every successful create/update/cancel on mobile | completes | A summary page is shown before returning to the list (rubric: 2 marks) |

## 7. Edge cases

| ID | Case | Expected |
|---|---|---|
| EC-1 | Device clock is 3 hours ahead of the server | Rule still evaluated server-side (BR-13). The UI may briefly show a Cancel button that the API refuses — that is correct behaviour, and the error message explains it |
| EC-2 | Reservation is updated to a slot that is itself less than 12h away | Refused with `E4103` — the *new* slot must also honour the notice period |
| EC-3 | Slot start passes while the prosumer is on the confirmation screen | The API re-checks at request time; result is `E4102`/`E4105` |
| EC-4 | Prosumer books two overlapping slots at different nodes | Allowed in v1. Note as a known limitation in the report; add BR-21 if the group decides to forbid it |
| EC-5 | Reservation never completed, slot end passes | Lazily marked `Expired` on next read (`state-machines.md` §1) |
| EC-6 | Update submitted with no actual change | Succeeds, no-op, still returns the summary. Do not error |
| EC-7 | `requestedKwh` exceeds what the slot offers | `E4002` with `field: "requestedKwh"` |
| EC-8 | Prosumer is deactivated between creating and updating | `E4302` on the update |
| EC-9 | Two operators approve the same reservation at once | Second gets `E4105` (status already `Approved`) — harmless |

## 8. Data touched

`EnergyReservation` (CRUD), `EnergyBookingSlots` (capacity), `Prosumers` (status read),
`SolarStationInfo` (active read), SQLite `reservation_cache` (display only, BR-14).

## 9. API surface

`api-contract.md` §6.

## 10. UI surface

- **Android**: node picker → slot picker (next 7 days only, greyed when full) → kWh input → confirm →
  **summary screen**. Booking detail with Modify / Cancel, both disabled with an explanatory hint when
  <12h remain. Cancellation confirm dialog.
- **Web**: reservations table with status/node/date filters, create-on-behalf form, row actions
  Approve / Modify / Cancel.

## 11. Out of scope

Payments, energy pricing, partial fulfilment, recurring bookings, notifications/email/SMS, waitlists.

## 12. Open questions / decisions

- Resolved: statuses and lazy expiry → ADR-001.
- Resolved: QR reissued on update → ADR-003 §5.
- EC-4 (overlapping bookings by one prosumer) is knowingly permitted. If the viva panel asks, the
  answer is "we identified it, decided it was out of scope for the deadline, and documented it" —
  which is a far better answer than not having noticed.
