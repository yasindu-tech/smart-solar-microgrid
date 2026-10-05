# Glossary

One agreed word per concept. If two documents, two screens or two developers use different words for
the same thing, the group loses time and the report reads as if four people wrote it — because they did.

| Term | Means | Do **not** say |
|---|---|---|
| **Prosumer** | A property owner with a solar array who both produces and consumes energy. Identified by NIC. Mobile-only actor. | customer, user, owner, EV owner |
| **Backoffice** | Web-only administrative role. Full system administration. | admin, super admin, manager |
| **Grid Operator** | Operational role, present on web and mobile. Runs the site, scans QR codes, finalises transfers. | operator only when unambiguous; never "station manager" |
| **Microgrid node** | A physical solar grid hub with GPS location, capacity and battery storage slots. Collection: `SolarStationInfo`. | station, hub, site, charger |
| **Energy booking slot** | A bookable time window at a node, with a capacity. Collection: `EnergyBookingSlots`. | timeslot, schedule entry |
| **Reservation** | A prosumer's claim on a slot at a node for a time window. Collection: `EnergyReservation`. | booking is acceptable as an informal synonym in UI copy; in docs and code use **reservation** |
| **Energy transfer** | The physical act of pushing/pulling energy at the node, finalised by an operator after QR verification. | charging, session, delivery |
| **Transaction QR code** | The signed token a prosumer presents; scanned by an operator and verified **server-side**. | QR, barcode, ticket |
| **Deactivated account** | A prosumer account switched off. Only Backoffice can reactivate it. | disabled, suspended, deleted |
| **Pending activation** | A prosumer account that exists but is awaiting Backoffice action (covers both new-registration approval and reactivation requests, if the group chooses to gate registration). | unapproved |
| **Slot start time** | The scheduled beginning of the reserved window. All time-window rules (7-day, 12-hour) are measured against this. | booking time, reservation time — those mean *when the reservation was created* |
| **Booking date / created-at** | When the reservation record was created. | — |
| **Active reservation** | A reservation in status `Pending` or `Approved` whose slot start time is in the future. Used by BR-09 (node deactivation block). | open booking |

## Time vocabulary

All server-side timestamps are stored **UTC**. All UI displays **Asia/Colombo (UTC+05:30)**.
A rule expressed in hours is evaluated server-side in UTC against `DateTime.UtcNow`.

Never compare a client-supplied "now" against a rule. The client's clock is not trusted — see BR-13.

**The one exception: a node's `schedule[].openTime` / `closeTime` are wall-clock
Asia/Colombo times, not UTC.** They describe when a physical station is open to
people, so "06:00" means 06:00 in Sri Lanka. Any rule that compares a stored UTC
instant against one of them must convert the instant first — `SriLankaTime.FromUtc`
in `backend/src/SmartSolar.Api/Services/`. Comparing the two frames directly shifts
every window by 5½ hours; that was a live defect in `ValidateWindowAsync` (AC-7),
where a 09:00 local slot was refused as "outside opening hours" and a 19:00 one was
accepted an hour after close. A rule measured purely in elapsed hours between two
instants — BR-05, BR-06, BR-07 — stays in UTC and needs no conversion.
