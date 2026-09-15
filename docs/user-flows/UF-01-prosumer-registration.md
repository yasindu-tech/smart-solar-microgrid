# UF-01 · Prosumer Registration

Owner: Member 1 · Requirement: REQ-PROS · Components: C3 Android → C1 API → C4 Mongo → C2 Web

---

## Actor
Prosumer (unauthenticated), then Backoffice.

## Preconditions
- The NIC is not already registered.
- The Android app is installed; no session in SQLite.

## Steps

| # | Where | Action | API | Rules |
|---|---|---|---|---|
| 1 | Android | Taps "Create account" on login | — | — |
| 2 | Android | Enters NIC, name, email, phone, address, solar capacity, password | — | client-side format hints only |
| 3 | API | Validates NIC format and uniqueness | `POST /prosumers/register` | BR-12 |
| 4 | Mongo | Writes `Prosumers` doc, `status: PendingActivation`, `_id = nic` | — | ADR-002 |
| 5 | Android | Shows "Awaiting Backoffice approval", returns to login | — | — |
| 6 | Web | Backoffice opens Prosumers → Pending activations | `GET /prosumers/pending` | BR-01 |
| 7 | Web | Clicks Approve | `PATCH /prosumers/{nic}/activate` | BR-01, BR-03 |
| 8 | Mongo | `status: Active` | — | — |
| 9 | Android | Prosumer logs in with NIC + password | `POST /auth/login` | BR-17 |

## Success
Prosumer reaches the Prosumer home screen. Session row written to SQLite.

## Failure paths

| Where | Condition | Result |
|---|---|---|
| 3 | NIC already exists | `E4001`, field `nic`, form stays filled |
| 3 | NIC format invalid | `400`, field `nic` |
| 9 | Not yet approved | `E4302` "Your account is awaiting approval" |
| 9 | Wrong password | `E4301` generic |

## Related
REQ-PROS · REQ-AUTH · ADR-002 · BR-12, BR-17
