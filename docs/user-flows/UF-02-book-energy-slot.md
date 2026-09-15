# UF-02 · Book an Energy Slot

Owner: Member 3 · Requirement: REQ-RESV §4.1 · Components: C3 Android → C1 API → C4 Mongo

---

## Actor
Prosumer, `status: Active`, authenticated.

## Preconditions
- At least one active node with a future slot inside the 7-day horizon and remaining capacity.

## Steps

| # | Where | Action | API | Rules |
|---|---|---|---|---|
| 1 | Android | Taps **Book** on home, or **Book here** from a map pin | — | — |
| 2 | Android | Picks a node (list or map) | `GET /nodes/nearby` or `GET /nodes` | BR-20 (inactive hidden) |
| 3 | Android | Picks a date within the next 7 days | `GET /nodes/{id}/slots?from=&to=` | slots beyond 7 days are not offered |
| 4 | Android | Picks a slot; full slots shown greyed with "Full" | — | presentation only |
| 5 | Android | Enters requested kWh, taps Confirm | — | — |
| 6 | API | Runs validation chain 1–7 (REQ-RESV §4.1) | `POST /reservations` | BR-17, BR-16, BR-08, **BR-05**, BR-15 |
| 7 | Mongo | Writes reservation `status: Pending`, denormalises `slotStartTime`, `createdAt = utcnow` | — | — |
| 8 | Android | **Summary screen**: node, date, time, kWh, status, reservation ID | — | rubric: 2 marks |
| 9 | Android | Updates `reservation_cache`; dashboard counts refresh from API | `GET /dashboard/prosumer` | BR-14 |
| 10 | Web/Android | Operator approves → QR issued | `PATCH /reservations/{id}/approve` | BR-15, ADR-003 |

## Success
Reservation exists in `Pending`; the prosumer's pending count increases by one; after approval, a QR
is available on the booking detail screen.

## Failure paths

| Condition | Error | Message shown |
|---|---|---|
| Slot more than 7 days out | `E4101` | "Reservations must be within 7 days." |
| Slot start already passed | `E4104` | "That slot has already started." |
| Slot full | `E4106` | "This slot is fully booked." |
| Prosumer deactivated mid-flow | `E4302` | "Your account is not active." |
| Node deactivated mid-flow | `E4404` / `E4004` | "This node is no longer available." |
| kWh above what the slot offers | `E4002` | field `requestedKwh` |

## Related
REQ-RESV · REQ-SLOT · REQ-NODE · BR-05, BR-08, BR-15, BR-16, BR-17 · UF-05
