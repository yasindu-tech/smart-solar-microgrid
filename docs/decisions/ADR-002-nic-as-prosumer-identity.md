# ADR-002 · NIC as Prosumer Identity

Status: **Accepted** · Date: 2026-09-13 · Deciders: whole group

## Context

The assignment states twice that prosumers are managed "using National Identity Card (NIC) as the
primary key". MongoDB gives every document an `ObjectId` `_id` by default. We had to decide whether
NIC is the actual `_id`, or merely a unique indexed field alongside a generated id.

We also had to decide whether a newly registered prosumer is immediately usable.

## Decision

### 1. NIC **is** the `_id`

```jsonc
{ "_id": "199512345678", "nic": "199512345678", ... }
```

`nic` is duplicated as a named field for query readability and for driver/serialisation comfort, but
there is one identity, not two. Reservations reference prosumers by `nic`.

### 2. NIC is immutable

Once registered, the NIC cannot be changed by anyone, including Backoffice. Changing it would orphan
every reservation. A mistyped NIC is handled by deactivating that account and registering correctly.

### 3. NIC is the prosumer's **login identifier**

Prosumers log in with NIC + password. Web users log in with email + password. One endpoint, two lookup
paths, chosen by which collection the identifier matches.

### 4. Registration produces `PendingActivation`, not `Active`

A self-registered prosumer cannot log in until a Backoffice officer approves them.

### 5. Collection naming

Web accounts live in `Users`; prosumers live in `Prosumers`. The rubric's "User's detail" collection
maps to `Prosumers`. This is stated in the report's database design section so the marker can map it.

## Alternatives considered

| Option | Why rejected |
|---|---|
| `ObjectId` `_id` + unique index on `nic` | Works, but the assignment says NIC *is* the primary key, and the rubric awards marks for exactly that. Two identities also invite bugs where one screen sends the ObjectId and another sends the NIC. |
| NIC as `_id` with **no** duplicate `nic` field | Queries and DTOs read worse (`_id` everywhere), and the mobile client would have to know that `_id` means NIC. Cheap duplication, better readability. |
| Registration goes straight to `Active` | Simpler — but the rubric explicitly awards 2 individual marks for a **pending activation view in the web app**. With auto-activation that screen has nothing to show. Gating registration creates the queue the rubric is asking for. |
| Allow NIC edit by Backoffice | Every reservation stores `nic` as the link. An edit would need a cascading update across collections with no transaction guarantee. Not worth it in 17 days. |

## Consequences

**Good**
- Directly satisfies an explicitly marked requirement.
- The pending-activation queue gives the web app a genuine Backoffice workflow to demonstrate.
- No ambiguity about which value identifies a prosumer.

**Bad / accepted**
- NIC appears in URLs (`GET /prosumers/199512345678`). It is a personal identifier in a path. Acceptable
  for a coursework project, and worth **mentioning in the report's challenges section** as a real-world
  privacy consideration we identified — that is exactly the kind of reflection the Excellent
  documentation band asks for.
- Both Sri Lankan NIC formats (old `951234567V`, new 12-digit) must be accepted. Regex in REQ-PROS EC-1.

## Follow-ups
- Validation regex lives in one constant in the API, referenced by both clients' hint text.
