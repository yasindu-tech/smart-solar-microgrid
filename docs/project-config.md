# Project Config

This is the file the `requirements-analyst` skill (`.claude/skills/requirements-analyst/SKILL.md`) reads
first. It is the set of project facts that make the skill's generic method concrete. Change this file
when the project changes; do not change the skill.

---

## 1. Project identity

| Field | Value |
|---|---|
| Project | Smart Solar Microgrid Trading System |
| Module | SE4040 Enterprise Application Development, Year 4 Semester 2, 2026 |
| Team size | 4 |
| Deadline | 30 September 2026, 23:59 |
| Planning horizon | 17 days (hard deadline), no phase 2 |
| Weight | 20% of module — 35 group marks, 65 individual marks |
| Repo | GitHub, feature branches per member, meaningful commits required |

## 2. Technical tiers (exactly four — every requirement must state which it touches)

> Called "components" (C1–C4) elsewhere in this doc set, but do not confuse
> them with the **business domains** (D1–D4, §2a) — a domain has code in
> several of these tiers. See `domains/README.md`.

| # | Tier | Technology | Responsibility |
|---|---|---|---|
| C1 | **Web API** | C# ASP.NET Web API, hosted on **Windows IIS** | *All* business logic, validation, authorisation, rule enforcement |
| C2 | **Web application** | React.js / Bootstrap 5 / Tailwind CSS | UI only, for Backoffice + Grid Operator |
| C3 | **Android application** | Pure native Android (Java/Kotlin), **no cross-platform frameworks**, local SQLite | UI + local session/reference persistence, for Prosumer + Grid Operator |
| C4 | **MongoDB** | Server-side NoSQL, 4 collections | System of record |

## 2a. Business domains (exactly four — one per member, final split agreed 14 Sep 2026)

| # | Domain | Owner | Individual marks | Doc |
|---|---|---|---|---|
| D1 | Identity & Access Management | M1 | 17 / 65 | `domains/DOM-01-identity-and-access-management.md` |
| D2 | Microgrid Inventory Management | M2 | 20 / 65 | `domains/DOM-02-microgrid-inventory-management.md` |
| D3 | Reservation & Scheduling Engine | M3 | 15 / 65 | `domains/DOM-03-reservation-and-scheduling-engine.md` |
| D4 | Fulfillment & Operational Verification | M4 | 13 / 65 | `domains/DOM-04-fulfillment-and-operational-verification.md` |

Each domain has a Web (C2), Mobile (C3) and Service & DB (C1/C4) piece — see
`domains/README.md` for the full cross-tier map and the exact hand-off points
between domains.

### Architectural constraint — "FAT service"

> No business rule may be implemented in C2 or C3. Clients may do *presentational* validation
> (disable a button, show a hint), but the API must re-validate and is the only place a rule is
> *enforced*. A rule enforced only in the client is a defect, and the rubric penalises it explicitly
> ("Significant business logic held in the clients" = Poor band).

SQLite (C3) may hold: logged-in user session, own profile cache, cached node/slot reference data,
queued-for-display booking summaries. It may **not** hold the authoritative booking state.

## 3. Actors

| Actor | Clients | Summary |
|---|---|---|
| **Backoffice** | Web only | System administration: manages users, prosumers, nodes. Sole authority to reactivate a deactivated prosumer. |
| **Grid Operator** | Web **and** Android | Operations: slot availability, booking monitoring, QR scan and energy-transfer finalisation. |
| **Prosumer** | Android only | Self-service: registers with NIC, manages own profile, books/modifies/cancels energy slots, views history, holds the QR code. |

## 4. Domain objects

| Object | Identity | Notes |
|---|---|---|
| User | `userId` (system) | Backoffice / Grid Operator web accounts |
| Prosumer | **NIC** (natural key) | Property owner with a solar array |
| Microgrid node (solar station) | `nodeId` | GPS location, capacity kW/h, battery storage slots, schedule |
| Energy booking slot | `slotId` | A bookable window at a node |
| Energy reservation | `reservationId` | A prosumer's claim on a slot |

## 5. Hard business constraints (full list in `business-rules/BR-register.md`)

- NIC is the prosumer primary key.
- Reservation must be scheduled **within 7 days** of the booking date.
- Update and cancellation require **at least 12 hours'** notice before slot start.
- A node **cannot be deactivated** while it has active reservations.
- A deactivated prosumer account can be **reactivated only by Backoffice**.
- Approved reservation produces a **secure QR code**; the operator scans it and the **server**
  verifies it before the transfer is finalised.

## 6. Mandatory integrations

- Google Maps API (Android) — nearby nodes plotted from stored lat/long.
- QR code generation (Android, prosumer side) and scanning (Android, operator side).
- SQLite (Android local persistence).

## 7. Non-negotiable submission constraints

- Comment header block on **every** `.cs` file; inline comment at the start of **every** method.
- Report contains source code **as text**, not screenshots.
- README with repo link, per-member contribution, and a ≤5 minute demo video link.
- Zip named with an IT number.
- Viva is compulsory; absence = unmarked.
- **AI Assessment Scale Level 2 (AI Planning).** AI may be used for planning, brainstorming and
  research only. All implementation code must be written by the team. AI tools used in planning must
  be disclosed in the individual contribution section.

## 8. Definition of Done (applies to every requirement)

A requirement is **Built** only when all of the following are true:

1. The API enforces every business rule listed in the requirement, and returns the documented error
   code on violation.
2. Both relevant clients call the API for the behaviour — no rule duplicated as the only enforcement.
3. Every acceptance criterion in the requirement doc passes on a manual run-through.
4. Every edge case in the requirement doc has been executed at least once and behaves as documented.
5. Screenshots captured for the report.
6. Commits reference the requirement ID.
7. The owner can explain the behaviour and the rule behind it without reading the code.

## 9. Hosting — fill this in once the VM exists

Full setup steps: `architecture/deployment-guide.md`. Record the real values here the moment they
exist, so nobody builds against a guessed or stale URL.

| Field | Value |
|---|---|
| Cloud provider | Azure |
| Resource group | `smartsolar-rg` *(fill in if different)* |
| VM name | `smartsolar-api` *(fill in if different)* |
| VM public IP / DNS name | *(not yet created)* |
| API base URL (what both clients point at) | `http://<public-ip-above>/api` |
| MongoDB Atlas project / cluster | *(fill in)* |
| Who holds the Azure account + admin password | *(fill in — one person, per the deployment guide §1)* |
| Last successful redeploy | *(date, once `scripts/deploy-to-iis.ps1` has been run for real)* |
