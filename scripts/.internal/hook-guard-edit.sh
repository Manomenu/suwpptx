#!/usr/bin/env bash
# Claude Code PreToolUse hook for Edit|Write (.claude/settings.json): refuses to change what must never be edited
# by hand — a migration script that already exists (grate refuses to start on a changed one) and generated files
# (lockfiles, the OpenAPI output). Exit 2 blocks the call and the message on stderr goes back to the agent.
set -euo pipefail

input="$(cat)"
path="$(printf '%s' "$input" | grep -o '"file_path"[[:space:]]*:[[:space:]]*"[^"]*"' | head -n 1 | sed 's/^[^:]*:[[:space:]]*"//; s/"$//')"
path="$(printf '%s' "$path" | tr '\134' '/')"
[ -n "$path" ] || exit 0

case "$path" in
    */Database/Migrations/up/*)
        if [ -e "$path" ]; then
            echo "Blocked: $path is a migration that already exists; grate refuses to start when a script changes after it ran." >&2
            echo "Add a new numbered script instead: Suwpptx.Server/Database/Migrations/up/NNNN_what_it_does.sql (next free number)." >&2
            exit 2
        fi
        ;;
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
