# Business Requirements Document

**Project:** Smart Solar Microgrid Trading System
**Module:** SE4040 Enterprise Application Development · Year 4 Semester 2, 2026
**Document version:** 1.0 · **Date:** 13 September 2026
**Status:** Draft — pending group sign-off
**Author:** Technical Product Owner, on behalf of the project group

> This BRD states the *business* problem, scope and objectives. It does not
> specify behaviour. Behaviour lives in `requirements/REQ-*.md`, invariants in
> `business-rules/BR-register.md`, and journeys in `user-flows/UF-*.md`. Every
> business requirement below names the documents that realise it.

---

## 1. Purpose

To define the business need, scope, stakeholders and success criteria for an
end-to-end Smart Solar Microgrid Trading System, so that a four-person team can
build it against one shared understanding rather than four private ones.

## 2. Business context

Rooftop solar has produced a population of **prosumers** — property owners who
both generate and consume electricity. Their surplus is stranded: it has value
to the grid but no orderly way to reach it. Meanwhile microgrid hubs have
finite battery storage and finite transfer windows, and no way to know who is
arriving when.

The gap is coordination, not hardware. Without a reservation system:

- Prosumers travel to hubs with no guarantee of an available transfer slot.
- Hub operators cannot plan staffing or battery capacity against real demand.
- Back-office staff have no reliable register of prosumers or hub capability.
- There is no verifiable record that a given energy transfer actually happened.

## 3. Business objectives

| # | Objective | How it is measured |
|---|---|---|
| BO-1 | Give prosumers a reliable way to reserve an energy transfer slot in advance | A prosumer can create, change and cancel a reservation from a phone, and see its status without contacting anyone |
| BO-2 | Give hub operators forward visibility of demand | An operator sees pending and approved future reservations for their hub, live |
| BO-3 | Make every energy transfer verifiable | No transfer is finalised without a server-verified, single-use credential |
| BO-4 | Give the back office a single administrative register | One place to manage prosumers, hubs, capacity and operating schedules |
| BO-5 | Protect hub planning against late changes | Changes and cancellations respect a fixed notice period |
| BO-6 | Keep the booking horizon short enough to stay accurate | Reservations cannot be made further ahead than demand can be predicted |

## 4. Stakeholders

| Stakeholder | Interest | Primary channel |
|---|---|---|
| **Solar prosumer** | Reserve a slot, know it is confirmed, change plans without penalty within reason | Android app |
| **Grid operator** | Know who is coming, keep availability accurate, verify and complete transfers | Android app + web |
| **Back-office officer** | Maintain the register of prosumers and hubs; control account status | Web |
| **Project group (4 members)** | Deliver a defensible system within 17 days and explain it at a viva | — |
| **Module assessors** | Evidence of enterprise architecture, business-rule enforcement and individual contribution | Report, demo, viva |

## 5. Scope

### 5.1 In scope

| Area | Summary | Realised by |
|---|---|---|
| Identity and access | Two web roles (Back-office, Grid Operator) and a prosumer role, with server-enforced authorisation | `REQ-AUTH`, `REQ-USER` |
| Prosumer register | Self-registration keyed on NIC, profile maintenance, deactivation, back-office-only reactivation | `REQ-PROS`, `UF-01`, `UF-06` |
| Microgrid hub register | Hub creation with GPS location, capacity, battery storage slots and operating schedules; guarded deactivation | `REQ-NODE` |
| Transfer slot management | Bookable windows at a hub, capacity accounting, operator availability updates | `REQ-SLOT` |
| Reservation lifecycle | Create, update, cancel, approve, complete, expire, under a booking horizon and a notice period | `REQ-RESV`, `UF-02`–`UF-04` |
| Transfer verification | Server-issued single-use credential, operator scan, server verification, finalisation | `REQ-QRV`, `UF-05` |
| Operational visibility | Role-appropriate dashboards, booking history with search and filter, hub map | `REQ-VIEW` |

### 5.2 Out of scope

Payments and energy pricing. Notifications of any kind (email, SMS, push).
Offline transaction capability. Partial or split transfers. Recurring or
standing reservations. Waiting lists. Hub hardware telemetry. Navigation or
directions. Password reset and email verification. Prosumer record deletion.
Multi-language interfaces.

These are listed so that they are visibly *decided*, not overlooked.

## 6. Business requirements

Business-level statements. Each maps to the specification documents that make
it real and to the invariants that constrain it.

| ID | Business requirement | Objective | Specified by | Constrained by |
|---|---|---|---|---|
| BRQ-01 | The organisation shall maintain an authoritative register of prosumers, each uniquely identified by their National Identity Card number | BO-4 | `REQ-PROS` | BR-12 |
| BRQ-02 | Only back-office staff shall perform system administration; operators shall have operational tools only | BO-4 | `REQ-AUTH`, `REQ-USER` | BR-01 |
| BRQ-03 | A prosumer shall be able to withdraw from the service themselves, but shall not be able to restore their own access | BO-4 | `REQ-PROS`, `UF-06` | BR-03, BR-04 |
| BRQ-04 | The organisation shall maintain a register of microgrid hubs including location, capacity, battery storage and operating schedule | BO-2, BO-4 | `REQ-NODE` | BR-19 |
| BRQ-05 | A hub shall not be withdrawn from service while energy is still booked at it | BO-2 | `REQ-NODE` | BR-09 |
| BRQ-06 | A prosumer shall be able to reserve an energy transfer slot at a hub in advance | BO-1 | `REQ-RESV`, `UF-02` | BR-05, BR-15, BR-16, BR-17 |
| BRQ-07 | Reservations shall not be made further ahead than the demand forecast is meaningful | BO-6 | `REQ-RESV` | **BR-05** — within 7 days |
| BRQ-08 | A prosumer shall be able to change or withdraw a reservation, provided the hub retains enough notice to re-plan | BO-1, BO-5 | `REQ-RESV`, `UF-03`, `UF-04` | **BR-06, BR-07** — at least 12 hours |
| BRQ-09 | A hub shall not accept more reservations for a slot than it has capacity to serve | BO-2 | `REQ-SLOT` | BR-15 |
| BRQ-10 | No energy transfer shall be recorded as complete without verification against the central system | BO-3 | `REQ-QRV`, `UF-05` | **BR-10**, BR-11, BR-18 |
| BRQ-11 | Each role shall see the operational picture relevant to them, computed centrally | BO-2 | `REQ-VIEW` | BR-14 |
| BRQ-12 | A prosumer shall be able to locate nearby hubs geographically | BO-1 | `REQ-VIEW` | BR-20 |
| BRQ-13 | A prosumer shall be able to review their own transfer history and search it | BO-1 | `REQ-VIEW` | BR-02 |

## 7. Non-functional and architectural requirements

| ID | Requirement | Rationale | Where |
|---|---|---|---|
| NFR-1 | All business logic shall reside in the central Web API; client applications shall be user-interface layers only | A rule enforced in one client is not enforced for the other, and cannot be trusted at all | `architecture/system-overview.md` §2, BR-14 |
| NFR-2 | The Web API shall be hosted on Windows IIS and reachable by both clients | Assignment constraint; also the integration gate for the whole system | `architecture/system-overview.md` §6 |
| NFR-3 | Server-side storage shall be a NoSQL database (MongoDB) with four collections | Assignment constraint | `architecture/data-model.md` |
| NFR-4 | The mobile client shall be pure native Android with a local SQLite database; cross-platform frameworks are prohibited | Assignment constraint | `ADR-004` |
| NFR-5 | All rule evaluation shall use the server clock in UTC; client-supplied timestamps shall never decide a rule | Device clocks are wrong, and trivially alterable | BR-13 |
| NFR-6 | Every rule violation shall return a documented error code and a message safe to show a user | Makes failures demonstrable rather than mysterious | `api-contract.md` §9 |
| NFR-7 | No credential shall appear in source control | | `.github/workflows/guards.yml` |
| NFR-8 | List endpoints shall support paging | Booking history grows without bound | `architecture/api-contract.md` |
| NFR-9 | Clients shall handle network failure with a visible message and no crash | | `REQ-VIEW` AC-11 |

## 8. Assumptions

| # | Assumption | If wrong |
|---|---|---|
| A-1 | A prosumer holds exactly one NIC and one account | The identity model in `ADR-002` needs revisiting |
| A-2 | Hub capacity is expressed adequately as a slot count per window | Capacity modelling in `REQ-SLOT` becomes more granular |
| A-3 | An operator is associated with specific hubs, not all hubs | BR-11 changes shape |
| A-4 | Energy transfer is a single event, not a metered session over time | The completion model in `REQ-QRV` changes |
| A-5 | Devices used by operators have a working camera and network at the hub | Offline verification would be required — currently out of scope |
| A-6 | Demand is predictable enough that a 7-day horizon is useful | BRQ-07's threshold changes |

## 9. Constraints

| # | Constraint | Source |
|---|---|---|
| C-1 | Fixed delivery date of 30 September 2026, with a compulsory viva after submission | Assignment brief |
| C-2 | Team of exactly four; 65 of 100 marks awarded individually | Assignment brief |
| C-3 | Technology stack is prescribed: C# Web API on IIS, MongoDB, pure native Android with SQLite, web UI using Bootstrap 5 / Tailwind / React | Assignment brief |
| C-4 | AI use is limited to the planning stage; implementation must be the team's own work, disclosed and defensible at viva | Assignment brief, AI Assessment Scale Level 2 |
| C-5 | Development must occur under version control with meaningful commits, as evidence of how the work was produced | Assignment brief |

## 10. Success criteria

The project is successful when:

1. Every business requirement BRQ-01 to BRQ-13 is traceable to a `REQ-*`
   document whose acceptance criteria all pass — tracked in
   `plan/traceability-matrix.md`.
2. Every business rule BR-01 to BR-20 can be demonstrated *failing correctly*:
   the refused operation returns its documented error code. The eleven
   headline demonstrations are listed at the end of the traceability matrix.
3. The Web API is hosted on IIS and both clients operate against the hosted
   instance, not a local one.
4. No business rule is enforced solely in a client.
5. Each member can explain, without reading code, the behaviour they own and
   the reasoning behind it.

## 11. Risks

Carried in `plan/work-breakdown.md` §6. The two that would most damage the
business outcome:

- **Interface contract not frozen early** — the four components cannot be built
  in parallel, and integration consumes the final week.
- **Business logic drifting into the clients** — directly contradicts NFR-1,
  and is the single largest block of architecture marks.

## 12. Related documents

| Document | Contains |
|---|---|
| `project-config.md` | Components, actors, constraints, Definition of Done |
| `glossary.md` | One agreed term per concept |
| `business-rules/BR-register.md` | All twenty invariants and where each is enforced |
| `requirements/REQ-*.md` | Behaviour, acceptance criteria, edge cases |
| `user-flows/UF-*.md` | End-to-end journeys across components |
| `architecture/*.md` | System shape, data model, API contract, state machines |
| `decisions/ADR-*.md` | Decisions taken and alternatives rejected |
| `plan/work-breakdown.md` | 17-day plan, ownership, risks |
| `plan/traceability-matrix.md` | Rubric line → requirement → owner → status |

## 13. Sign-off

This document is **Draft** until all four members have read it and confirmed
the scope. Record sign-off here.

| Member | Role | Date | Confirmed |
|---|---|---|---|
| | M1 — Identity | | ☐ |
| | M2 — Hubs and slots | | ☐ |
| | M3 — Reservations | | ☐ |
| | M4 — Verification, maps, views | | ☐ |
