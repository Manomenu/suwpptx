#!/usr/bin/env bash
# End-to-end tests (Suwpptx.Web/src/**/*.e2e.ts): a real browser against the real server and a real folder. Not part of
# check.sh. Arguments go to Playwright, e.g.
#   e2e.sh --headed        watch the browser
#   e2e.sh -g "note"       only the tests whose name matches
#
# Every run starts from an empty root folder of its own (.artifacts/e2e/root; a linked worktree has its own
# .artifacts, and its own ports, checkout-env.sh) and its own server and web app (playwright.config.ts): the folder
# you work on by hand is not touched. The server needs soffice and pdftoppm on PATH.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# shellcheck source=scripts/.internal/checkout-env.sh
source "$ROOT/scripts/.internal/checkout-env.sh"

E2E_ROOT="$ROOT/.artifacts/e2e/root"
rm -rf "$E2E_ROOT"
mkdir -p "$E2E_ROOT"
export E2E_ROOT

mkdir -p "$ROOT/.artifacts/logs"
dotnet build "$ROOT/Suwpptx.Server" --nologo -v q

cd "$ROOT/Suwpptx.Web"
[ -d node_modules ] || pnpm install --frozen-lockfile --silent
exec pnpm exec playwright test -c e2e/playwright.config.ts "$@"
