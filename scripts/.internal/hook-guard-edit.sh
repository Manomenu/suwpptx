#!/usr/bin/env bash
# Claude Code PreToolUse hook for Edit|Write (.claude/settings.json): refuses to change what must never be edited
# by hand — generated files (lockfiles, the OpenAPI output) — and what an agent must not change on its own: the gate, the hooks and the lint
# configs, unless the owner started the session with ALLOW_GATE_EDIT=1. Exit 2 blocks the call and the message on
# stderr goes back to the agent.
set -euo pipefail

input="$(cat)"
path="$(printf '%s' "$input" | grep -o '"file_path"[[:space:]]*:[[:space:]]*"[^"]*"' | head -n 1 | sed 's/^[^:]*:[[:space:]]*"//; s/"$//')"
path="$(printf '%s' "$path" | tr '\134' '/')"
[ -n "$path" ] || exit 0

if [ "${ALLOW_GATE_EDIT:-}" != "1" ]; then
    case "$path" in
        */scripts/.internal/check.sh | */scripts/.internal/criteria.sh | */scripts/.internal/hook-*.sh | */criteria.json | \
            */.claude/settings.json | */.github/workflows/* | */.editorconfig | */eslint.config.js | */BannedSymbols.txt | \
            */Directory.Build.props | */tsconfig*.json)
            echo "Blocked: $path is part of the gate (a check, a hook or a lint config), which an agent does not change on its own." >&2
            echo "Do not work around it: ask the user to start a session with ALLOW_GATE_EDIT=1 claude and give them the exact change to make there (the file, what to change, why)." >&2
            exit 2
            ;;
    esac
fi

case "$path" in
    */packages.lock.json)
        echo "Blocked: $path is a generated lockfile. Change Directory.Packages.props and run: dotnet restore Suwpptx.slnx" >&2
        exit 2
        ;;
    */pnpm-lock.yaml)
        echo "Blocked: $path is a generated lockfile. Change package.json and run: pnpm install" >&2
        exit 2
        ;;
    */openapi.d.ts)
        echo "Blocked: $path is generated from the server's OpenAPI document. Regenerate it with scripts/.internal/api-types.sh (just api-types) instead of editing it." >&2
        exit 2
        ;;
esac
exit 0
