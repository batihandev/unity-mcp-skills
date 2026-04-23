#!/usr/bin/env bash
# Sync the installable paths from `dev` to `main`.
# Refuses to run from any branch other than `dev` with a dirty working tree.
#
# Usage:
#   tools/publish.sh            # sync, commit on main, report
#   tools/publish.sh --dry-run  # print what would change, no commit
set -euo pipefail

DRY_RUN=0
if [[ "${1:-}" == "--dry-run" ]]; then
    DRY_RUN=1
fi

# Paths that ship to main. Everything else stays on dev.
INCLUDE_PATHS=(
    "recipes"
    "skills"
    "references"
    "README.md"
    "SKILL.md"
    "mcp-tools.md"
    "LICENSE"
    ".gitignore"
)

# Branch + cleanliness guards.
current_branch="$(git rev-parse --abbrev-ref HEAD)"
if [[ "$current_branch" != "dev" ]]; then
    echo "error: publish must run from 'dev' (current: $current_branch)" >&2
    exit 2
fi
if [[ -n "$(git status --porcelain)" ]]; then
    echo "error: working tree not clean. commit or stash first." >&2
    exit 2
fi

dev_sha="$(git rev-parse --short HEAD)"
repo_root="$(git rev-parse --show-toplevel)"
worktree="$(mktemp -d -t unity-skills-main-XXXXXX)"
trap 'git worktree remove --force "$worktree" >/dev/null 2>&1 || true; rm -rf "$worktree"' EXIT

git worktree add -f "$worktree" main >/dev/null
echo "main worktree at: $worktree"

# Mirror each include path from dev's tree into main's worktree.
# Using rsync --delete so deletions on dev propagate to main.
for p in "${INCLUDE_PATHS[@]}"; do
    src="$repo_root/$p"
    dst="$worktree/$p"
    if [[ -d "$src" ]]; then
        mkdir -p "$dst"
        rsync -a --delete "$src/" "$dst/"
    elif [[ -f "$src" ]]; then
        mkdir -p "$(dirname "$dst")"
        cp "$src" "$dst"
    else
        echo "warn: $p not present on dev; skipping" >&2
    fi
done

cd "$worktree"

if [[ "$DRY_RUN" == "1" ]]; then
    echo "--- dry-run diff for main ---"
    git status --short
    echo
    git --no-pager diff --stat HEAD || true
    echo "--- end dry-run ---"
    exit 0
fi

if [[ -z "$(git status --porcelain)" ]]; then
    echo "main is already in sync with dev's installable paths; nothing to commit."
    exit 0
fi

git add -A
commit_msg="publish: $(date +%Y-%m-%d) (from dev @ $dev_sha)

$(git --no-pager diff --cached --stat | { head -40; cat >/dev/null; })"

git commit -m "$commit_msg"
echo
echo "committed on main:"
git --no-pager log -1 --stat
echo
echo "push when ready:  git push origin main"
