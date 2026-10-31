#!/usr/bin/env bash
# End-to-end tests (Suwpptx.Web/src/**/*.e2e.ts): a real browser against the real server and database. Not part of
# check.sh. Arguments go to Playwright, e.g.
#   e2e.sh --headed        watch the browser
#   e2e.sh -g "note"       only the tests whose name matches
#
# Every run starts from an empty database of its own (suwpptx_e2e; a linked worktree gets a suffixed name and its own
# ports, checkout-env.sh), next to the development one, and its own server and web app (playwright.config.ts):
# what you run by hand is not touched. Locally it starts PostgreSQL (db.sh up); CI provides it as a service.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# shellcheck source=scripts/.internal/checkout-env.sh
source "$ROOT/scripts/.internal/checkout-env.sh"

if [ -z "${CI:-}" ]; then
    "$ROOT/scripts/.internal/db.sh" up >/dev/null
fi

# Two commands, not one: CREATE DATABASE cannot run inside the transaction a single -c would be.
recreate=(-c "DROP DATABASE IF EXISTS $E2E_DATABASE WITH (FORCE)" -c "CREATE DATABASE $E2E_DATABASE")
if [ -n "${CI:-}" ]; then
    psql -q "postgresql://suwpptx:suwpptx@127.0.0.1:5543/postgres" "${recreate[@]}"
else
    "$ROOT/scripts/.internal/db.sh" psql -q -d postgres "${recreate[@]}"
fi

mkdir -p "$ROOT/.artifacts/logs"
dotnet build "$ROOT/Suwpptx.Server" --nologo -v q

cd "$ROOT/Suwpptx.Web"
[ -d node_modules ] || pnpm install --frozen-lockfile --silent
exec pnpm exec playwright test -c e2e/playwright.config.ts "$@"
