#!/usr/bin/env bash
# Installs the local git hooks. Every member runs this once after cloning.
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
git config core.hooksPath scripts/hooks
chmod +x "$ROOT"/scripts/hooks/*

echo "Hooks installed:"
echo "  pre-push   blocks direct pushes to main and develop"
echo "  commit-msg enforces Conventional Commits"
