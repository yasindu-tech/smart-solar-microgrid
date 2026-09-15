## What and why

<!-- One short paragraph. What changed, and which requirement it serves. -->

Implements: <!-- REQ-RESV-2, BR-07, UF-04 - cite the document IDs -->
Closes: <!-- #issue, or "none" -->

## Component

<!-- api | web | mobile | docs | repo -->

## How to verify

<!-- Exact steps a reviewer can follow. "Test it" is not enough. -->

```
1.
2.
3. Expected:
```

---

## Business rule checklist

Only tick what you actually verified. If a rule is not touched by this PR, write
`N/A` with a reason rather than ticking it.

- [ ] **Enforced in the API** — every rule this PR adds or changes is implemented in a service class, not in the web or Android client (BR-14, ADR-004)
- [ ] **Documented error code** — each rule violation returns a code from `docs/architecture/api-contract.md` §9, not a bare 500
- [ ] **Boundary tested** — if this PR touches a time-window rule, the exactly-at-the-limit case is covered by a test (7.0 days, 12.0 hours)
- [ ] **State transitions** — any status write goes through `ReservationStateMachine.AssertTransition`
- [ ] **Ownership check** — endpoints returning or changing a prosumer's data verify the caller owns it (BR-02)
- [ ] **Knowledge base updated** — if behaviour changed, the relevant `docs/` file changed in this same PR

## Security and hygiene

- [ ] No `appsettings.Development.json`, `.env`, `local.properties`, keystore or API key committed
- [ ] No connection string, signing secret or Maps key hard-coded in source
- [ ] No `bin/`, `obj/`, `node_modules/`, `dist/` or build output committed

## Standard checks

- [ ] Base branch is `develop`
- [ ] Branch name follows `<type>/<component>/<description>`
- [ ] PR title is a Conventional Commit (the squash merge uses it)
- [ ] CI green — Backend Build, Web Build, Mobile Build
- [ ] Comment header block on every new `.cs` file, inline comment at the start of every method
- [ ] Screenshot attached if this PR changes a UI

## Screenshots

<!-- Required for any UI change. These go into the report later, so capture them properly now. -->

## Notes for the reviewer

<!-- Anything you are unsure about, or deliberately left out of scope. -->
