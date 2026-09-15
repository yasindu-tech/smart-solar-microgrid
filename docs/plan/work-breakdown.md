# Work Breakdown — 17 Days to 30 September 2026

Status: **Agreed** · Today: Sun 13 Sep 2026 · Submission: **Wed 30 Sep 2026, 23:59**

Working assumption: 17 calendar days, but the last two are report, video and buffer. **Treat Sun 28
Sep as the code freeze.** That gives 15 build days.

---

## 1. Ownership — why it is split this way

**Superseded 14 Sep 2026.** Ownership is now organised as four business
domains, not an ad-hoc grouping — see `domains/README.md` for the full
definition, goals, and cross-domain handoffs. This table is the short form.

65 of 100 marks are individual. The split gives each member a full vertical
slice (API + web + mobile) of one domain, so that nobody's mark depends on
someone else finishing. Three pieces moved from the original grouping when
the domain view was adopted — Maps (D4→D2), prosumer booking views (D4→D3) —
rebalancing what had become a lopsided split (M4 previously carried ~27
individual marks against ~9–18 for everyone else).

| Domain | Member | Owns | Individual marks |
|---|---|---|---|
| **D1 — Identity & Access Management** | M1 | Auth, RBAC, web user management, prosumer management, deactivation/reactivation | 17 |
| **D2 — Microgrid Inventory Management** | M2 | Node CRUD, slot admin, availability, **map screen + Google Maps SDK** | 20 |
| **D3 — Reservation & Scheduling Engine** | M3 | Reservation create/update/cancel/approve, the 7-day and 12-hour rules, **prosumer booking dashboard, history, search, filter** | 15 |
| **D4 — Fulfillment & Operational Verification** | M4 | QR issue/verify/finalise, operator mode, operator dashboard, SQLite persistence | 13 |

**17 + 20 + 15 + 13 = 65** — every individual mark is accounted for exactly
once; see `plan/traceability-matrix.md` for the line-by-line proof.

Shared, group-marked work (35 marks) is assigned explicitly in §3 so it does not fall between chairs.

---

## 2. The critical path

```
Day 1-2   Shared foundations  ──►  Day 3  API CONTRACT FROZEN  ──►  parallel build ──► Day 15 freeze
                                        ▲
                              nothing else can start in
                              earnest until this happens
```

**The single biggest risk in this project is not freezing the API contract early.** If the web and
Android developers are still guessing endpoint shapes on Day 8, integration will eat the last week.

---

## 3. Day-by-day

### Days 1–2 · Sun 13 – Mon 14 Sep — Foundations (everyone)

| Owner | Task |
|---|---|
| All | Read the whole knowledge base. Each member writes 3 questions it does not answer; resolve them and update the docs |
| All | GitHub repo created, `develop` branch, one feature branch per member, `.gitignore` for `appsettings.Development.json` and the Maps key |
| M1 | ASP.NET Web API skeleton, folder structure per `system-overview.md` §3, JWT middleware, error-envelope middleware, `DomainException` |
| M2 | MongoDB Atlas cluster, 4 collections, indexes, `/seed/seed.js` with the sample data from `data-model.md` §4 |
| M3 | Controller stubs for every endpoint in `api-contract.md`, each returning hard-coded sample JSON |
| M4 | Android project (pure native), SQLite helper with the 4 tables, Retrofit/HttpURLConnection client, base URL config |

**Exit criteria for Day 2:** repo exists, API runs locally, Mongo has seed data, Android app builds.

### Day 3 · Tue 15 Sep — **Contract freeze** (hard gate)

- Every stub returns realistic sample JSON.
- Postman collection committed at `/docs/api/SmartSolar.postman_collection.json`.
- Both client developers have called every stub once.
- **Run the checklist in `api-contract.md` §10 as a group, out loud, before anyone goes to bed.**

### Days 4–8 · Wed 16 – Sun 20 Sep — Core build

| Member | Deliverable by Day 8 |
|---|---|
| M1 | Real login working end-to-end on web and Android; role routing; web user CRUD; prosumer register + pending queue + approve |
| M2 | Node CRUD with validation; **BR-09 deactivation guard**; slot CRUD with the capacity invariant; web admin screens for both |
| M3 | Reservation create + update + cancel with **BR-05, BR-06, BR-07** fully enforced and unit-checked against the boundary cases in REQ-RESV AC-2…AC-7 |
| M4 | QR issue on approve, prosumer QR display, operator scanner, `verify-qr` + `finalize`; SQLite caching working |

**Day 8 checkpoint (Sun 20 Sep, evening):** demo to each other. Every member runs *their own* feature
from a client, not from Postman. Anything not demoable here is a red flag with 10 days left.

### Days 9–12 · Mon 21 – Thu 24 Sep — Integration and the marked details

| Member | Deliverable |
|---|---|
| M1 | Deactivation cascade (BR-04); reactivation restricted to Backoffice (BR-03) with the `E4303` screenshot; per-request status re-check (REQ-AUTH EC-6) |
| M2 | Slot-time propagation to reservations; BR-19 capacity guard; node schedule editor; the `E4004` refusal surfaced properly in the web UI |
| M3 | Approve flow; summary screens after create/update/cancel on mobile; prosumer dashboard + booking history, search and filters with **API-computed counts** |
| M2 | Google Maps with real pins + bottom sheet + **Book here** handoff into M3's slot picker |
| M4 | Operator dashboard (pending + approved-future counts, **API-computed**); offline banner for cached screens |
| M1 | Backoffice dashboard (totals, pending activations, active nodes) |
| All | UI polish pass — Bootstrap 5 / Tailwind applied consistently, home (index) page complete, every page implemented |

### Days 13–15 · Fri 25 – Sun 27 Sep — Deploy, harden, evidence

| Day | Work |
|---|---|
| 13 | **Deploy the API to IIS.** Both clients repointed to the hosted base URL and retested end to end. Do not leave this to the last weekend — IIS hosting is 4 group marks and always takes longer than expected |
| 14 | Walk every acceptance criterion in every REQ doc. Record pass/fail in `traceability-matrix.md`. Fix failures |
| 14 | Walk every **edge case**. This is where the Excellent bands live, and where viva questions come from |
| 15 | **Code freeze, Sun 28 Sep.** Comment header block on every `.cs` file; inline comment at the start of every method; unique screenshots captured for every UI |

### Days 16–17 · Mon 28 – Wed 30 Sep — Report, video, submit

| Owner | Task |
|---|---|
| All | Each member writes their own individual contribution section + AI-use disclosure (planning stage only) |
| M2 | Database design section from `data-model.md` |
| M4 | High-level architecture diagram, use case diagram, DFD |
| M1 | Screenshots of all UIs, assembled and labelled |
| M3 | Source code pasted **as text** into the report |
| All | Challenges section — use the "Bad / accepted" lists in the four ADRs; they are already written as honest reflections |
| All | README: repo link, per-member contributions, ≤5 min video link |
| M4 | Record and edit the demo video (≤5 minutes) |
| — | **Zip named `IT<number>.zip`, submit by Tue 29 Sep evening.** One day of buffer is not optional |

---

## 4. Daily rhythm

- 15-minute stand-up, same time daily. Three questions: what did you finish, what is blocked, does
  anything in the knowledge base need to change?
- Every change to shared behaviour → update the doc in the same commit as the code.
- Merge to `develop` at least once a day. A branch that lives a week is a branch that conflicts.

## 5. Commit convention (the rubric rewards this directly)

```
<type>(<area>): <what> (<REQ or BR id>)

feat(resv): enforce 12-hour cancellation window (BR-07, REQ-RESV AC-5)
fix(node): block deactivation when active reservations exist (BR-09)
docs(adr): record QR payload decision (ADR-003)
```

"Develop under version control with meaningful, descriptive commits" is stated in the assignment's AI
section as evidence of how the work was produced. Vague commits (`update`, `fix bug`, `final`) are a
liability at the viva, not just a style issue.

## 6. Risk register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| API contract not frozen by Day 3 | Medium | **Critical** | Hard gate, group sign-off, stubs before logic |
| IIS deployment left to the final weekend | High | High | Scheduled on Day 13, not Day 16 |
| Business logic drifts into the Android app | Medium | High (12 group marks) | BR-14 + ADR-004 + the Day 14 code-review check |
| One member falls behind, blocking another | Medium | High | Vertical slices (§1) — no member's mark depends on another's feature |
| Dashboard counts hard-coded to save time | Medium | High (10 individual marks) | REQ-VIEW AC-13 is an explicit check |
| Google Maps key not working on the demo device | Medium | Medium | Test on the actual demo device by Day 12 |
| Viva: member cannot explain their own code | Medium | **Severe** (individual marks) | Every requirement doc is written to be explainable without code; rehearse on Day 15 |

## 7. Viva rehearsal (Day 15, one hour, do not skip)

Each member is asked, by the others:

1. Walk me through what happens when a prosumer cancels 11 hours before their slot. *(UF-04)*
2. Where is that rule enforced, and why not in the app? *(BR-14, ADR-004)*
3. Why does the QR still work if the prosumer screenshots it? *(ADR-003, consequences)*
4. Why is NIC the `_id`? *(ADR-002)*
5. Show me a line of code you would change if we had another week, and say why.
6. Which AI tools did you use in planning, and what did you change about what they gave you?

Question 6 is explicitly promised by the assignment. Have a real answer ready.
