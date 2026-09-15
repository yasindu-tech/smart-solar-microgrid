# Smart Solar Microgrid Trading System — Requirements Knowledge Base

**Single source of truth.** If behaviour is not written down here, it is not agreed. If a developer
implements something that contradicts a document here, the document wins until an ADR changes it.

Module: SE4040 Enterprise Application Development · Group of 4 · Deadline **30 Sept 2026, 23:59**

---

## How to use this knowledge base

### If you are a developer and you have a question

0. **"Why are we building this, and what is in scope?"** → `BRD.md`.
1. **"What should happen when X?"** → `requirements/` (the area doc), section 4 (main flow),
   6 (acceptance criteria) and 7 (edge cases).
2. **"Is this allowed?" / "What's the time limit?"** → `business-rules/BR-register.md`.
3. **"What does this endpoint return?"** → `architecture/api-contract.md`.
4. **"What fields does this collection have?"** → `architecture/data-model.md`.
5. **"What status can this move to?"** → `architecture/state-machines.md`.
5a. **"How do we get this actually running / redeploy it?"** → `architecture/deployment-guide.md`.
6. **"Why did we do it this way?"** → `decisions/`.
7. **"Who owns this, and what's the goal of their piece?"** → `domains/` (one doc per member) then `plan/work-breakdown.md` for the day-by-day plan.

**If the answer is not here, do not guess and do not decide alone in code.** Raise it as an open
question (section 12 of the relevant requirement doc), get a group answer, and record it. A decision
that changes shared behaviour becomes an ADR.

### If you are the technical PO

Use the `requirements-analyst` skill (`.claude/skills/requirements-analyst/SKILL.md`). It defines the
document formats used here, the developer Q&A protocol, and the rule that every answer must cite a
document ID or become one.

---

## Layout

```
docs/
├── README.md                     ← you are here
├── BRD.md                        ← business requirements: problem, scope, objectives, sign-off
├── project-config.md             ← the facts the skill reads: stack, roles, components, constraints
├── glossary.md                   ← one agreed word per concept
│
├── architecture/
│   ├── system-overview.md        ← 3-tier shape, what is allowed to live where
│   ├── data-model.md             ← MongoDB collections, SQLite tables, field-level spec
│   ├── api-contract.md           ← every endpoint, request, response, error code
│   ├── state-machines.md         ← reservation / account / node lifecycles
│   └── deployment-guide.md       ← Azure VM + IIS, step by step — who does what to get it live
│
├── requirements/                 ← REQ-* : the behavioural spec, one file per feature area
│   ├── REQ-AUTH-authentication-and-rbac.md
│   ├── REQ-USER-web-user-management.md
│   ├── REQ-PROS-prosumer-management.md
│   ├── REQ-NODE-microgrid-node-management.md
│   ├── REQ-SLOT-energy-slot-management.md
│   ├── REQ-RESV-reservation-management.md
│   ├── REQ-QRV-qr-dispatch-and-operator-verification.md
│   └── REQ-VIEW-dashboards-views-and-maps.md
│
├── user-flows/                   ← UF-* : end-to-end journeys across components
│   ├── UF-01-prosumer-registration.md
│   ├── UF-02-book-energy-slot.md
│   ├── UF-03-modify-booking.md
│   ├── UF-04-cancel-booking.md
│   ├── UF-05-operator-complete-transfer.md
│   └── UF-06-deactivation-and-reactivation.md
│
├── business-rules/
│   └── BR-register.md            ← every invariant, numbered, with where it is enforced
│
├── decisions/                    ← ADR-* : why, and what we rejected
│   ├── ADR-001-reservation-status-model.md
│   ├── ADR-002-nic-as-prosumer-identity.md
│   ├── ADR-003-qr-payload-and-server-verification.md
│   └── ADR-004-sqlite-scope-on-android.md
│
├── domains/                      ← D1-D4 : the final ownership split, one doc per member
│   ├── README.md                 ← the four domains, marks per domain, cross-domain handoffs
│   ├── DOM-01-identity-and-access-management.md
│   ├── DOM-02-microgrid-inventory-management.md
│   ├── DOM-03-reservation-and-scheduling-engine.md
│   └── DOM-04-fulfillment-and-operational-verification.md
│
└── plan/
    ├── work-breakdown.md         ← 17-day plan, 4 members, daily milestones
    └── traceability-matrix.md    ← every rubric line → requirement → owner → status
```

---

## ID conventions

| Prefix | Meaning | Example |
|---|---|---|
| `REQ-<AREA>-<n>` | A numbered requirement inside an area doc | `REQ-RESV-4` |
| `BR-<n>` | A business rule / invariant | `BR-07` |
| `AC-<n>` | An acceptance criterion, scoped to its requirement | `REQ-RESV-4 / AC-3` |
| `EC-<n>` | An edge case, scoped to its requirement | `REQ-RESV-4 / EC-2` |
| `UF-<nn>` | A user flow | `UF-04` |
| `ADR-<nnn>` | An architecture decision record | `ADR-001` |
| `E<n>` | An API error code | `E4102` |

Cite IDs in commit messages: `feat(resv): enforce BR-07 12h cancellation window (REQ-RESV-6)`.
The rubric rewards *meaningful, descriptive commits* — IDs are the cheapest way to get there.

---

## Document status values

- **Draft** — being written, do not build from it yet.
- **Agreed** — the group has read it; build exactly this.
- **Built** — implemented and demoable.
- **Superseded** — replaced; the replacing doc/ADR is named at the top.

---

## The one rule that protects your marks

The rubric gives **65 of 100 marks individually** and the viva asks *structured questions about your
submitted work and your development decisions*. Every document here is written so that the owner can
answer "why does it behave that way?" from the document, in their own words, without reading code.
Keep it that way.
