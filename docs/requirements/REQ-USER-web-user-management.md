# REQ-USER · Web User Management (Backoffice and Grid Operator accounts)

Status: **Agreed** · Owner: **Member 1** · Components: C1 API, C2 Web, C4 Mongo
Rubric: *Web Application Features — User Management (4)*

---

## 1. Intent

Backoffice creates and administers the two web-side roles. This is the only place `Backoffice` and
`Operator` accounts come into existence — there is no self-registration for web users.

## 2. Actors and permissions

| Action | Backoffice | Operator | Prosumer |
|---|:--:|:--:|:--:|
| List / view web users | ✅ | ✖ | ✖ |
| Create web user | ✅ | ✖ | ✖ |
| Edit web user | ✅ | ✖ | ✖ |
| Activate / deactivate web user | ✅ | ✖ | ✖ |
| Edit own profile | ✅ | ✅ | n/a |

## 3. Preconditions

Caller is authenticated with role `Backoffice` (BR-01).

## 4. Main flow — create a user

1. Backoffice opens **Users → Add user**.
2. Enters full name, email, password, role, and (for Operators) one or more assigned node IDs.
3. API validates: email unique, password ≥ 8 chars, role in `{Backoffice, Operator}`, every
   `assignedNodeId` exists.
4. API hashes the password, generates the next `userId`, writes the document, returns the created user
   **without** `passwordHash`.
5. The list refreshes and the new user is visible.

## 5. Business rules

BR-01 (Backoffice only) · BR-12-analogue (unique email) · NFR-2 (never return a hash).

## 6. Acceptance criteria

| ID | Given | When | Then |
|---|---|---|---|
| AC-1 | Backoffice on the users page | they create an Operator with valid data | The user appears in the list with role `Operator` and can immediately log in |
| AC-2 | An email already in use | Backoffice submits it | `E4001`, `field: "email"`, nothing written |
| AC-3 | An Operator | calls `GET /users` directly (Postman) | `E4303`, `403` — proving the rule is in the API and not the menu |
| AC-4 | Backoffice | deactivates an Operator | That Operator's next API call returns `E4302`; the record is retained, not deleted |
| AC-5 | Backoffice | creates an Operator with `assignedNodeIds: ["ND-999"]` (nonexistent) | `E4404` |
| AC-6 | Any list request | `?role=Operator&page=1&pageSize=10` | Only Operators, paged, with a total count |
| AC-7 | Backoffice | edits a user's role from Operator to Backoffice | `assignedNodeIds` is cleared; change takes effect on that user's next login |
| AC-8 | Any response containing a user | inspected | No `passwordHash` field present anywhere |

## 7. Edge cases

| ID | Case | Expected |
|---|---|---|
| EC-1 | Backoffice deactivates **themselves** | Refused with `E4303` and message "You cannot deactivate your own account" |
| EC-2 | Last remaining active Backoffice is deactivated | Refused — the system must always keep ≥1 active Backoffice |
| EC-3 | Operator's assigned node is later deactivated | The operator remains valid; the assignment stays; finalising at that node is blocked by BR-20 |
| EC-4 | Password submitted in plain text over HTTP | Deployment must serve HTTPS; note it in the report's deployment section |
| EC-5 | Deleting a user who finalised past transfers | Soft delete only (`isActive:false`) — history keeps `completedByOperatorId` resolvable |

## 8. Data touched

`Users` (CRUD), `SolarStationInfo` (read, to validate assignments).

## 9. API surface

`api-contract.md` §2 — `GET/POST/PUT /users`, `PATCH /users/{id}/status`, `DELETE /users/{id}`.

## 10. UI surface

Web: Users list (search + role filter + paging), Add/Edit user modal, status toggle with confirm.

## 11. Out of scope

Operator self-registration, per-permission ACLs beyond the two roles, audit log UI.

## 12. Open questions

None open. EC-1 and EC-2 were group decisions made on writing this doc; if either is dropped, record
why in an ADR — the viva will ask.
