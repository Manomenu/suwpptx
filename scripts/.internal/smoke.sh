#!/usr/bin/env bash
# Smoke test of a running stack: smoke.sh <base-url>, e.g. smoke.sh http://127.0.0.1:8200
# Read-only requests through the web's nginx, the way traffic arrives: the page answers, and the server behind it
# answers with its database. Prints what failed and what to look at; exit code is non-zero on any failure.
set -euo pipefail

BASE="${1:-}"
if [ -z "$BASE" ]; then
    echo "usage: smoke.sh <base-url>   e.g. smoke.sh http://127.0.0.1:8200" >&2
    exit 2
fi
BASE="${BASE%/}"

FAILED=0
check() {
    local path="$1" expected="$2" hint="$3" body status
    body="$(mktemp)"
    status="$(curl -sS -m 10 -o "$body" -w '%{http_code}' "$BASE$path" 2>&1)" || status="unreachable"
    if [ "$status" = "200" ] && grep -q "$expected" "$body"; then
        echo "ok    $path"
    else
        echo "FAIL  $BASE$path: got status '$status', expected 200 with '$expected' in the body. $hint" >&2
        FAILED=1
    fi
    rm -f "$body"
}

check /healthz '^ok' "nginx in the web container does not answer: docker compose ps, docker compose logs web."
check /api/health '"status":"ok"' "the page reaches nginx but not a healthy server with its database: docker compose logs server, check ConnectionStrings__Default."
exit "$FAILED"
