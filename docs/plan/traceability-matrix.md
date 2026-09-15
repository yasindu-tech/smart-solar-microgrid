# Traceability Matrix — Rubric → Requirement → Owner → Status

Status: **Agreed (statuses to be updated daily from Day 8)**

Every marked line item in the SE4040 marking scheme, mapped to the document that specifies it and the
person accountable. **If a row has no requirement, the marks are unclaimed.** Update the Status column
as you go; this is the sheet you walk on Day 14.

**Ownership updated 14 Sep 2026** to match the final four-domain split in `domains/README.md`. Four
rows below are marked `(moved from M4, 14 Sep)` — Map + Maps SDK moved to M2, prosumer booking views
moved to M3. New per-member individual totals: **M1 17 · M2 20 · M3 15 · M4 13** (sums to 65).

Status values: `Not started` · `In progress` · `Built` · `Verified` (every AC and EC in the doc passed)

---

## Table 1 — Group contribution (35 marks)

| Rubric criterion | Marks | Requirement / doc | Owner | Status |
|---|---|---|---|---|
| Hosting the Web API / Service on IIS | 4 | `system-overview.md` §6, work-breakdown Day 13 | M1 + all | Not started |
| MongoDB connection | 4 | `data-model.md`, `system-overview.md` §6 | M2 | Not started |
| *(Service Architecture & API Design total)* | **8** | `system-overview.md`, `api-contract.md` | M1 | Not started |
| User's detail collection | 1 | `data-model.md` §1.2 (`Prosumers`) | M1 | Not started |
| SolarStationInfo collection | 1 | `data-model.md` §1.3 | M2 | Not started |
| EnergyBookingSlots collection | 1 | `data-model.md` §1.4 | M2 | Not started |
| Energy Reservation collection | 1 | `data-model.md` §1.5 | M3 | Not started |
| *(Database Design total)* | **4** | | M2 | Not started |
| Pure native Android with SQLite | 6 | `ADR-004`, `data-model.md` §3, `REQ-VIEW` | M4 | Not started |
| Web application as a UI layer | 6 | `system-overview.md` §2 (FAT service) | M2 | Not started |
| *(Client Build & Architecture Compliance total)* | **12** | | M4 + M2 | Not started |
| Mobile interfaces | 2 | `REQ-VIEW` §2 | M4 | Not started |
| Web interfaces with Tailwind / Bootstrap 5 | 2 | `REQ-USER`, `REQ-NODE`, `REQ-SLOT` §10 | M2 | Not started |
| Home (index) page | 1 | work-breakdown Days 9–12 | M2 | Not started |
| Completeness of all pages | 1 | traceability walk, Day 14 | all | Not started |
| *(UI & Experience Design total)* | **6** | | M2 | Not started |
| Screenshots of all UIs | 1 | work-breakdown Day 15 | M1 | Not started |
| High-level, use case and DFD diagrams | 1 | work-breakdown Days 16–17 | M4 | Not started |
| References | 1 | report | all | Not started |
| Individual contribution | 1 | report + README | all | Not started |
| Challenges | 1 | ADR "Bad / accepted" sections | all | Not started |
| *(Documentation & Deployment total)* | **5** | | M1 | Not started |

---

## Table 2 — Individual contribution (65 marks)

### Web Application Features and Business Rules (18)

| Line item | Marks | Requirement | Owner | Status |
|---|---|---|---|---|
| Login and role-based access | 4 | `REQ-AUTH` AC-1…AC-4, AC-10 | M1 | Not started |
| User management | 4 | `REQ-USER` AC-1…AC-8 | M1 | Not started |
| Microgrid node management | 5 | `REQ-NODE` AC-1…AC-10 (BR-09 is AC-3) | M2 | Not started |
| Slot booking management | 5 | `REQ-SLOT` AC-1…AC-10, `REQ-RESV` (web side) | M2 + M3 | Not started |
| *Rubric's Excellent band also requires:* Backoffice and Operator users created; stations and slots created/updated/deleted; reservations created/updated/cancelled with **7-day and 12-hour rules enforced** | — | `BR-05`, `BR-06`, `BR-07` | M3 | Not started |

### Mobile Authentication and Account Management (9)

| Line item | Marks | Requirement | Owner | Status |
|---|---|---|---|---|
| Login with role-based home | 2 | `REQ-AUTH` AC-3, AC-4, AC-8, AC-9 | M1 | Not started |
| Pending activation view in web app | 2 | `REQ-PROS` §4.2, AC-3; `UF-01` steps 6–8 | M1 | Not started |
| Create account in mobile | 3 | `REQ-PROS` §4.1, AC-1, AC-2; `UF-01` | M1 | Not started |
| Modify own account | 1 | `REQ-PROS` §4.3, AC-4, AC-5 | M1 | Not started |
| Deactivate account | 1 | `REQ-PROS` §4.4, AC-6; `UF-06` Part A | M1 | Not started |

### Reservation Workflow and Booking Management (9)

| Line item | Marks | Requirement | Owner | Status |
|---|---|---|---|---|
| Create booking request | 3 | `REQ-RESV` §4.1, AC-1…AC-3; `UF-02` | M3 | Not started |
| Update request | 2 | `REQ-RESV` §4.2, AC-7…AC-9; `UF-03` | M3 | Not started |
| Cancel request | 2 | `REQ-RESV` §4.3, AC-4…AC-6; `UF-04` | M3 | Not started |
| Summary page after each action | 2 | `REQ-RESV` AC-15 | M3 | Not started |

### Booking Views and Operational Dashboards (10)

| Line item | Marks | Requirement | Owner | Status |
|---|---|---|---|---|
| Current / pending bookings | 2 | `REQ-VIEW` §2.2, AC-1, AC-2 | **M3** (moved from M4, 14 Sep) | Not started |
| Booking history | 2 | `REQ-VIEW` §2.2, AC-3 | **M3** (moved from M4, 14 Sep) | Not started |
| Filter criteria | 2 | `REQ-VIEW` AC-4, AC-5 | **M3** (moved from M4, 14 Sep) | Not started |
| Pending reservations (operator) | 2 | `REQ-VIEW` §2.3, AC-6, AC-7 | M4 | Not started |
| Count of approved future reservations | 2 | `REQ-VIEW` §2.3, AC-13 | M4 | Not started |

### Grid Operator Verification and Map Features (7)

| Line item | Marks | Requirement | Owner | Status |
|---|---|---|---|---|
| Read QR code and update the job as done | 2 | `REQ-QRV` AC-2, AC-3; `UF-05` | M4 | Not started |
| Show nearby stations on the map | 5 | `REQ-VIEW` §2.5, AC-8…AC-10, AC-12 | **M2** (moved from M4, 14 Sep) | Not started |

### Service Integration, Local Persistence and Device Capabilities (12)

| Line item | Marks | Requirement | Owner | Status |
|---|---|---|---|---|
| Web app to Web API | 2 | `system-overview.md` §2 | M2 | Not started |
| Mobile app to Web API | 2 | `system-overview.md` §2, `BR-14` | M4 | Not started |
| SQLite local persistence | 3 | `ADR-004`, `data-model.md` §3, `REQ-VIEW` AC-11 | M4 | Not started |
| Google Maps API integration | 3 | `REQ-VIEW` §2.5 | **M2** (moved from M4, 14 Sep) | Not started |
| QR code scanning | 2 | `REQ-QRV` AC-2, AC-5, AC-10 | M4 | Not started |

---

## Coverage check — assignment features with no marks attached

These appear in the assignment brief but not as separate rubric lines. Build them anyway; their absence
shows up in the "Completeness of all pages" and architecture-compliance bands.

| Feature | Where specified | Owner |
|---|---|---|
| Node GPS location, capacity kW/h, battery storage slots | `REQ-NODE` §4.1 | M2 |
| Node schedules | `REQ-NODE` §4.2 | M2 |
| Operator updates battery slot availability | `REQ-SLOT` §4.2 | M2 |
| Prosumer reactivation restricted to Backoffice | `UF-06` Part B, `BR-03` | M1 |
| Responsive web UI | `REQ-*` §10 | M2 |
| Comment header block on every `.cs` file | `project-config.md` §7 — **code without this is not marked** | all |
| Source code as text in the report | `project-config.md` §7 | M3 |
| ≤5 min video + README | `project-config.md` §7 | M4 |

---

## Business rules coverage

Every rule must be enforced somewhere and demonstrable. Tick when you have run the failing case and
seen the documented error code.

| Rule | Demo | Verified |
|---|---|---|
| BR-03 Backoffice-only reactivation | Operator attempts it → `E4303` | ☐ |
| BR-04 Deactivation cancels future bookings | Deactivate with 2 bookings → both `Cancelled` | ☐ |
| BR-05 7-day horizon | Book 8 days out → `E4101` | ☐ |
| BR-06 12h update | Update 11h out → `E4103` | ☐ |
| BR-07 12h cancel | Cancel 11h out → `E4102` | ☐ |
| BR-09 Node deactivation guard | Deactivate node with a future booking → `E4004` | ☐ |
| BR-10 Server-side QR verification | Edit one char of the token → `E4201` | ☐ |
| BR-11 Operator node assignment | Wrong operator scans → `E4303` | ☐ |
| BR-15 Slot capacity | Book a full slot → `E4106` | ☐ |
| BR-17 Inactive account gate | Deactivated prosumer logs in → `E4302` | ☐ |
| BR-18 Single-use QR | Rescan a completed booking → `E4202` | ☐ |

**Screenshot every one of these.** Eleven screenshots of correctly-refused operations is the most
efficient evidence you can put in a report, and each one is a rehearsed viva answer.
