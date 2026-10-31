#!/usr/bin/env bash
# Claude Code Stop hook (.claude/settings.json): runs the quick gate and, when it fails, blocks the stop (exit 2)
# so the agent keeps working with the failure in front of it. When the stop was already blocked once in this turn
# (stop_hook_active), it lets the agent finish: a red gate it cannot fix must not loop forever.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"

input="$(cat)"
if printf '%s' "$input" | grep -Eq '"stop_hook_active"[[:space:]]*:[[:space:]]*true'; then
    exit 0
fi

if output="$(bash "$ROOT/scripts/.internal/check.sh" --quick 2>&1)"; then
    exit 0
fi
{
    echo "The quick gate (scripts/.internal/check.sh --quick) is red. Fix it before finishing:"
    printf '%s\n' "$output" | tail -n 60
} >&2
exit 2
