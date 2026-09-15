# Architecture — System Overview

Status: **Agreed — final system architecture**, confirmed 14 Sep 2026

This document describes the **technical tiers** (C1–C4): what the system is
built with. For the **business domains** (D1–D4) — what each of the four
members owns, cutting across these tiers — see `domains/README.md` and
section 1a below. The two are different axes of the same system; do not
confuse a technical tier with a business domain.

---

## 1. Shape

```
┌────────────────────────┐          ┌────────────────────────┐
│  Android (C3)          │          │  Web app (C2)          │
│  pure native + SQLite  │          │  React / Bootstrap 5   │
│  Prosumer, Operator    │          │  Backoffice, Operator  │
└───────────┬────────────┘          └───────────┬────────────┘
            │  HTTPS / REST + JWT                │
            └───────────────┬────────────────────┘
                            ▼
              ┌──────────────────────────────┐
              │   Web API (C1)               │
              │   C# ASP.NET, IIS            │
              │   ALL business logic lives   │
              │   here. Nothing else does.   │
              └──────────────┬───────────────┘
                             ▼
                   ┌───────────────────┐
                   │  MongoDB (C4)     │
                   │  4 collections    │
                   └───────────────────┘
```

## 1a. The final system, read as four business domains

The diagram above shows tiers. Read the same system as **domains** — four
vertical slices, one per member, each with code in every tier that domain
needs. This is the organisation confirmed on 14 Sep 2026 and detailed in
`domains/README.md`.

```
                    C2 Web app          C3 Android          C1 API / C4 Mongo
                 ┌───────────────┬───────────────────┬───────────────────────┐
  D1 Identity    │ user & prosumer│ login, register,  │ Auth/User/Prosumer    │
                 │ admin, pending │ profile, deactivate│ Services · Users +   │
                 │ activation view│ · SQLite session   │ Prosumers collections│
                 ├───────────────┼───────────────────┼───────────────────────┤
  D2 Inventory   │ node & slot    │ map: nearby nodes  │ Node/Slot Services ·  │
                 │ admin, schedule│ · Google Maps SDK  │ SolarStationInfo +    │
                 │ editor         │ · SQLite node_cache│ EnergyBookingSlots    │
                 ├───────────────┼───────────────────┼───────────────────────┤
  D3 Reservation │ create-on-     │ book/modify/cancel │ ReservationService,   │
                 │ behalf, approve│ · dashboard, history│ Rules, StateMachine ·│
                 │ / cancel table │ · SQLite resv_cache│ EnergyReservation     │
                 ├───────────────┼───────────────────┼───────────────────────┤
  D4 Fulfillment │ operator       │ QR display (prosumer│ QrTokenService,       │
                 │ dashboard      │ side) · scanner &  │ DashboardService ·    │
                 │ widgets        │ finalise (operator) │ reservation.status →  │
                 │                │                     │ Completed             │
                 └───────────────┴───────────────────┴───────────────────────┘
```

Every cell above still obeys the FAT-service rule in §2 below: the API column
is where a rule is *enforced*; the client columns are where it is *shown*.
Cross-domain handoffs (map pin → booking flow, approval → QR issue, etc.) are
listed in `domains/README.md`.

## 2. What is allowed to live where

| Concern | C1 API | C2 Web | C3 Android | C4 Mongo |
|---|:--:|:--:|:--:|:--:|
| Authentication / token issue | ✅ | ✖ | ✖ | ✖ |
| Authorisation (role checks) | ✅ enforced | ⚠ hides UI only | ⚠ hides UI only | ✖ |
| Business rule enforcement (BR-*) | ✅ **only here** | ✖ | ✖ | ✖ |
| Input format validation | ✅ authoritative | ⚠ for UX | ⚠ for UX | ✖ |
| Persisting reservations / nodes / users | ✅ | ✖ | ✖ | ✅ store |
| Session token, own profile cache, node reference cache | ✖ | ✖ | ✅ SQLite | ✖ |
| QR **generation** | ✅ issues signed payload | ✖ | ✅ renders it | ✖ |
| QR **verification** | ✅ **only here** | ✖ | ✖ | ✖ |
| Google Maps rendering | ✖ | ✖ | ✅ | ✖ |

⚠ = permitted as a presentation nicety, never as the enforcement point. If the button is disabled in
the UI, the API must still reject the request when it arrives.

## 3. Layering inside the API (C1)

```
Controllers/      thin. bind, authorise, call service, map result to HTTP.
Services/         ALL rules. one service per domain area. throws DomainException(code).
Repositories/     Mongo access only. no rules, no validation.
Models/           persisted documents.
DTOs/             request and response shapes. never expose the Mongo model directly.
Middleware/       JWT, exception → error envelope, request logging.
```

**Controller test**: if a controller method contains an `if` about *domain meaning* (dates, statuses,
ownership, counts), it is in the wrong place. Move it to the service.

## 4. Authentication

- JWT bearer token issued by `POST /api/auth/login`.
- Claims: `sub` (userId or NIC), `role` (`Backoffice` | `Operator` | `Prosumer`), `exp`.
- Android stores the token in SQLite (`session` table) so the user stays logged in across launches.
- Web stores it in memory + `sessionStorage`.
- Every protected endpoint is decorated with a role requirement. Role checks are **server-side**.

## 5. Error envelope

Every non-2xx response from the API uses one shape, so both clients can handle errors with one code path:

```json
{
  "code": "E4102",
  "message": "Cancellation requires at least 12 hours notice before the slot start time.",
  "field": "slotStartTime",
  "traceId": "c2f1a..."
}
```

`message` is safe to show to the user. `code` is what the clients branch on. The full catalogue is in
`api-contract.md` §9.

## 6. Deployment

**Full step-by-step guide: `architecture/deployment-guide.md`.** This table is the summary.

| Piece | Target | Notes |
|---|---|---|
| Web API | IIS on an **Azure VM** (Windows Server 2022), application pool `SmartSolarApi` | Rubric explicitly rewards "hosted on IIS and reachable by both clients". Confirmed host 14 Sep 2026 |
| MongoDB | MongoDB Atlas free tier (M0), VM's public IP allow-listed | Connection string in `appsettings.Production.json` on the VM only — **never committed** |
| Web app | Built with `npm run build`, `VITE_API_BASE_URL` pointed at the hosted API | Must be reachable from a browser during the viva |
| Android | Debug APK, `API_BASE_URL` pointed at the hosted API | Physical device or emulator with Google Play services (needed for Maps) |

Android emulator reaches a **locally** hosted API at `http://10.0.2.2:<port>`, not `localhost` — but
once the API is on the Azure VM, both clients use the VM's real public IP instead, recorded in
`project-config.md` §9. Redeploy with `scripts/deploy-to-iis.ps1`, run on the VM.

## 7. Cross-cutting non-functional expectations

| # | Expectation | Why |
|---|---|---|
| NFR-1 | Every API response for a list endpoint supports paging (`page`, `pageSize`) | Booking history grows; rubric asks for history + search + filter |
| NFR-2 | No endpoint returns a password hash or the QR signing secret | Basic hygiene, viva question |
| NFR-3 | Clients handle network failure gracefully — visible message, no crash | Rubric: "handle responses and errors gracefully" |
| NFR-4 | All rule violations return a documented `E****` code, never a bare 500 | Makes edge-case demos in the viva trivial |
| NFR-5 | Every `.cs` file has a header comment block; every method has a leading inline comment | **Code without this is not marked** |
