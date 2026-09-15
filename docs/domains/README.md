# Business Domains

Status: **Agreed** · Supersedes the informal M1–M4 grouping used before 14 Sep 2026

This is the final functional decomposition of the system, agreed by the group on
14 September 2026. It replaces the earlier ad-hoc grouping in
`plan/work-breakdown.md` §1 (that file has been updated to match).

## Two different meanings of "component" — do not confuse them

This project uses "component" in two senses. Keep them separate when reading
any document:

| Term | Meaning | Defined in |
|---|---|---|
| **Technical tier** (`C1`–`C4`) | *What it's built with* — Web API, Web app, Android app, MongoDB | `architecture/system-overview.md` |
| **Business domain** (`D1`–`D4`) | *What it's for* — a functional slice that cuts across all four technical tiers | this folder |

A domain is not a tier. Domain D3 (Reservation & Scheduling) has code in the
Web API, the web app, and the Android app — three tiers, one domain, one
owner. This is deliberate: it is what lets one member carry a full vertical
slice of the system and be examined on it at the viva without depending on
anyone else finishing first.

## The four domains

| ID | Domain | Owner | Individual marks |
|---|---|---|---|
| [D1](DOM-01-identity-and-access-management.md) | Identity & Access Management | M1 | 17 / 65 |
| [D2](DOM-02-microgrid-inventory-management.md) | Microgrid Inventory Management | M2 | 20 / 65 |
| [D3](DOM-03-reservation-and-scheduling-engine.md) | Reservation & Scheduling Engine | M3 | 15 / 65 |
| [D4](DOM-04-fulfillment-and-operational-verification.md) | Fulfillment & Operational Verification | M4 | 13 / 65 |

**65 / 65 accounted for** — see `plan/traceability-matrix.md` for the full
line-by-line reconciliation against the SE4040 marking scheme.

## What changed from the original split (14 Sep 2026)

Three rubric-marked pieces moved when the group adopted the four-domain view.
Recorded here because it changes what each member is individually examined on
at the viva — if a member built to the old assignment, they need to know it moved.

| What | Was | Now | Marks | Why |
|---|---|---|---|---|
| Map screen (nearby nodes) + Google Maps SDK integration | D4 | **D2** | 5 + 3 = 8 | The map is how a prosumer *finds inventory* — it is Inventory Management's output, not Fulfillment's |
| Prosumer booking dashboard, history, search, filter | D4 | **D3** | 2+2+2 = 6 | These are views over the Reservation engine's own data; D3 already owns the create/update/cancel flows they display |
| Back-office dashboard (unmarked, but real work) | unassigned | **D1** | — | Back-office identity/admin is D1's domain; the dashboard is an Identity concern |

Net effect: individual marks rebalanced from a lopsided **17 / 12 / 9 / 27**
(under the earlier M1–M4 split) to a much fairer **17 / 20 / 15 / 13**. See
each domain's "Definition of done" for what stayed the same.

## Cross-domain handoffs

A domain owns a vertical slice, but the system is one product. These are the
exact points where one domain's output becomes another's input — get these
interfaces right and each member can build independently between them.

| From | To | Handoff | Where it's specified |
|---|---|---|---|
| D2 (map pin tapped) | D3 (booking flow) | `nodeId` carried into the slot picker via the "Book here" action | `requirements/REQ-VIEW-dashboards-views-and-maps.md` §2.5, `user-flows/UF-02-book-energy-slot.md` step 1–2 |
| D2 (slot admin) | D3 (capacity consumption) | `EnergyBookingSlots.availableCapacity` — D2 owns the CRUD screen, D3's `ReservationService` owns decrementing/restoring it | `requirements/REQ-SLOT-energy-slot-management.md` §2, BR-15 |
| D3 (reservation approved) | D4 (QR issue) | Approval transition issues `qrToken` — D3 triggers it, D4 owns the token's format and verification | `decisions/ADR-003-qr-payload-and-server-verification.md`, `requirements/REQ-QRV` |
| D4 (transfer completed) | D3 (booking history) | `status: Completed` write is D4's; the history screen showing it is D3's | `requirements/REQ-VIEW` §2.2 |
| D1 (account deactivated) | D3 (bookings cancelled) | BR-04 cascade — D1's deactivation endpoint cancels D3's reservation records | `user-flows/UF-06-deactivation-and-reactivation.md` Part A |

## Shared files — reviewed by more than one domain owner

A small number of backend files serve more than one domain and cannot be
split cleanly at the file level. `.github/CODEOWNERS` lists both owners on
these; treat a PR touching them as needing a second pair of eyes even though
self-merge is technically allowed:

- `Controllers/DashboardController.cs`, `Services/DashboardService.cs` —
  one file, three endpoints (`/dashboard/prosumer` is D3, `/dashboard/operator`
  is D4, `/dashboard/backoffice` is D1)
- `Controllers/SlotsController.cs`, `Services/SlotService.cs` — D2 owns
  admin CRUD, D3 owns the capacity-consumption path invoked from
  `ReservationService`
