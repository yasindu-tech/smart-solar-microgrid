# REQ-VIEW · Booking Views, Dashboards, Maps and Local Persistence

Status: **Agreed** · Components: C1 API, C2 Web, C3 Android
Rubric: *Booking Views and Operational Dashboards (10)* · *Map Features — show nearby stations (5)* · *Service Integration — SQLite (3), Google Maps (3)*

**Ownership split by domain (updated 14 Sep 2026 — see `domains/README.md`).** This file stays one
document because the screens share a data model, but each subsection below belongs to a different
domain owner. Read the domain tag before you build a section.

| Subsection | Screen | Domain | Owner |
|---|---|---|---|
| §2.1 | Prosumer dashboard | **D3** Reservation & Scheduling | M3 |
| §2.2 | Prosumer bookings — current, history, search, filter | **D3** Reservation & Scheduling | M3 |
| §2.3 | Operator dashboard | **D4** Fulfillment & Verification | M4 |
| §2.4 | Backoffice dashboard | **D1** Identity & Access | M1 |
| §2.5 | Map — nearby nodes | **D2** Microgrid Inventory | M2 |

This area carries **more individual marks than any other single feature area**, and its Poor band is
literally "values are hard-coded". Every number on every screen must come from the API, whichever
domain owns the screen.

---

## 1. Intent

Each actor sees, at a glance, what matters to them — and can search and filter history. The prosumer
also sees nearby microgrid nodes on a map. The Android app renders instantly from SQLite cache and
then reconciles with the API.

## 2. Screens and what each must show

### 2.1 Prosumer dashboard (Android) — rubric: active/pending counts
| Element | Source |
|---|---|
| Count of **active** (approved, future) reservations | `GET /dashboard/prosumer` |
| Count of **pending** reservations | same |
| Next upcoming reservation card (node, date, time, status) | same |
| Quick actions: Book, My bookings, Map, Profile | — |

### 2.2 Prosumer bookings (Android) — rubric: booking history, pending booking, search booking
| View | Content |
|---|---|
| **Current / upcoming** | `Pending` + `Approved` with future slot start, soonest first |
| **History** | `Cancelled`, `Completed`, `Expired`, newest first, paged |
| **Search** | by reservation ID or node name |
| **Filter** | by status and by date range |

### 2.3 Operator dashboard (Android + Web) — rubric: pending reservations, count of approved future reservations
| Element | Source |
|---|---|
| Count of pending reservations at the operator's node(s) | `GET /dashboard/operator` |
| Count of **approved future** reservations | same |
| Today's reservation list with status | same |
| Entry point to the QR scanner (mobile) | — |

### 2.4 Backoffice dashboard (Web)
Total prosumers, pending activations count, active nodes, reservations grouped by status.

### 2.5 Map (Android) — rubric: 5 marks
- Google Maps fragment centred on the device's last known location.
- Pins for **active** nodes from `GET /nodes/nearby?lat=&lng=&radiusKm=`.
- Tapping a pin opens a bottom sheet: node name, address, capacity, battery slots, distance, and a
  **Book here** button that carries the `nodeId` into the booking flow.
- Nodes are cached in SQLite `node_cache` so the map draws pins immediately on reopen, then refreshes.

## 3. Business rules

BR-14 (cache never decides) · BR-20 (deactivated nodes absent) · BR-02 (prosumer sees only own bookings)

## 4. Acceptance criteria

| ID | Given | When | Then |
|---|---|---|---|
| AC-1 | A prosumer with 2 approved future and 1 pending reservation | opens the dashboard | Shows **2** active and **1** pending, both returned by the API |
| AC-2 | The same prosumer | cancels one approved reservation | The dashboard count drops to 1 after refresh — the count is not stored in the app |
| AC-3 | A prosumer with 12 past reservations | opens History | Paged list, newest first, all terminal statuses present |
| AC-4 | History open | prosumer searches a reservation ID | Only that reservation is listed |
| AC-5 | History open | prosumer filters status = Cancelled | Only cancelled reservations listed |
| AC-6 | An operator assigned to ND-001 | opens the operator dashboard | Counts reflect ND-001 only, not the whole system |
| AC-7 | An operator dashboard | a prosumer books at that node | After refresh, the pending count increases by 1 |
| AC-8 | Device with location permission | opens the map | Active nodes appear as pins at their stored latitude/longitude |
| AC-9 | A node is deactivated | the map is refreshed | Its pin disappears (BR-20) |
| AC-10 | A pin | is tapped | Bottom sheet shows node details from the API and a working Book here action |
| AC-11 | Airplane mode, app previously used | app reopened | Dashboard and map render from SQLite cache with a visible "offline — showing last synced data" banner; **all action buttons are disabled** |
| AC-12 | Location permission denied | map opened | Map still shows nodes, centred on a default Colombo coordinate, with a prompt to enable location |
| AC-13 | Any dashboard number | inspected in code | Comes from an API response field, not a client-side `count()` over a cached list |

## 5. Edge cases

| ID | Case | Expected |
|---|---|---|
| EC-1 | Prosumer has zero reservations | Empty states with a clear call to action, not a blank screen or a crash |
| EC-2 | 200 nodes in range | API caps `nearby` at 50 nearest; the app does not cluster pins in v1 |
| EC-3 | Node has no coordinates (bad data) | Skipped on the map, logged; the API should have rejected it at creation (`E4002`) |
| EC-4 | Cache is stale after a cancel made on the web | Any screen that loads calls the API and overwrites the cache. Cache is never authoritative (BR-14) |
| EC-5 | Google Maps API key missing or unrestricted | Map renders grey. Put key setup in the README deployment steps; do **not** commit the key |
| EC-6 | Very old cached rows | `synced_at` older than 24h → show the offline banner even when online until refresh completes |
| EC-7 | Two accounts used on one device | Clearing session on logout must also clear `profile_cache` and `reservation_cache` |

## 6. Data touched

Read: `EnergyReservation`, `SolarStationInfo`, `Prosumers`, `Users`.
SQLite: `node_cache`, `reservation_cache`, `profile_cache`.

## 7. API surface

`api-contract.md` §8 (dashboards), §4 (`/nodes/nearby`), §6 (`/reservations/me`, `/reservations`).

## 8. Out of scope

Charts/graphs, push notifications, directions/navigation to a node, marker clustering, offline actions.

## 9. Demo checklist (do this before recording the video)

- [ ] Prosumer dashboard counts change live after a booking and a cancellation
- [ ] History shows every terminal status at least once
- [ ] Search and filter both demonstrably narrow the list
- [ ] Operator dashboard counts differ from the prosumer's — proving they are scoped
- [ ] Map shows ≥3 pins, one tapped through to booking
- [ ] Airplane-mode reopen shows cached data with the offline banner (this one screenshot proves
      SQLite persistence to the marker better than any code walkthrough)
