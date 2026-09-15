# Architecture — State Machines

Status: **Agreed**

Three lifecycles. A transition not drawn here is **not allowed** and must be rejected with `E4105`
(invalid state transition). This file exists so that four developers never disagree about what
"cancelled" means.

---

## 1. Reservation lifecycle

```
                    prosumer creates
                           │
                           ▼
                     ┌───────────┐
         operator/   │  Pending  │  ── prosumer or operator cancels (≥12h) ──┐
         backoffice  └─────┬─────┘                                           │
         approves          │                                                 │
                           ▼                                                 ▼
                    ┌────────────┐  ── prosumer/operator cancels (≥12h) ─▶ ┌───────────┐
                    │  Approved  │                                          │ Cancelled │
                    │  + qrToken │                                          └───────────┘
                    └─────┬──────┘                                                ▲
                          │                                                       │
     operator scans QR,   │                       slot start passed with          │
     server verifies,     │                       no completion ─────────────┐    │
     transfer finalised   ▼                                                  ▼    │
                    ┌────────────┐                                    ┌──────────┐│
                    │ Completed  │                                    │ Expired  ││
                    └────────────┘                                    └──────────┘│
                                                                                  │
                          Terminal states: Cancelled, Completed, Expired ─────────┘
```

### Transition table

| From | To | Trigger | Who | Rules enforced | Error if refused |
|---|---|---|---|---|---|
| — | `Pending` | Create reservation | Prosumer (mobile) | BR-05 (7-day), BR-15 (slot capacity), BR-16 (refs exist), BR-17 (account Active) | `E4101`, `E4106`, `E4404`, `E4302` |
| `Pending` | `Approved` | Approve | Operator / Backoffice | BR-15 capacity still free | `E4106` |
| `Pending` | `Cancelled` | Cancel | Prosumer (own) / Operator | BR-07 (≥12h) | `E4102` |
| `Pending` | `Pending` | Update (time / kWh) | Prosumer (own) / Operator | BR-06 (≥12h), BR-05 (new time still ≤7 days) | `E4103`, `E4101` |
| `Approved` | `Cancelled` | Cancel | Prosumer (own) / Operator | BR-07 (≥12h). Restores slot capacity, invalidates QR | `E4102` |
| `Approved` | `Approved` | Update | Prosumer (own) / Operator | BR-06 (≥12h). **Re-issues a new QR token**, old token invalid | `E4103` |
| `Approved` | `Completed` | QR scanned + server-verified + finalised | Operator | BR-10 (QR valid, unused, matches reservation), BR-11 (operator assigned to node) | `E4201`, `E4202`, `E4203`, `E4303` |
| `Pending` / `Approved` | `Expired` | Slot start time passed, not completed | System (lazy, on read) | — | — |
| any terminal | anything | — | — | Rejected | `E4105` |

### Expiry is lazy, not scheduled

We do **not** run a background job. When a reservation in `Pending`/`Approved` is read and its
`slotEndTime` is in the past, the service marks it `Expired` before returning it. Rationale and the
rejected alternative are in ADR-001. This keeps the API stateless and is trivially demoable at the viva.

---

## 2. Prosumer account lifecycle

```
   register (mobile)
         │
         ▼
  ┌────────────────────┐   backoffice approves   ┌──────────┐
  │ PendingActivation  │ ──────────────────────▶ │  Active  │
  └────────────────────┘                          └────┬─────┘
                                                       │ prosumer requests deactivation
                                                       ▼
                                              ┌────────────────┐
                                              │  Deactivated   │
                                              └───────┬────────┘
                                                      │  ONLY Backoffice (BR-03)
                                                      └──────────▶ Active
```

| From | To | Trigger | Who | Notes |
|---|---|---|---|---|
| — | `PendingActivation` | Self-registration on mobile | Prosumer | Group decision: registration is gated. See ADR-002 §4 |
| `PendingActivation` | `Active` | Approve | **Backoffice only** | Appears in the web "pending activations" view (rubric: 2 marks) |
| `Active` | `Deactivated` | Self-service deactivation request | Prosumer (own account) | BR-04: takes effect immediately; future reservations auto-cancelled |
| `Deactivated` | `Active` | Reactivate | **Backoffice only** (BR-03) | Operator attempting this → `E4303` |
| `Deactivated` | login | — | — | Login refused with `E4302` and a message naming the reason |

A `Deactivated` or `PendingActivation` prosumer **cannot create or modify reservations** (BR-17).

---

## 3. Microgrid node lifecycle

```
   backoffice creates
         │
         ▼
   ┌──────────┐   deactivate (only if NO active reservations — BR-09)   ┌────────────┐
   │  Active  │ ──────────────────────────────────────────────────────▶ │ Deactivated│
   └──────────┘ ◀────────────────────── reactivate (Backoffice) ─────── └────────────┘
```

| From | To | Trigger | Who | Rule | Error |
|---|---|---|---|---|---|
| — | `Active` | Create node | Backoffice | Unique `nodeId`, valid lat/long, capacity > 0 | `E4001`, `E4002` |
| `Active` | `Active` | Update (schedule, capacity, battery slots) | Backoffice, or Operator for **availability only** | Reducing `batterySlotCount` below the count of approved future reservations is refused | `E4003` |
| `Active` | `Deactivated` | Deactivate | Backoffice | **BR-09**: refuse if any reservation on this node is `Pending` or `Approved` with a future start time | `E4004` |
| `Deactivated` | `Active` | Reactivate | Backoffice | — | — |

A deactivated node must not appear in mobile "nearby nodes", must not be bookable, and its existing
completed history remains readable.

---

## 4. How to enforce this in code (C1)

One place, one pattern:

```csharp
// ReservationService.cs
// Guard clause: validate that the requested status change is one the domain allows.
private static readonly Dictionary<string, string[]> Allowed = new() {
    ["Pending"]   = new[] { "Approved", "Cancelled", "Pending", "Expired" },
    ["Approved"]  = new[] { "Cancelled", "Completed", "Approved", "Expired" },
    ["Cancelled"] = Array.Empty<string>(),
    ["Completed"] = Array.Empty<string>(),
    ["Expired"]   = Array.Empty<string>()
};
```

Every status write goes through one `AssertTransition(from, to)` helper. This is a good viva answer
and costs twenty lines.
