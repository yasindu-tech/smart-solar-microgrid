#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  protect-branches.sh
#
#  Blocks direct commits to main and develop. After this runs, EVERY change to
#  either branch must arrive through a pull request whose CI has passed -
#  including changes by the repository owner (enforce_admins is true).
#
#  Usage:
#      ./scripts/protect-branches.sh                 # infers the repo from origin
#      REPO=owner/name ./scripts/protect-branches.sh
#
#  Re-run this any time you rename a CI job, because required status checks are
#  matched by job NAME. The names below must match .github/workflows/*.yml.
# ---------------------------------------------------------------------------
set -euo pipefail

say()  { printf '\n\033[1m==> %s\033[0m\n' "$1"; }
warn() { printf '\033[33m    %s\033[0m\n' "$1"; }
die()  { printf '\033[31mERROR: %s\033[0m\n' "$1" >&2; exit 1; }

command -v gh >/dev/null || die "GitHub CLI (gh) is not installed."
gh auth status >/dev/null 2>&1 || die "gh is not authenticated. Run: gh auth login"

# Work out which repository to protect.
if [ -n "${REPO:-}" ]; then
  TARGET="$REPO"
else
  TARGET="$(gh repo view --json nameWithOwner --jq .nameWithOwner 2>/dev/null || true)"
  [ -n "$TARGET" ] || die "Could not determine the repository. Set REPO=owner/name."
fi

# These strings must exactly match the job `name:` values in the workflows.
# If CI is renamed and this is not updated, protection silently stops gating.
read -r -d '' PROTECTION <<'JSON' || true
{
  "required_status_checks": {
    "strict": true,
    "contexts": [
      "Backend Build",
      "Web Build",
      "Mobile Build",
      "Guard - no secrets or build output",
      "Guard - PR title is a Conventional Commit"
    ]
  },
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "dismiss_stale_reviews": true,
    "require_code_owner_reviews": false,
    "required_approving_review_count": 0
  },
  "restrictions": null,
  "required_linear_history": true,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "required_conversation_resolution": true
}
JSON

protect() {
  local branch="$1"
  say "Protecting $TARGET:$branch"

  if printf '%s' "$PROTECTION" | gh api \
      -X PUT "repos/$TARGET/branches/$branch/protection" \
      -H "Accept: application/vnd.github+json" \
      --input - >/dev/null 2>/tmp/protect-err.txt; then
    echo "    protected"
  else
    warn "Could not protect $branch:"
    sed 's/^/    /' /tmp/protect-err.txt
    warn ""
    warn "Most common causes:"
    warn "  - The branch does not exist on the remote yet. Push it first."
    warn "  - The repository is PRIVATE on a free plan. Branch protection needs"
    warn "    GitHub Pro/Team for private repos. Either make the repo public"
    warn "    (gh repo edit $TARGET --visibility public --accept-visibility-change-consequences)"
    warn "    or upgrade the plan. GitHub Free gives students Pro via the"
    warn "    Student Developer Pack: https://education.github.com/pack"
    warn "  - Your token lacks the 'repo' scope. Run: gh auth refresh -s repo"
    return 1
  fi
}

FAILED=0
protect main    || FAILED=1
protect develop || FAILED=1

if [ "$FAILED" -eq 0 ]; then
  say "Verifying"
  for b in main develop; do
    echo "  $b:"
    gh api "repos/$TARGET/branches/$b/protection" \
      --jq '"    admins enforced: \(.enforce_admins.enabled)
    force pushes:    \(.allow_force_pushes.enabled)
    required checks: \(.required_status_checks.contexts | join(", "))"'
  done

  cat <<EOF

  Direct commits to main and develop are now rejected for everyone, including
  you. From here on:

      git switch develop && git pull
      git switch -c feature/<component>/<description>
      # work, commit
      git push -u origin feature/<component>/<description>
      gh pr create --base develop

EOF
else
  die "One or more branches could not be protected. See the guidance above."
fi
