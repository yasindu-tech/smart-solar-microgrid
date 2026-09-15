# ADR-004 · What SQLite Is Allowed to Hold on Android

Status: **Accepted** · Date: 2026-09-13 · Deciders: whole group

## Context

The assignment requires a **pure native Android app with a local SQLite database for local user
management**, and simultaneously requires that all business logic sit in the central API and that the
clients be "limited to user interface functionality".

These pull in opposite directions. Left unresolved, one developer caches bookings and decides
cancellability locally (losing marks for logic in the client), while another uses no SQLite at all
(losing the 3 marks for local persistence). Both failure modes are in the rubric's Poor band:
*"Clients rarely reach the API, or hold business logic and data locally."*

## Decision

SQLite holds exactly four tables, and nothing else:

| Table | Purpose | Authoritative? |
|---|---|---|
| `session` | JWT, role, user identifier, expiry — so login survives an app restart | Yes, for *session only* |
| `profile_cache` | The signed-in prosumer's own profile | No — display only |
| `node_cache` | Node id, name, lat/long — so map pins draw instantly | No — display only |
| `reservation_cache` | Last-synced booking list for instant render | No — display only |

**The rule (BR-14):** a screen may *render* from cache, but every *action* — create, update, cancel,
verify, finalise — calls the API, and the screen re-renders from the API response. The cache is never
consulted to decide whether something is allowed.

Concretely:
- The Cancel button may be greyed out based on cached `slot_start_time`. That is a hint.
- Whether the cancellation actually happens is decided by the API (`E4102` or success). That is the rule.
- On logout, all four tables are cleared.

## Alternatives considered

| Option | Why rejected |
|---|---|
| No SQLite at all; call the API for everything | Forfeits 3 individual marks for local persistence and contradicts an explicit assignment requirement. |
| Full offline-first with a sync queue | Weeks of work, and it would require the client to reason about business rules (can this queued cancellation still be valid?) — a direct violation of the FAT-service constraint, which is worth 12 group marks. |
| SQLite as the source of truth, syncing up periodically | The clearest possible way to fail the "Client Application Build and Architecture Compliance" criterion. |
| Cache only the session, nothing else | Safe, but wastes the easy win: an airplane-mode screenshot showing cached bookings is the single most convincing evidence of SQLite persistence for the report. |

## Consequences

**Good**
- Local persistence is real and demonstrable (airplane mode → app still renders → banner explains).
- No business rule can leak into the client, because the client has nothing to decide with.
- One sentence answers the viva question "why does your app have a database if the API has one?"

**Bad / accepted**
- Cached data can be stale — a booking cancelled on the web still shows on the phone until refresh.
  Handled by refreshing on every screen load and showing a `synced_at`-based offline banner
  (REQ-VIEW EC-4, EC-6).
- Two representations of a reservation exist (cache row + API DTO). Keep the cache row deliberately
  thin — id, node, start time, status — so drift is cheap.

## Follow-ups
- Every Android developer reads this before writing their first DAO.
- Code review check: search the Android source for date arithmetic. Any comparison of
  `slot_start_time` against `now` that *blocks a request* rather than *greys a button* is a defect.
