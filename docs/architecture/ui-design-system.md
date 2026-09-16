# UI Design System — SunGrid

Status: **Agreed — strict**. This is not a style guide to consult occasionally; it is a rule every
piece of UI code (web and Android) must follow. `CLAUDE.md` §8 points here and says so explicitly.

Brand name: **SunGrid** (Smart Solar Microgrid Trading System). Logo master file:
`docs/assets/logo.png`, also copied to `web/public/logo.png` (web) and
`mobile/app/src/main/res/drawable/logo.png` (Android — see §22 for launcher-icon generation).

---

## 1. Purpose

This repository contains application UI that must follow **one shared visual identity**.

The design system is defined in:

```
web/src/styles/base.css
```

Always use `base.css` as the source of truth for UI colors, typography, spacing, radius, shadows,
and shared visual primitives on the **web** client. The **Android** client does not share this file
directly (XML can't `@import` CSS), so it gets an equivalent token set — see §22 — that must stay in
numeric/hex lockstep with `base.css`. If the two ever drift, `base.css` wins and the Android tokens
are corrected to match, not the other way round.

## 2. NON-NEGOTIABLE DESIGN RULE

**Use only the SunGrid design tokens.** Do not invent or hardcode new UI colors in feature code.

Bad:

```css
color: #123456;
background: #f7f7f7;
border-color: #abc123;
```

Good:

```css
color: var(--sg-text);
background: var(--sg-surface);
border-color: var(--sg-border);
```

Use the tokens already defined in `base.css`. If a visual requirement cannot be represented by the
existing tokens, first determine whether an existing token is appropriate. Only add a new token to
`base.css` when it represents a genuinely reusable design-system value. Never create one-off colors
inside a page or component.

## 3. Source of truth

`base.css` is the shared design-system source of truth for the web client. Before writing new CSS:

1. Check `base.css`.
2. Reuse an existing token.
3. Reuse an existing shared class/component.
4. Only if no suitable pattern exists, create a new shared style.
5. Put reusable styles in the shared design-system/component layer (`web/src/styles/` or
   `web/src/components/`), never inline in a page file.
6. Do not create duplicated page-specific styles for the same visual pattern.

## 4. Brand theme

SunGrid uses:

- **Grid Blue** (`--sg-primary`) for primary interaction and technology
- **Solar Green** (`--sg-secondary`) for renewable energy and success
- **Solar Yellow** (`--sg-accent`) for solar/energy highlights
- **Energy Cyan** (`--sg-cyan`) for information/connectivity
- **Slate neutrals** for most surfaces and text

The UI should remain mostly neutral with controlled use of brand colors. Do not turn entire pages
into saturated blue, green, or yellow layouts.

## 5. Color rules

Only use colors from `base.css`.

**Primary:** `var(--sg-primary)` `var(--sg-primary-hover)` `var(--sg-primary-soft)`

**Secondary / Energy:** `var(--sg-secondary)` `var(--sg-secondary-hover)` `var(--sg-secondary-soft)`

**Solar accent:** `var(--sg-accent)` `var(--sg-accent-soft)`

**Cyan:** `var(--sg-cyan)` `var(--sg-cyan-soft)`

**Semantic:**

```
--sg-success / --sg-success-hover / --sg-success-soft / --sg-success-text
--sg-warning / --sg-warning-hover / --sg-warning-soft / --sg-warning-text
--sg-danger  / --sg-danger-hover  / --sg-danger-soft  / --sg-danger-text
--sg-info    / --sg-info-hover    / --sg-info-soft    / --sg-info-text
```

**Neutrals:**

```
--sg-bg / --sg-surface / --sg-surface-muted / --sg-border / --sg-border-strong
--sg-text / --sg-text-secondary / --sg-text-muted / --sg-text-disabled / --sg-white
```

Do not use arbitrary hex, RGB, HSL, named colors, Tailwind color literals, or framework color classes
when a SunGrid token exists. (Tailwind's own utility classes for layout — flex, grid, spacing — are
still fine; it's specifically *color* utilities like `bg-blue-500` that are banned in favour of
`var(--sg-*)` or the `.sg-*` classes below.)

## 6. Shared components

Use shared components instead of rebuilding UI patterns. The shared component library should
include, where applicable: Button, IconButton, Input, PasswordInput, SearchInput, Select,
MultiSelect, Textarea, Checkbox, Radio, Switch, Card, MetricCard, Badge, StatusBadge, Alert, Toast,
Modal, ConfirmationModal, Dropdown, Tabs, Table, Pagination, Avatar, Tooltip, Spinner, Skeleton,
EmptyState, ErrorState, DatePicker, TimePicker, DateTimePicker, FilterBar, Drawer, BottomSheet,
Breadcrumb, PageHeader, LoadingState, FormField, Divider.

If a shared component already exists, do not create another version of it. On the web client these
live under `web/src/components/` — build that folder up as components are actually needed; don't
pre-build the whole list speculatively.

## 7. New styles must be shared

Do **not** do this:

```
pages/
  booking/
    booking.css
  station/
    station.css
  users/
    users.css
```

with duplicated button/card/form styles per page.

**Do this** — create or extend a shared component/style:

```
web/src/
  styles/
    base.css
  components/
    Button/
    Card/
    ...
```

The principle: if two or more screens can reasonably use the same style, it belongs in the shared
layer.

## 8. Buttons

Use the shared Button component. Supported visual variants: `primary`, `secondary`, `success`,
`danger`, `ghost`, `link`, `icon` (`.sg-btn-primary`, `.sg-btn-secondary`, etc. in `base.css`).

Do not create `BlueButton`, `GreenButton`, `SaveButton`, `CustomButton`, `BookingButton`,
`StationButton` when they are visually the same as an existing Button variant.

**Business meaning determines the variant:**

| Action | Variant |
|---|---|
| Create Booking / Save Changes | `primary` |
| Approve Booking / Complete Transfer | `success` |
| Cancel (destructive) / Delete | `danger` |
| View Details | `secondary` / `ghost` |

## 9. Toast rules

Toast is a shared/global component. Use it for short-lived feedback **after a completed action** —
e.g. "Booking created successfully.", "Station updated successfully.", "Unable to save changes.",
"Network connection lost."

Use the shared semantic variants (`.sg-toast-success/-warning/-danger/-info`). Do not create custom
toast colors, duplicate Toast CSS inside feature pages, use Toast as a substitute for field
validation, or show duplicate toasts for the same event.

## 10. Alerts

Use Alert (`.sg-alert-*`) for persistent contextual information inside a page. Variants: success,
warning, danger, info. Use semantic tokens from `base.css`.

## 11. Modals

Use the shared Modal component (`.sg-modal*`). Use ConfirmationModal for destructive or important
actions — "Delete Station?", "Deactivate User?", "Cancel Reservation?". Do not build one-off modal
styling inside individual screens.

## 12. Forms

Use shared form components. Every form field should support: label, input, helper text, validation
message, disabled state, error state, success state where appropriate.

Use `var(--sg-border)`, `var(--sg-border-strong)`, `var(--sg-border-focus)`, `var(--sg-danger)`,
`var(--sg-success)`. Do not invent input colors.

## 13. Status badges

Use the shared StatusBadge component (`.sg-badge-*`). Business status mapping:

| Status | Variant |
|---|---|
| APPROVED, COMPLETED | success |
| PENDING | warning |
| CANCELLED | danger |
| ACTIVE | primary |
| INACTIVE | neutral |

This maps directly onto the reservation/node/prosumer states in
`docs/architecture/state-machines.md` — use that document for which states exist, this section for
which color each one gets. Do not create custom badge colors for individual screens.

## 14. Tables

Use the shared Table component (`.sg-table*`) for desktop data-heavy views. Tables should support:
loading, empty state, error state, sorting, filtering, pagination, row actions, status badges. Do not
create a new table style per page.

## 15. Loading states

Never leave an API-driven screen blank while data is loading. Use shared Spinner, Skeleton,
LoadingState, or a Button loading state (`.sg-spinner`, `.sg-skeleton`, `.sg-btn-loading`). Never
create a new loading animation for a single page.

## 16. Empty states

Every data collection that can legitimately contain zero records should use a shared EmptyState
(`.sg-empty`), e.g.:

```
No reservations yet
Your reservations will appear here.
[ Create Reservation ]
```

Do not display an unexplained blank area.

## 17. Error states

Use the shared ErrorState (`.sg-error-state`) for page/data loading failures. Do not expose stack
traces, raw exceptions, internal server messages, or database errors — map every `E****` code from
`docs/architecture/api-contract.md` §9 to a user-friendly message instead. Provide a retry action
where appropriate.

## 18. Typography

Use typography tokens from `base.css`. Do not introduce arbitrary font sizes or weights. Use the
shared classes: `.sg-display` `.sg-h1` `.sg-h2` `.sg-h3` `.sg-h4` `.sg-body` `.sg-body-lg`
`.sg-body-sm` `.sg-caption` `.sg-label`.

## 19. Spacing

Use the shared spacing tokens: `--sg-space-1` through `--sg-space-16`. Do not use arbitrary spacing
values when a token provides the required spacing.

## 20. Cards and surfaces

Use the shared Card component (`.sg-card`, `.sg-card-body`, `.sg-card-header`, `.sg-card-footer`). Do
not create a different card style for stations, users, bookings, or dashboard metrics — use
variants/composition of the same shared Card system.

## 21. Responsive design

The web UI must be responsive. Desktop can use tables, sidebar, dense layouts, dashboard grids.
Mobile (the web app viewed on a phone) should adapt using cards, lists, stacked forms, bottom
navigation, drawers, bottom sheets. Do not simply shrink desktop components. `base.css` already ships
breakpoints at 1024px and 768px for the grid/table/modal/toast patterns — extend those, don't
introduce new ones per page.

## 22. Mobile / Android consistency

The native Android application must visually match the web application. The Android implementation
should use **equivalent design tokens** for colors, typography, spacing, radius, status colors,
button hierarchy, and component behaviour. Do not invent a completely separate Android theme. The
platform may use native Android interaction patterns, but the SunGrid visual identity must remain
recognizable.

Concretely, in this repo:

- `mobile/app/src/main/res/values/colors.xml` — the Android color tokens, numerically identical to
  `base.css`'s `--sg-*` custom properties (`sg_primary`, `sg_secondary`, `sg_accent`, `sg_cyan`, the
  semantic success/warning/danger/info sets, and the neutrals). Created alongside this document —
  keep it in sync by hand whenever `base.css` changes; there is no build step that generates one from
  the other.
- `mobile/app/src/main/res/values/dimens.xml` — the Android spacing/radius tokens, matching
  `--sg-space-*` and `--sg-radius-*` (converted from `rem`/`px` to `dp`).
- Compose or XML layouts reference these (`@color/sg_primary`, `@dimen/sg_space_4`), never a literal
  hex or `dp` value, for exactly the same reason §2 bans literal colors in web CSS.
- **Launcher icon:** `mobile/app/src/main/res/drawable/logo.png` is the master logo file, not a
  ready-made launcher icon. Run it through Android Studio's *Image Asset Studio*
  (right-click `res/` → New → Image Asset) to generate the full `mipmap-*` density set
  (`ic_launcher.png` / `ic_launcher_round.png`) — don't hand-copy one PNG into every `mipmap-*`
  folder, the OS expects genuinely different pixel sizes per density bucket.

## 23. Accessibility

All shared components must consider: keyboard navigation on web, visible focus states (`base.css`
sets `:focus-visible` globally — don't remove it), readable contrast, accessible labels,
touch-friendly mobile controls, screen-reader semantics, color-independent status communication.
Never communicate an important state using color alone — pair a status badge's color with its text
label (already the pattern in §13), not color alone.

## 24. Icons

Use one consistent icon family per client. Do not mix unrelated icon styles. Icon-only buttons
(`.sg-btn-icon`) must have an accessible label (`aria-label` on web, `contentDescription` on
Android). Do not use decorative icons where they make the interface harder to understand.

## 25. Animations

Keep animations subtle. Use the motion tokens in `base.css` (`--sg-duration-*`, `--sg-ease`). Do not
introduce excessive page animations, bouncing UI, unnecessary transitions, or decorative animations.
Respect `prefers-reduced-motion` — `base.css` already has a global media query for this; don't bypass
it with `!important` animations in feature code.

## 26. Business UI behaviour

UI must reflect the real result of the API — never show a success state before the server confirms
the operation. This is the same rule as NFR-3/NFR-4 in `system-overview.md` §7, expressed as a UI
flow:

```
Request
  -> Loading state
  -> Server response
       success        -> update UI + success toast
       validation err  -> field/context error
       401 unauthorized -> auth handling (redirect to login)
       403 forbidden    -> permission error (ErrorState, not a blank page)
       network error    -> retry / offline feedback
       5xx server error -> user-friendly ErrorState
```

## 27. Do not over-engineer the UI

Before adding a new component, ask: does an existing shared component already solve this? Can an
existing variant solve it? Can this be composed from existing components? Only if not, and only if
the new thing is genuinely reusable, create a new component.

## 28. Definition of done for UI work

A UI task is not done until it:

- Uses `base.css` tokens (web) or the matching `colors.xml`/`dimens.xml` tokens (Android)
- Uses shared components, not one-off styling
- Does not hardcode new colors
- Does not duplicate existing styles
- Handles loading, empty, error, and success states
- Handles disabled states
- Works responsively (web) / on at least one phone and one tablet size (Android)
- Has accessible labels and focus states
- Matches the SunGrid visual identity

This is in addition to, not instead of, the general Definition of Done in `project-config.md` §8.

## 29. Final rule

Build the UI as a design system, not as a collection of individually styled pages. If a new style is
created and another screen could reuse it, move it into the shared component/design-system layer.
`base.css` is the source of truth. Reuse first. Extend shared styles second. Create page-specific
styles only when there is genuinely page-specific behaviour.
