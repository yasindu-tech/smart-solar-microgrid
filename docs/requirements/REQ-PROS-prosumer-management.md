# REQ-PROS · Prosumer Management (NIC-keyed accounts)

Status: **Agreed** · Owner: **Member 1** · Components: C1 API, C2 Web, C3 Android, C4 Mongo
Rubric: *Web — User Management (4)* · *Mobile Auth & Account Management — Pending activation view (2), Create account (3), Modify own account (1), Deactivate account (1)*

---

## 1. Intent

Prosumers self-register on Android using their **NIC as the primary key**, manage their own profile,
and may request deactivation. Backoffice administers them from the web and holds the sole power to
reactivate a deactivated account.

## 2. Actors and permissions

| Action | Prosumer (self) | Operator | Backoffice |
|---|:--:|:--:|:--:|
| Register (mobile) | ✅ | ✖ | ✖ |
| View own profile | ✅ | — | — |
| Edit own profile | ✅ | ✖ | ✅ |
| Request deactivation | ✅ | ✖ | ✅ |
| **Reactivate** | ✖ | ✖ | ✅ **only** (BR-03) |
| Approve pending activation | ✖ | ✖ | ✅ |
| List / search prosumers | ✖ | ✅ read | ✅ |

## 3. Preconditions

Registration: none (public endpoint). Everything else: authenticated, and owner-or-Backoffice (BR-02).

## 4. Main flows

### 4.1 Register (Android)
1. Prosumer enters NIC, full name, email, phone, address, declared solar capacity, password.
2. API validates NIC format, uniqueness (BR-12) and required fields.
3. Document written with `status: "PendingActivation"`.
4. App shows "Account created — awaiting Backoffice approval" and returns to login.

### 4.2 Approve (Web, Backoffice)
1. Backoffice opens **Prosumers → Pending activations**.
2. Sees the queue with NIC, name, requested date.
3. Clicks Approve → `PATCH /prosumers/{nic}/activate` → status `Active`.
4. Prosumer can now log in.

### 4.3 Edit own profile (Android)
1. Prosumer opens Profile → Edit.
2. May change name, email, phone, address, solar capacity. **NIC is read-only** (BR-12).
3. API writes and returns the updated profile; Android refreshes `profile_cache`.

### 4.4 Request deactivation (Android)
1. Prosumer opens Profile → Deactivate account, confirms in a dialog naming the consequence.
2. API sets `status: "Deactivated"`, stamps `deactivationRequestedAt`, and **cancels all future
   `Pending`/`Approved` reservations**, restoring slot capacity (BR-04).
3. App logs out and clears the SQLite session.

### 4.5 Reactivate (Web, Backoffice only)
1. Backoffice filters the prosumer list by status `Deactivated`.
2. Clicks Reactivate → `PATCH /prosumers/{nic}/activate` → status `Active`, `reactivatedBy` stamped.
3. Previously cancelled reservations are **not** restored — the prosumer books again.

## 5. Business rules

BR-01 · BR-02 · BR-03 · BR-04 · BR-12 · BR-17

## 6. Acceptance criteria

| ID | Given | When | Then |
|---|---|---|---|
| AC-1 | A new NIC | prosumer registers on mobile | Record created with `status: PendingActivation`; login is refused with `E4302` |
| AC-2 | An NIC already registered | prosumer registers again | `E4001`, `field: "nic"`, nothing written |
| AC-3 | A pending prosumer | Backoffice approves from the web queue | Status becomes `Active`; the prosumer logs in successfully on the next attempt |
| AC-4 | An Active prosumer | edits phone and address on mobile | Values persist; re-opening the profile after app restart shows the new values |
| AC-5 | An Active prosumer | attempts to edit their NIC | The field is not editable in the UI **and** the API ignores/rejects an NIC in the update body |
| AC-6 | An Active prosumer with 2 future approved reservations | requests deactivation | Status `Deactivated`, both reservations `Cancelled`, both slots' `availableCapacity` incremented |
| AC-7 | A Deactivated prosumer | an **Operator** attempts reactivation | `E4303` — proves BR-03 |
| AC-8 | A Deactivated prosumer | Backoffice reactivates | Status `Active`, `reactivatedBy` set to the Backoffice `userId`, prosumer can log in |
| AC-9 | Backoffice on the prosumer list | searches "1995" | Matching NICs and names returned, paged |
| AC-10 | A prosumer | requests another prosumer's profile by NIC | `E4304` |

## 7. Edge cases

| ID | Case | Expected |
|---|---|---|
| EC-1 | NIC in old format (`951234567V`) vs new 12-digit | Accept both; validate with a regex covering `^([0-9]{9}[vVxX]|[0-9]{12})$`; store exactly as entered, compare case-insensitively |
| EC-2 | Prosumer deactivates with a reservation starting in 3 hours | BR-04 cancels it regardless of the 12-hour rule — deactivation is an account action, not a booking action. **Documented deliberately**; operator sees it disappear from today's list |
| EC-3 | Backoffice edits a prosumer's email to one already used | `E4001` |
| EC-4 | Prosumer registers, then Backoffice rejects rather than approves | Out of scope for v1 — there is no Reject action. Backoffice simply leaves it pending. Noted as a known limitation in the report |
| EC-5 | Deactivated prosumer's NIC re-registers | Refused with `E4001` — reactivation is the path, not re-registration |
| EC-6 | Prosumer edits profile while Backoffice deactivates them | The edit request re-checks status and returns `E4302` |

## 8. Data touched

`Prosumers` (CRUD), `EnergyReservation` (bulk cancel on BR-04), `EnergyBookingSlots` (capacity restore),
SQLite `profile_cache`, `session`.

## 9. API surface

`api-contract.md` §3 — register, list, pending queue, get, update, deactivate, activate.

## 10. UI surface

- **Android**: Register screen, Profile view, Profile edit, Deactivate confirmation dialog.
- **Web**: Prosumers list (status filter + search + paging), Pending activations view, Prosumer detail
  with Activate / Deactivate actions.

## 11. Out of scope

Prosumer-facing password change, document upload / NIC image verification, prosumer deletion.

## 12. Open questions / decisions

- Resolved: NIC is `_id` → ADR-002.
- Resolved: registration is gated (`PendingActivation` first) → ADR-002 §4. This is what gives the
  rubric's "pending activation view in web app (2 marks)" something to display.
