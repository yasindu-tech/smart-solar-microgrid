# Smart Solar Microgrid Trading System — agent instructions

SE4040 Enterprise Application Development. Group of 4. Deadline **30 Sep 2026**.

## 1. Read these first

- `docs/project-config.md` — components, actors, hard constraints, Definition of Done
- `docs/business-rules/BR-register.md` — every invariant, numbered
- `docs/architecture/api-contract.md` — the frozen interface between components

If you are about to decide how something should behave, stop and use the
`requirements-analyst` skill instead of deciding in code.

## 2. The architectural constraint that carries the most marks

**All business logic lives in the C# Web API.** The React web app and the
Kotlin Android app are UI layers. They may disable a button as a hint; they may
never be the place a rule is enforced. SQLite on Android caches for display
only — see `docs/decisions/ADR-004-sqlite-scope-on-android.md`.

If you find yourself writing date arithmetic, a status comparison or a capacity
check in `web/` or `mobile/` that *blocks* an action, it belongs in
`backend/src/SmartSolar.Api/Services/`.

## 3. The two time rules use different anchors

```
BR-05  slot start <= reservation.createdAt + 7 days     (checked at creation)
BR-06  slot start -  DateTime.UtcNow >= 12 hours        (checked at update)
BR-07  slot start -  DateTime.UtcNow >= 12 hours        (checked at cancel)
```

Both boundaries are inclusive. Do not write one helper for both. See
`backend/src/SmartSolar.Api/Services/ReservationRules.cs`.

## 4. Code conventions

**Every `.cs` file needs a header comment block. Every method needs an inline
comment at the start.** This is not style — the assignment states that code
without it is not marked. Copy the format from `ReservationRules.cs`.

- C#: services throw `DomainException` with a code from `api-contract.md` §9.
  Controllers bind, authorise, call a service, map the result. No domain `if`
  in a controller.
- TypeScript: strict mode on. All API calls go through `web/src/api/client.ts`.
- Kotlin: pure native Android, no cross-platform framework. Local persistence
  limited to the four tables in `ADR-004`.

## 5. Branching, commits and PRs

Use the `open-pr` skill — it is the full policy. Short version:

- Never commit to `main` or `develop`. Both are protected for everyone.
- Branch `<type>/<component>/<description>` from `develop`.
- Commit `<type>(<scope>): <summary>`, citing the REQ or BR id.
- Squash merge into `develop` once CI is green.

Run `./scripts/install-hooks.sh` once after cloning.

## 6. Never commit

Connection strings, JWT or QR signing secrets, Google Maps keys, keystores,
`appsettings.Development.json`, `local.properties`, `google-services.json`, or
any build output. CI blocks the obvious cases; that is a net, not a substitute
for looking.

## 7. AI use policy — read this, it is assessed

This assignment is set at **AI Assessment Scale Level 2 — AI Planning**. AI may
be used for planning, brainstorming and research. **The implementation must be
written by the team.** Each member discloses the AI tools they used in planning
in their individual contribution section, and must be able to explain and
justify every design decision at the viva.

So: use an agent to query and maintain `docs/`, to review a diff against the
documented rules, to draft a PR description. Do not use one to write the C#,
Kotlin or React that gets submitted.
