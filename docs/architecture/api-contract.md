# Architecture — API Contract

Status: **Agreed** · Base URL: `https://<iis-host>/api`

This is the contract between C1 and both clients. **Freeze this on Day 3.** Once frozen, the web and
Android developers can build against it in parallel without waiting for the API to be finished.
A change to this file needs a message in the group chat and an update here in the same hour.

Auth: `Authorization: Bearer <jwt>` on everything except `/auth/*` and `POST /prosumers/register`.

---

## 1. Auth

| Method | Path | Role | Body | 200 response |
|---|---|---|---|---|
| POST | `/auth/login` | — | `{ identifier, password }` (`identifier` = email for web users, NIC for prosumers) | `{ token, role, displayName, nicOrUserId, expiresAt }` |
| POST | `/auth/logout` | any | — | `204` |
| GET | `/auth/me` | any | — | current principal |

Errors: `E4301` bad credentials · `E4302` account not active (message states PendingActivation or Deactivated).

## 2. Web users (Backoffice + Operators)

| Method | Path | Role | Notes |
|---|---|---|---|
| GET | `/users?role=&page=&pageSize=` | Backoffice | list |
| GET | `/users/{userId}` | Backoffice | |
| POST | `/users` | Backoffice | `{ fullName, email, password, role, assignedNodeIds }` |
| PUT | `/users/{userId}` | Backoffice | |
| PATCH | `/users/{userId}/status` | Backoffice | `{ isActive }` |
| DELETE | `/users/{userId}` | Backoffice | soft delete = `isActive:false` |

## 3. Prosumers

| Method | Path | Role | Notes |
|---|---|---|---|
| POST | `/prosumers/register` | — (public) | `{ nic, fullName, email, phone, address, password, solarCapacityKw }` → creates `PendingActivation` |
| GET | `/prosumers?status=&search=&page=` | Backoffice, Operator | `search` matches NIC or name |
| GET | `/prosumers/pending` | Backoffice | the pending-activation queue |
| GET | `/prosumers/{nic}` | Backoffice, Operator, **self** | |
| PUT | `/prosumers/{nic}` | Backoffice, **self** | profile edit |
| PATCH | `/prosumers/{nic}/deactivate` | **self** (mobile), Backoffice | BR-04 |
| PATCH | `/prosumers/{nic}/activate` | **Backoffice only** | BR-03 — Operator gets `E4303` |

## 4. Microgrid nodes

| Method | Path | Role | Notes |
|---|---|---|---|
| GET | `/nodes?active=&page=` | any authenticated | |
| GET | `/nodes/nearby?lat=&lng=&radiusKm=` | Prosumer, Operator | active nodes only; used by the map |
| GET | `/nodes/{nodeId}` | any authenticated | |
| POST | `/nodes` | Backoffice | `{ name, location{latitude,longitude,addressLine}, capacityKwh, batterySlotCount, type, schedule[] }` |
| PUT | `/nodes/{nodeId}` | Backoffice | |
| PATCH | `/nodes/{nodeId}/schedule` | Backoffice, Operator | availability updates |
| PATCH | `/nodes/{nodeId}/deactivate` | Backoffice | **BR-09** → `E4004` if active reservations exist |
| PATCH | `/nodes/{nodeId}/activate` | Backoffice | |

## 5. Energy booking slots

| Method | Path | Role | Notes |
|---|---|---|---|
| GET | `/nodes/{nodeId}/slots?from=&to=` | any authenticated | available slots in a window |
| POST | `/nodes/{nodeId}/slots` | Backoffice, Operator | `{ startTime, endTime, totalCapacity, energyOfferedKwh }` |
| PUT | `/slots/{slotId}` | Backoffice, Operator | REQ-SLOT-5 propagation applies |
| PATCH | `/slots/{slotId}/availability` | Operator | `{ availableCapacity }` — operator's daily job |
| DELETE | `/slots/{slotId}` | Backoffice | refused if reservations reference it (`E4004`) |

## 6. Reservations

| Method | Path | Role | Notes |
|---|---|---|---|
| POST | `/reservations` | Prosumer | `{ nodeId, slotId, requestedKwh }` — NIC taken from the token, never the body |
| GET | `/reservations?status=&nodeId=&from=&to=&search=&page=` | Operator, Backoffice | operational list + filters |
| GET | `/reservations/mine?status=&page=` | Prosumer | own bookings; `status=history` → terminal states |
| GET | `/reservations/{reservationId}` | owner, Operator, Backoffice | |
| PUT | `/reservations/{reservationId}` | owner, Operator | `{ slotId, requestedKwh }` — **BR-06** |
| PATCH | `/reservations/{reservationId}/approve` | Operator, Backoffice | issues `qrToken` |
| PATCH | `/reservations/{reservationId}/cancel` | owner, Operator | **BR-07** |
| GET | `/reservations/{reservationId}/qr` | owner | returns `{ qrToken, expiresAt }` — Approved only |

## 7. Operator verification (QR)

| Method | Path | Role | Notes |
|---|---|---|---|
| POST | `/operator/verify-qr` | Operator | `{ qrToken }` → `{ valid, reservationId, prosumerName, nic, nodeId, slotStartTime, requestedKwh, status }`. **Read-only — does not change state.** |
| POST | `/operator/finalize` | Operator | `{ reservationId, deliveredKwh }` → sets `Completed`. BR-10, BR-11 |

Splitting verify from finalize means the operator sees the details and confirms before anything is
written. That two-step is what the rubric's "verifies it against the server and finalises the job"
band describes.

## 8. Dashboards

| Method | Path | Role | Returns |
|---|---|---|---|
| GET | `/dashboard/prosumer` | Prosumer | `{ activeCount, pendingCount, nextReservation, recentHistory[] }` |
| GET | `/dashboard/operator?nodeId=` | Operator | `{ pendingCount, approvedFutureCount, todaysReservations[] }` |
| GET | `/dashboard/backoffice` | Backoffice | `{ totalProsumers, pendingActivations, activeNodes, reservationsByStatus{} }` |

**All counts are computed by the API.** A count computed in the client is a hard-coded value as far as
the rubric is concerned ("One view only, or values are hard-coded" = Poor band).

---

## 9. Error code catalogue

| Code | HTTP | Meaning | Rule |
|---|---|---|---|
| `E4000` | 400 | Validation Error | |
| `E4001` | 409 | Duplicate identifier (nodeId / NIC / email) | |
| `E4002` | 400 | Invalid coordinates or capacity | |
| `E4003` | 409 | Capacity reduction below committed reservations | |
| `E4004` | 409 | Node/slot cannot be deactivated or deleted — active reservations exist | **BR-09** |
| `E4101` | 400 | Reservation date outside the 7-day window | **BR-05** |
| `E4102` | 409 | Cancellation refused — less than 12 hours to slot start | **BR-07** |
| `E4103` | 409 | Update refused — less than 12 hours to slot start | **BR-06** |
| `E4104` | 400 | Slot start time is in the past | BR-08 |
| `E4105` | 409 | Invalid status transition | state machine |
| `E4106` | 409 | Slot has no remaining capacity | BR-15 |
| `E4201` | 400 | QR token malformed or signature invalid | **BR-10** |
| `E4202` | 409 | QR token already used | BR-10 |
| `E4203` | 409 | QR token does not match an Approved reservation | BR-10 |
| `E4301` | 401 | Invalid credentials | |
| `E4302` | 403 | Account not Active (pending or deactivated) | BR-17 |
| `E4303` | 403 | Role not permitted for this operation | BR-01, BR-03, BR-11 |
| `E4304` | 403 | Not the owner of this resource | BR-02 |
| `E4404` | 404 | Referenced entity not found | BR-16 |
| `E5000` | 500 | Unhandled — must never appear in the demo | |

---

## 10. Contract-freeze checklist (Day 3)

- [ ] Every endpoint above has a controller stub returning hard-coded sample JSON
- [ ] A Postman / `.http` collection is committed at `/docs/api/SmartSolar.postman_collection.json`
- [ ] Both client developers have hit every stub once
- [ ] Base URL for the IIS host is recorded in `project-config.md`

Until the stubs exist, the web and Android developers are blocked. This is the single biggest
schedule risk in a 17-day build.
