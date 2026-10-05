# Architecture — Data Model

Status: **Agreed** · Owner: Member 1 (schema), all members (their own fields)

The rubric names four collections and awards 4 marks for "all four collections created with every
required field". Field names below are **binding** — do not rename them in code without an ADR.

---

## 1. MongoDB collections (C4)

### 1.1 `Users` — web/back-office accounts

```jsonc
{
  "_id": ObjectId,
  "userId": "USR-0001",          // human-readable, unique
  "fullName": "Nimal Perera",
  "email": "nimal@grid.lk",      // unique, login identifier
  "passwordHash": "…",           // BCrypt. never returned by any endpoint
  "role": "Backoffice",          // Backoffice | Operator
  "assignedNodeIds": ["ND-003"], // operators only; [] for backoffice
  "isActive": true,
  "createdAt": ISODate,
  "updatedAt": ISODate
}
```

Indexes: unique on `email`, unique on `userId`.

### 1.2 `Prosumers` — solar prosumer accounts (NIC-keyed)

> Naming note: the rubric calls this "User's detail". We use the collection name `Prosumers` because
> `Users` is already taken by web accounts. Recorded in ADR-002.

```jsonc
{
  "_id": "199512345678",         // ← NIC IS the _id. See ADR-002.
  "nic": "199512345678",         // duplicated for query clarity
  "fullName": "Kamal Silva",
  "email": "kamal@mail.com",
  "phone": "0771234567",
  "address": "12 Galle Rd, Colombo 03",
  "passwordHash": "…",
  "solarCapacityKw": 5.5,        // declared array size
  "status": "Active",            // Active | PendingActivation | Deactivated
  "deactivationRequestedAt": ISODate | null,
  "reactivatedBy": "USR-0001" | null,
  "createdAt": ISODate,
  "updatedAt": ISODate
}
```

Indexes: unique on `nic`, index on `status`.

### 1.3 `SolarStationInfo` — microgrid nodes

```jsonc
{
  "_id": ObjectId,
  "nodeId": "ND-003",            // unique, human-readable
  "name": "Moratuwa Microgrid Hub A",
  "location": {
    "latitude": 6.7729,
    "longitude": 79.8816,
    "addressLine": "Katubedda, Moratuwa"
  },
  "capacityKwh": 250.0,          // total node capacity
  "batterySlotCount": 8,         // available battery storage slots
  "type": "AC",                  // AC | DC  (kept for reporting)
  "schedule": [                  // operating windows, Asia/Colombo wall-clock (NOT UTC)
    { "dayOfWeek": "Mon", "openTime": "06:00", "closeTime": "18:00" }
  ],
  "isActive": true,
  "createdBy": "USR-0001",
  "createdAt": ISODate,
  "updatedAt": ISODate
}
```

Indexes: unique on `nodeId`, index on `isActive`, 2d index on `location` (optional — nearby search
can be done with a bounding-box query if a geo index is not set up).

### 1.4 `EnergyBookingSlots` — bookable windows at a node

```jsonc
{
  "_id": ObjectId,
  "slotId": "SL-0042",
  "nodeId": "ND-003",            // reference → SolarStationInfo.nodeId
  "startTime": ISODate,          // UTC
  "endTime": ISODate,            // UTC
  "totalCapacity": 4,            // how many reservations this slot can hold
  "availableCapacity": 3,        // decremented on approval, restored on cancel
  "energyOfferedKwh": 20.0,
  "isActive": true,
  "createdAt": ISODate,
  "updatedAt": ISODate
}
```

Indexes: unique on `slotId`, compound index on `{ nodeId: 1, startTime: 1 }`.

### 1.5 `EnergyReservation` — the prosumer's claim

```jsonc
{
  "_id": ObjectId,
  "reservationId": "RS-0107",
  "nic": "199512345678",         // reference → Prosumers.nic
  "nodeId": "ND-003",
  "slotId": "SL-0042",
  "slotStartTime": ISODate,      // denormalised from the slot — all time rules read THIS
  "slotEndTime": ISODate,
  "requestedKwh": 15.0,
  "status": "Approved",          // Pending | Approved | Cancelled | Completed | Expired
  "qrToken": "eyJ…",             // present only when status = Approved. See ADR-003.
  "qrIssuedAt": ISODate | null,
  "createdAt": ISODate,          // the "booking date" — 7-day rule measured from here
  "updatedAt": ISODate,
  "cancelledAt": ISODate | null,
  "cancelledBy": "199512345678" | "USR-0004" | null,
  "completedAt": ISODate | null,
  "completedByOperatorId": "USR-0004" | null,
  "deliveredKwh": 14.2 | null
}
```

Indexes: unique on `reservationId`, compound `{ nic: 1, status: 1 }`, compound `{ nodeId: 1, slotStartTime: 1 }`.

---

## 2. Relationships

```
Prosumers.nic  ──1:N──▶  EnergyReservation.nic
SolarStationInfo.nodeId ──1:N──▶ EnergyBookingSlots.nodeId
EnergyBookingSlots.slotId ──1:N──▶ EnergyReservation.slotId
Users.assignedNodeIds ──N:M──▶ SolarStationInfo.nodeId
```

MongoDB does not enforce these. **The service layer must**: creating a reservation validates that the
NIC, nodeId and slotId all exist and are active (BR-16).

### Why `slotStartTime` is denormalised onto the reservation

Every time-window rule (7-day, 12-hour) reads the slot's start time. Copying it onto the reservation
means the rule check is a single-document read and cannot be broken by a later slot edit. If an
operator edits a slot's time, the API must propagate the new time to affected reservations and notify
them — see REQ-SLOT-5.

---

## 3. SQLite on Android (C3)

**Local persistence only.** Authoritative state always comes from the API.

```sql
-- the signed-in user, so the app survives a restart
CREATE TABLE session (
  id              INTEGER PRIMARY KEY CHECK (id = 1),
  nic_or_user_id  TEXT NOT NULL,
  role            TEXT NOT NULL,         -- Prosumer | Operator
  jwt             TEXT NOT NULL,
  expires_at      INTEGER NOT NULL,
  full_name       TEXT
);

-- own profile cache, so the profile screen renders offline
CREATE TABLE profile_cache (
  nic          TEXT PRIMARY KEY,
  full_name    TEXT,
  email        TEXT,
  phone        TEXT,
  address      TEXT,
  status       TEXT,
  synced_at    INTEGER
);

-- reference data for the map, refreshed on each dashboard load
CREATE TABLE node_cache (
  node_id      TEXT PRIMARY KEY,
  name         TEXT,
  latitude     REAL,
  longitude    REAL,
  is_active    INTEGER,
  synced_at    INTEGER
);

-- last-known booking list, for instant render before the API responds
CREATE TABLE reservation_cache (
  reservation_id   TEXT PRIMARY KEY,
  node_id          TEXT,
  slot_start_time  INTEGER,
  status           TEXT,
  synced_at        INTEGER
);
```

**Hard rule (BR-14):** a screen may render from cache, but any *action* (create, update, cancel,
verify) must call the API and must re-render from the API response. Never decide from cache.

---

## 4. Seed data required for the demo

The rubric's Excellent band asks for "sample data added manually or through the application".
Minimum seed for a credible demo:

| Collection | Count | Notes |
|---|---|---|
| `Users` | 3 | 1 Backoffice, 2 Operators (one assigned to ND-001, one to ND-002) |
| `Prosumers` | 4 | 2 Active, 1 PendingActivation, 1 Deactivated — so every status is demoable |
| `SolarStationInfo` | 3 | Spread across real Colombo/Moratuwa coordinates so the map looks right |
| `EnergyBookingSlots` | ~12 | Across the next 7 days; include one slot starting <12h from now to demo BR-07 rejection |
| `EnergyReservation` | 5 | 1 Pending, 1 Approved (with QR), 1 Cancelled, 1 Completed, 1 in the past |

Keep the seed script in `/seed/seed.js` (mongosh) and reference it in the README — the rubric's
Excellent documentation band asks for reproducible hosting and deployment steps.
