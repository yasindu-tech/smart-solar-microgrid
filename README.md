# Smart Solar Microgrid Trading System

SE4040 Enterprise Application Development · Year 4 Semester 2, 2026 · Group of 4

An end-to-end client–server system for reserving and verifying solar energy
transfers at microgrid hubs. A C# Web API holds all business logic; a React web
app serves back-office and grid-operator staff; a native Android app serves
prosumers and operators in the field.

---

## Repository layout

```
backend/          C# ASP.NET Web API — ALL business logic lives here
  src/SmartSolar.Api/
    Controllers/    thin: bind, authorise, call a service, map the result
    Services/       every business rule (BR-*) is enforced in here
    Repositories/   MongoDB access only — no rules
    Models/ DTOs/ Middleware/ Configuration/
  tests/            boundary tests for the 7-day and 12-hour rules

web/              React + Vite + TypeScript + Tailwind — UI layer only
  src/api/          the single place the web app calls the API
  src/features/     one folder per feature area, owned per CODEOWNERS
  src/pages/ components/ hooks/ lib/

mobile/           Pure native Android, Kotlin — UI layer + SQLite cache
  app/src/main/java/lk/sliit/smartsolar/
    data/local/     the four SQLite tables (ADR-004) — cache only
    data/remote/    Retrofit client
    ui/auth ui/prosumer ui/operator ui/map

docs/             The requirements knowledge base. Start at docs/README.md
scripts/          GitHub bootstrap, branch protection, git hooks
seed/             MongoDB sample data for the demo
.claude/skills/   open-pr and requirements-analyst skills for the team
.github/          CI workflows, CODEOWNERS, PR and issue templates
```

## Getting started

```bash
git clone https://github.com/<owner>/smart-solar-microgrid.git
cd smart-solar-microgrid
./scripts/install-hooks.sh        # blocks direct pushes and bad commit messages
git switch develop
```

### Backend

```bash
cp backend/src/SmartSolar.Api/appsettings.Development.json.example \
   backend/src/SmartSolar.Api/appsettings.Development.json
# fill in the Mongo connection string and the two signing secrets
dotnet restore backend/SmartSolar.sln
dotnet run   --project backend/src/SmartSolar.Api
dotnet test  backend/SmartSolar.sln
```

### Web

```bash
cd web
cp .env.example .env
npm install
npm run dev
```

### Mobile

Open `mobile/` in Android Studio, or:

```bash
cd mobile
gradle assembleDebug
```

The emulator reaches a locally hosted API at `http://10.0.2.2:<port>`, not
`localhost`.

## How we work

| Thing | Where it is written |
|---|---|
| Why we are building this, and what is out of scope | `docs/BRD.md` |
| What a feature should do, in every case | `docs/requirements/` |
| Whether something is allowed, and the limits | `docs/business-rules/BR-register.md` |
| What happens end to end when a user does X | `docs/user-flows/` |
| Endpoints, fields, error codes, statuses | `docs/architecture/` |
| Why a decision went the way it did | `docs/decisions/` |
| Who owns what, and the 17-day plan | `docs/plan/` |
| Branching, commits, PRs | `.claude/skills/open-pr/SKILL.md` |

**Branch from `develop`. Never commit to `main` or `develop` directly** — both
are protected for everyone, including the repo owner.

```bash
git switch develop && git pull
git switch -c feature/<component>/<description>
git commit -m "feat(resv): enforce 12-hour cancellation window (BR-07)"
git push -u origin feature/<component>/<description>
gh pr create --base develop
```

## Business domains — component ownership

Four domains, one per member, each a full vertical slice (API + web + mobile).
See `docs/domains/README.md` for goals, cross-domain handoffs and the full
mark reconciliation.

| Domain | Member | Owns | Individual marks |
|---|---|---|---|
| D1 — Identity & Access Management | M1 | Auth, RBAC, web user mgmt, prosumer mgmt | 17 |
| D2 — Microgrid Inventory Management | M2 | Node CRUD, slot admin, **map + Google Maps** | 20 |
| D3 — Reservation & Scheduling Engine | M3 | Reservation lifecycle, 7-day/12-hour rules, **prosumer booking views** | 15 |
| D4 — Fulfillment & Operational Verification | M4 | QR verification, operator mode, operator dashboard, SQLite | 13 |

Full day-by-day split in `docs/plan/work-breakdown.md`. Enforced by `.github/CODEOWNERS`.

## CI

| Check | Runs when | Required to merge |
|---|---|---|
| Backend Build | `backend/**` changes | ✅ |
| Web Build | `web/**` changes | ✅ |
| Mobile Build | `mobile/**` changes | ✅ |
| Guard — no secrets or build output | every PR | ✅ |
| Guard — PR title is a Conventional Commit | every PR | ✅ |

## AI use

This assignment is set at **AI Assessment Scale Level 2 — AI Planning**. AI was
used for planning, requirements structuring and research only. All
implementation is the team's own work. Each member's use is disclosed in their
individual contribution section of the report. See `CLAUDE.md` §7.
