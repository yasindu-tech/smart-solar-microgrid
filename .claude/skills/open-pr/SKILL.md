---
name: "open-pr"
description: "Open a pull request in the smart-solar-microgrid repo, or explain the repo's branching, commit and PR policy. Always branches from and targets develop, and fills in the PR template including the business-rule checklist. Use when the user says \"raise a PR\", \"open a PR\", \"push this as a PR\", asks to submit work for review in this repo, or asks how branching, commits or releases work here."
---

## Why this skill exists

This repo has no CONTRIBUTING.md on purpose. The branching model, commit
convention and PR rules live here, as something an agent actively applies
rather than a document that goes stale halfway through a 17-day build. If you
are asked to explain the process rather than execute it, answer from this
skill directly.

## Non-negotiables

1. **Base branch is always `develop`.** The only exception is `release/*`,
   which targets `main`. If unsure, it is `develop`.
2. **Never commit directly to `main` or `develop`.** Both are protected on
   GitHub with `enforce_admins: true` — this applies to the repo owner too.
   A local `pre-push` hook refuses it as well (`scripts/install-hooks.sh`).
3. **The PR template gets filled in, not left blank.** Especially the business
   rule checklist.
4. **Never tick a checklist box you have not verified.** Write `N/A — <reason>`
   instead, or leave it unticked with a note.
5. **Never commit a credential.** Connection strings, JWT and QR signing
   secrets, Google Maps keys, keystores. The `Guard - no secrets` CI job blocks
   the obvious cases; it is not a substitute for looking.

## Repo policy reference

### Branches

| Branch | Purpose | Protected | From | Into |
|---|---|---|---|---|
| `main` | Submission-ready. Tagged at each milestone. | ✅ | `release/*` | — |
| `develop` | Integration. Default branch. | ✅ | `feature/*`, `fix/*`, `docs/*` | `release/*` |
| `feature/<component>/<desc>` | New work | ❌ | `develop` | `develop` |
| `fix/<component>/<desc>` | Bug fix | ❌ | `develop` | `develop` |
| `docs/<desc>` | Docs only | ❌ | `develop` | `develop` |
| `release/<version>` | Milestone stabilisation | ❌ | `develop` | `main` + `develop` |
| `spike/<desc>` | Throwaway experiment, never merged | ❌ | any | — |

Keep branches short-lived — days, not weeks. With a 30 September deadline, a
branch that lives a week is a branch that conflicts.

### Naming

`<type>/<component>/<short-kebab-description>`

Components: `api` · `web` · `mobile` · `auth` · `user` · `prosumer` · `node` ·
`slot` · `resv` · `qr` · `view` · `docs` · `ci` · `repo`

```
✅ feature/resv/twelve-hour-cancellation-guard
✅ fix/node/block-deactivation-with-active-reservations
✅ docs/adr-005-slot-capacity-at-approval
❌ my-branch   ❌ yasindu-work   ❌ fix
```

### Commits — Conventional Commits

```
<type>(<scope>): <imperative summary, lower case, no trailing period>
```

Types: `feat` `fix` `docs` `test` `refactor` `perf` `chore` `ci`.
Scopes: the component list above.

**Cite the requirement or rule ID.** The marking scheme rewards meaningful
descriptive commits, and IDs are the cheapest way to get there.

```
✅ feat(resv): enforce 12-hour cancellation window (BR-07, REQ-RESV AC-5)
✅ fix(node): block deactivation when active reservations exist (BR-09)
✅ docs(adr): record QR payload decision (ADR-003)
❌ update stuff   ❌ WIP   ❌ final fix
```

The `commit-msg` hook enforces the format locally; the `Guard - PR title` CI
job enforces it on the PR title, which becomes the squash-merge message.

### PR rules

| Rule | Value |
|---|---|
| Target branch | `develop` |
| Approvals required | 0 — self-merge allowed |
| CI | Must pass: Backend Build, Web Build, Mobile Build, both Guards |
| Conversations | All resolved before merge |
| Merge method | Squash and merge |
| Direct pushes to `main`/`develop` | Rejected by GitHub, for everyone |

Required approvals is 0 because GitHub disables the Approve button on your own
PR and there is no self-approval setting. What still protects the branch: a PR
is required, CI must pass, conversations must resolve, no force-push or
deletion. **Still request a real review** for anything touching
`docs/architecture/`, `docs/business-rules/`, `backend/src/SmartSolar.Api/Services/`
or `.github/` — CODEOWNERS will request it automatically.

Keep PRs under ~400 changed lines where practical; say why in the description
if larger.

### Releases

```bash
git switch -c release/v1.0-submission develop
# stabilise: docs, screenshots, version bumps, no new features
# PR release/v1.0-submission -> main, squash merge
git tag -a v1.0-submission -m "SE4040 Assignment 1 submission"
git push origin v1.0-submission
# then PR main -> develop to carry back any release fixes
```

## Steps to open a PR

1. **Orient.** `git rev-parse --abbrev-ref HEAD`, `git status`,
   `git log --oneline -5`.

2. **Branch, if not already on one.** `git switch develop && git pull origin develop`,
   then `git switch -c <type>/<component>/<desc>`. If `develop` does not exist
   or cannot be pulled, stop and tell the user — do not improvise a base.

3. **Scan the diff before committing.** Refuse and tell the user if it touches
   `appsettings.Development.json`, `.env`, `local.properties`,
   `google-services.json`, `*.keystore`, `*.jks`, `*.pem`, `*.key`, or any
   `bin/`, `obj/`, `node_modules/`, `dist/`, `build/` path. Also grep added
   lines for a MongoDB Atlas connection string with embedded credentials
   (`mongodb` + `+srv://` + a `user:pass@` segment) or an `AIza`-prefixed
   Maps key.

4. **Commit** using Conventional Commits, citing the REQ/BR id.

5. **Push.** `git push -u origin <branch>`. If this fails on credentials, stop
   and tell the user to push themselves — do not go hunting for tokens.

6. **Fill the template and open the PR.** Read `.github/pull_request_template.md`,
   fill every section, write it to a temp file, then:
   ```bash
   gh pr create --base develop --head <branch> \
     --title "<type>(<scope>): <summary>" --body-file /tmp/pr-body.md
   ```
   If `gh` is unavailable, print the filled body and the compare URL
   (`https://github.com/<owner>/<repo>/compare/develop...<branch>`) and tell
   the user to paste it in.

7. **Report back.** Give the PR URL, and call out anything marked `N/A` or left
   unticked so they can check your judgment before merging.

## Filling the business-rule checklist honestly

Do not tick because it is plausible. Concrete things to check:

| Checklist item | What to actually check |
|---|---|
| Enforced in the API | Grep the diff's web/mobile files for date arithmetic against `now`, status comparisons, or capacity checks that *block* an action rather than grey a control. Any such logic in a client is a defect (BR-14, ADR-004) |
| Documented error code | Every `throw new DomainException(` in the diff uses a code that exists in `docs/architecture/api-contract.md` §9. A new code means the contract doc changes in the same PR |
| Boundary tested | If `ReservationRules` changed, confirm a test asserts the exactly-at-the-limit case — 7.0 days allowed, 12.0 hours allowed — not only the obvious pass and fail |
| State transitions | Every status assignment goes through `ReservationStateMachine.AssertTransition`, not a bare `reservation.Status = "..."` |
| Ownership check | Endpoints that read or write prosumer data compare the route NIC against the token's `sub` claim (BR-02), not just that the caller is authenticated |
| Knowledge base updated | If an AC or EC in a `docs/requirements/` file no longer matches the code, the doc changed in this PR too |

If the PR is pure infra or docs with no rule code, write
`N/A — no business rule code in this PR` once for the whole section and move on.

## If invoked outside this repo

These specifics are for `smart-solar-microgrid`. Elsewhere, fall back to a
generic flow: check the target repo's CONTRIBUTING or PR template, ask for the
base branch if unclear, and use plain Conventional Commits.
