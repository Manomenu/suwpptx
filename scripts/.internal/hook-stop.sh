#!/usr/bin/env bash
# Claude Code Stop hook (.claude/settings.json): runs the quick gate and, when it fails, blocks the stop (exit 2)
# so the agent keeps working with the failure in front of it. When the stop was already blocked once in this turn
# (stop_hook_active), it lets the agent finish: a red gate it cannot fix must not loop forever. When the working
# tree is exactly as it was at the last green quick gate (same HEAD, same changes, same untracked files), it does
# not run the gate again: a reply that changed nothing costs no wait.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"

input="$(cat)"
if printf '%s' "$input" | grep -Eq '"stop_hook_active"[[:space:]]*:[[:space:]]*true'; then
    exit 0
fi

# Why: a clean `git status` is not enough: a change committed in the same turn leaves it clean and unchecked
fingerprint() {
    {
        git -C "$ROOT" rev-parse HEAD 2>/dev/null || true
        git -C "$ROOT" diff HEAD --binary 2>/dev/null || git -C "$ROOT" diff --cached --binary
        git -C "$ROOT" ls-files --others --exclude-standard -z | (cd "$ROOT" && xargs -0 -r sha256sum)
    } | sha256sum | cut -d' ' -f1
}
green="$(git -C "$ROOT" rev-parse --git-path quick-gate-green)"
case "$green" in /*) ;; *) green="$ROOT/$green" ;; esac
now="$(fingerprint)"
if [ -f "$green" ] && [ "$(cat "$green")" = "$now" ]; then
    exit 0
fi

if output="$(bash "$ROOT/scripts/.internal/check.sh" --quick 2>&1)"; then
    printf '%s\n' "$now" >"$green"
    exit 0
fi
{
    echo "The quick gate (scripts/.internal/check.sh --quick) is red. Fix it before finishing:"
    printf '%s\n' "$output" | tail -n 60
} >&2
exit 2
