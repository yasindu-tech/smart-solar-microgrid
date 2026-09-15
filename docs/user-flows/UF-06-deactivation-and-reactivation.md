# UF-06 · Account Deactivation and Reactivation

Owner: Member 1 · Requirement: REQ-PROS §4.4–4.5 · Components: C3 Android → C1 API → C2 Web

---

## Actors
Prosumer (requests deactivation), Backoffice (sole reactivator — BR-03).

## Rule

> A prosumer may deactivate their own account from the mobile app.
> **Only a Backoffice officer can reactivate it.** Not the prosumer, not a Grid Operator.

## Part A — Deactivation (prosumer, Android)

| # | Where | Action | API | Rules |
|---|---|---|---|---|
| 1 | Android | Profile → Deactivate account | — | — |
| 2 | Android | Confirmation dialog stating: bookings will be cancelled, and only Backoffice can restore the account | — | — |
| 3 | API | Verifies caller is the account owner | `PATCH /prosumers/{nic}/deactivate` | BR-02 |
| 4 | Mongo | `status: Deactivated`, `deactivationRequestedAt` stamped | — | — |
| 5 | API | **Cancels every `Pending`/`Approved` reservation with a future slot**, restores each slot's capacity, invalidates their QR tokens | — | **BR-04**, BR-15, BR-18 |
| 6 | Android | Clears SQLite `session`, `profile_cache`, `reservation_cache`; returns to login | — | BR-14 |

**Deliberate exception:** step 5 bypasses the 12-hour rule (BR-07). Deactivating an account is an
account-level action and must not be blocked by a booking that starts in 3 hours. Documented in
REQ-PROS EC-2 and UF-04.

## Part B — Reactivation (Backoffice, Web)

| # | Where | Action | API | Rules |
|---|---|---|---|---|
| 1 | Web | Prosumers list → filter status `Deactivated` | `GET /prosumers?status=Deactivated` | BR-01 |
| 2 | Web | Open the prosumer, click Reactivate | `PATCH /prosumers/{nic}/activate` | **BR-03** |
| 3 | API | Verifies role is `Backoffice` — an Operator gets `E4303` | — | BR-03 |
| 4 | Mongo | `status: Active`, `reactivatedBy` = Backoffice `userId` | — | — |
| 5 | Android | Prosumer can log in again | `POST /auth/login` | BR-17 |

**Cancelled reservations are not restored.** The prosumer books again. Restoring them would mean
re-taking slot capacity that other prosumers may have since claimed.

## Failure paths

| Who | Action | Result |
|---|---|---|
| Grid Operator | attempts reactivation | `E4303` — this is the single clearest demo of BR-03; **screenshot it for the report** |
| Prosumer | attempts to reactivate themselves | Cannot log in at all (`E4302`), so no route exists |
| Deactivated prosumer | re-registers with the same NIC | `E4001` — reactivation is the only path (REQ-PROS EC-5) |
| Prosumer | deactivates twice | Second call is a no-op, `200` |

## Related
REQ-PROS · REQ-AUTH · UF-04 · BR-01, BR-02, BR-03, BR-04, BR-17
