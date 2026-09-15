#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  bootstrap-github.sh
#
#  Creates the GitHub repository, pushes main and develop, and applies the
#  branch protection that blocks direct commits to both.
#
#  Run this ONCE, from the root of this repo, on a machine where the GitHub
#  CLI is installed and authenticated:
#
#      gh auth login            # if you have not already
#      ./scripts/bootstrap-github.sh
#
#  Everything it does is idempotent enough to re-run if a step fails.
# ---------------------------------------------------------------------------
set -euo pipefail

REPO_NAME="${REPO_NAME:-smart-solar-microgrid}"
VISIBILITY="${VISIBILITY:-private}"
DESCRIPTION="SE4040 Enterprise Application Development - Smart Solar Microgrid Trading System"

say()  { printf '\n\033[1m==> %s\033[0m\n' "$1"; }
warn() { printf '\033[33m    %s\033[0m\n' "$1"; }
die()  { printf '\033[31mERROR: %s\033[0m\n' "$1" >&2; exit 1; }

command -v git >/dev/null || die "git is not installed."
command -v gh  >/dev/null || die "GitHub CLI (gh) is not installed. See https://cli.github.com"
gh auth status >/dev/null 2>&1 || die "gh is not authenticated. Run: gh auth login"

OWNER="$(gh api user --jq .login)"
say "Authenticated as $OWNER"

# --- 1. Initialise the local repository ------------------------------------
if [ ! -d .git ]; then
  say "Initialising local git repository"
  git init -b main
else
  say "Local git repository already exists"
  git symbolic-ref -q HEAD >/dev/null || git checkout -b main
fi

if [ -z "$(git log --oneline -1 2>/dev/null || true)" ]; then
  say "Creating the initial commit"
  git add -A
  git commit -m "chore(repo): scaffold monorepo, CI, docs and requirements knowledge base"
fi

# --- 2. Create the remote repository ---------------------------------------
if gh repo view "$OWNER/$REPO_NAME" >/dev/null 2>&1; then
  say "Remote $OWNER/$REPO_NAME already exists - reusing it"
else
  say "Creating $OWNER/$REPO_NAME ($VISIBILITY)"
  gh repo create "$OWNER/$REPO_NAME" \
    --"$VISIBILITY" \
    --description "$DESCRIPTION" \
    --disable-wiki
fi

if ! git remote get-url origin >/dev/null 2>&1; then
  git remote add origin "https://github.com/$OWNER/$REPO_NAME.git"
fi

# --- 3. Push main and develop ----------------------------------------------
say "Pushing main"
git push -u origin main

say "Creating and pushing develop"
if git show-ref --verify --quiet refs/heads/develop; then
  git checkout develop
else
  git checkout -b develop
fi
git push -u origin develop

say "Setting develop as the default branch"
gh repo edit "$OWNER/$REPO_NAME" --default-branch develop

# --- 4. Apply branch protection --------------------------------------------
say "Applying branch protection"
REPO="$OWNER/$REPO_NAME" ./scripts/protect-branches.sh

# --- 5. Next steps ----------------------------------------------------------
cat <<EOF

  Repository ready: https://github.com/$OWNER/$REPO_NAME

  Do these three things now, before anyone writes code:

  1. Add your three teammates as collaborators with write access:
       gh api -X PUT repos/$OWNER/$REPO_NAME/collaborators/<username> -f permission=push

  2. Replace the placeholder handles in .github/CODEOWNERS with their real
     usernames. A CODEOWNERS file naming a non-collaborator is silently ignored.

  3. Everyone clones and branches from develop:
       git clone https://github.com/$OWNER/$REPO_NAME.git
       git switch develop
       git switch -c feature/<component>/<description>

EOF
