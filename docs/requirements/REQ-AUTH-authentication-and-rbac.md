# REQ-AUTH · Authentication and Role-Based Access

Status: **Agreed** · Owner: **Member 1** · Components: C1 API, C2 Web, C3 Android
Rubric: *Web Application Features — Login and role-based access (4)* · *Mobile Authentication — Login with role-based home (2)*

---

## 1. Intent

One login endpoint serves three actors on two clients. After a successful login the user lands on the
home screen **for their role**, and every subsequent request carries a token whose role claim the API
trusts instead of anything the client says.

## 2. Actors and permissions

| Actor | Client | Lands on |
|---|---|---|
| Backoffice | Web | Backoffice dashboard (users, prosumers, nodes, pending activations) |
| Grid Operator | Web | Operator dashboard (today's reservations, slot availability) |
| Grid Operator | Android | Operator home (QR scanner, assigned node, nearby map) |
| Prosumer | Android | Prosumer home (active/pending counts, next booking, book button, map) |

## 3. Preconditions

- The account exists.
- For a prosumer: status is `Active` (BR-17).
- For a web user: `isActive` is true.

## 4. Main flow

1. User submits identifier + password. Identifier is **email** for web users and **NIC** for prosumers.
2. API looks the principal up — `Users` by email, then `Prosumers` by NIC.
3. API verifies the password against the BCrypt hash.
4. API checks account status (BR-17).
5. API issues a JWT with `sub`, `role`, `exp` (8 hours).
6. Client stores the token — Android in SQLite `session`, web in `sessionStorage`.
7. Client routes to the role's home screen.
8. Android: on next app launch, if a non-expired token is in SQLite, skip login and go straight to the
   role home.

## 5. Business rules

BR-01 (role gates admin functions) · BR-02 (own-resource access) · BR-13 (server clock) · BR-17 (status gate)

## 6. Acceptance criteria

| ID | Given | When | Then |
|---|---|---|---|
| AC-1 | A Backoffice user with valid credentials | logs in on web | They see the Backoffice dashboard, and the Operator-only menu items are absent |
| AC-2 | A Grid Operator with valid credentials | logs in on web | They see the Operator dashboard; navigating directly to `/users` is refused by the API with `E4303` even if the route is typed manually |
| AC-3 | A Prosumer with NIC + password | logs in on Android | They see the Prosumer home with their name and live active/pending counts |
| AC-4 | A Grid Operator | logs in on Android | They see the Operator home with the QR scanner entry point, **not** the prosumer booking screen |
| AC-5 | Any user | supplies a wrong password | `E4301`, generic message ("Invalid credentials") that does not reveal whether the account exists |
| AC-6 | A `Deactivated` prosumer | logs in | `E4302` with a message naming the reason and telling them to contact Backoffice |
| AC-7 | A `PendingActivation` prosumer | logs in | `E4302` with a message that the account is awaiting approval |
| AC-8 | A Prosumer who logged in yesterday, token still valid | reopens the Android app | They land on the home screen without re-entering credentials (session read from SQLite) |
| AC-9 | A Prosumer whose token expired | reopens the app | They are sent to login; the stale session row is deleted |
| AC-10 | Any client | calls a protected endpoint with no/invalid token | `401` with `E4301` |

## 7. Edge cases

| ID | Case | Expected |
|---|---|---|
| EC-1 | NIC typed with different casing/spacing (`199512345678 ` vs `199512345678`) | Trim and compare case-insensitively; same account |
| EC-2 | A prosumer NIC that is also a web user's email | Impossible — different lookup fields; document it and move on |
| EC-3 | Token's role claim is tampered with | Signature check fails → `401`. Never read role from the request body |
| EC-4 | Operator logs in on web and mobile simultaneously | Allowed. Tokens are independent; no server-side session table |
| EC-5 | Password field left empty | Client blocks; API still returns `400` with `field: "password"` |
| EC-6 | Deactivation happens while a token is still valid | Next API call re-checks status server-side and returns `E4302`. Status is checked per request, not only at login |

**EC-6 matters** — it is the difference between "role-based access" and real authorisation, and it is
an easy viva question.

## 8. Data touched

`Users` (read), `Prosumers` (read `status`, `passwordHash`), SQLite `session` (write).

## 9. API surface

`POST /auth/login` · `POST /auth/logout` · `GET /auth/me` — see `api-contract.md` §1.

## 10. UI surface

Web: `/login`, role-routed `/backoffice` and `/operator` shells.
Android: `LoginActivity`, `ProsumerHomeActivity`, `OperatorHomeActivity`.

## 11. Out of scope

Password reset, email verification, refresh tokens, OAuth, MFA, remember-me beyond token expiry.

## 12. Open questions / decisions

- Resolved: registration is gated by Backoffice approval → ADR-002 §4.
- Resolved: NIC is the prosumer login identifier → ADR-002.
