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

## 4a. UI design system — strict, not a suggestion

**`docs/architecture/ui-design-system.md` is mandatory for every piece of UI code, web and
Android.** Read it before writing or reviewing any component, screen, or style file. The short
version:

- **Web:** `web/src/styles/base.css` is the only place UI colors, typography, spacing, radius and
  shadows are defined. Use its `var(--sg-*)` tokens and `.sg-*` shared classes. Never hardcode a hex
  value or a Tailwind color utility (`bg-blue-500` etc.) in a page or component — if the token you
  need doesn't exist, that's a `base.css` change, not a one-off style.
- **Android:** the equivalent tokens live in `mobile/app/src/main/res/values/colors.xml` and
  `dimens.xml` (`@color/sg_primary`, `@dimen/sg_space_4`, etc.), kept numerically in sync with
  `base.css` by hand. Never hardcode a hex or `dp` literal in a layout or Composable.
- Reuse a shared component before writing new CSS/XML. If two or more screens need the same visual
  pattern, it belongs in the shared layer, not duplicated per page.
- A PR that introduces a new hardcoded color, a duplicated card/button/table style, or a missing
  loading/empty/error state is a defect against this document, the same way an unenforced business
  rule is a defect against `BR-register.md`.
- Logo: `docs/assets/logo.png` is the master file (also at `web/public/logo.png` and
  `mobile/app/src/main/res/drawable/logo.png`). Regenerate Android launcher icons from it via Android
  Studio's Image Asset Studio — don't hand-copy it into `mipmap-*`.

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

