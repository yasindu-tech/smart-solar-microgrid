# D1 · Identity & Access Management

Status: **Agreed** · Owner: **Member 1** · Individual marks: **17 / 65**

## Goal

Give the system one trustworthy answer to "who is this, and what are they
allowed to do" — enforced centrally, so that a role check in a menu and a role
check in the API can never disagree. Give prosumers a self-service way in and
out of the system, with the one restriction the assignment insists on:
**only Backoffice can bring a deactivated account back.**

## Scope, as agreed

**Web application** — Backoffice administrators manage the full lifecycle of
prosumer profiles, including the exclusive authority to reactivate deactivated
accounts. Role-based access ensures Grid Operators only see operational tools.

**Mobile application** — Prosumers register using their National Identity Card
(NIC) as the primary key, manage their profiles, and request account
deactivation. The pure native Android app uses SQLite to locally persist login
and session state.

**Web Service & DB** — The C# API centralises all role validation and
authentication logic. MongoDB stores user records in dedicated collections
(`Users` for web accounts, `Prosumers` for NIC-keyed prosumer accounts).

## Requirements owned

| Doc | Covers |
|---|---|
| `requirements/REQ-AUTH-authentication-and-rbac.md` | Login, JWT issuance, role-based home screens, per-request authorisation |
| `requirements/REQ-USER-web-user-management.md` | Backoffice and Operator account CRUD |
| `requirements/REQ-PROS-prosumer-management.md` | Registration, profile edit, deactivation request, pending-activation queue, reactivation |
| `requirements/REQ-VIEW-dashboards-views-and-maps.md` §2.4 | Back-office dashboard (totals, pending activations, active nodes) |

## User flows owned

`user-flows/UF-01-prosumer-registration.md` · `user-flows/UF-06-deactivation-and-reactivation.md`

## Business rules owned

BR-01 (Backoffice-only admin) · BR-02 (own-resource access) · **BR-03**
(Backoffice-only reactivation) · BR-04 (deactivation cascade) · BR-12
(NIC identity) · BR-13 (server clock) · BR-17 (active-account gate)

## Data owned

`Users`, `Prosumers` collections · SQLite `session` table (schema shared with
D4, who owns `profile_cache`/`node_cache`/`reservation_cache` in the same file)

## Technical tiers this domain has code in

| Tier | What lives here |
|---|---|
| C1 Web API | `AuthController`, `UsersController`, `ProsumersController`, `AuthService`, `UserService`, `ProsumerService` |
| C2 Web app | `features/auth/`, `features/users/`, `features/prosumers/` — login screen, user admin, prosumer admin, pending-activation queue |
| C3 Android | `ui/auth/` — login, register, profile, deactivate |

## Definition of done

Everything in `project-config.md` §8, plus:

- A Backoffice user cannot be created by anyone but Backoffice, and cannot
  deactivate the last remaining Backoffice account.
- An Operator attempting `PATCH /prosumers/{nic}/activate` gets `E4303` —
  this is the single clearest demonstration of BR-03 and belongs in the report.
- A deactivated prosumer's *next* API call is refused, not just their next
  login — role/status is re-checked per request (`REQ-AUTH` EC-6).
- Session survives an Android app restart via SQLite, and is fully cleared
  on logout and on deactivation.

## Viva questions this domain should be able to answer cold

1. Why is NIC the Mongo `_id` rather than a generated key? → `ADR-002`
2. Walk through what happens when an Operator tries to reactivate an account.
3. Why does registration produce `PendingActivation` instead of `Active`?
