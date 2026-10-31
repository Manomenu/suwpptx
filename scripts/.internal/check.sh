#!/usr/bin/env bash
# The quality gate. CI runs exactly this script; humans run `just check`, hooks run `--quick`.
#   check.sh --quick   build with analyzers, csharpier, server tests against the real database, tsc, eslint, vitest
#   check.sh           quick, plus prettier, knip, API types up to date, acceptance criteria guarded, shellcheck and gitleaks
# Acceptance criteria are not in --quick: the Stop hook runs it after every turn, and a criterion written into
# a spec before its guardrail (or one the owner must first decide on) would block the agent from stopping to ask.
# Quiet on success: one line per step. A failed step prints the tail of its log. A step that cannot run
# (a tool is missing) is SKIP, never PASS; with CI set a SKIP fails the gate, locally the report ends with
# "NOT VERIFIED: <steps>". Exit code is non-zero on any failure.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# shellcheck source=scripts/.internal/checkout-env.sh
source "$ROOT/scripts/.internal/checkout-env.sh"

QUICK=0
case "${1:-}" in
    "") ;;
    --quick) QUICK=1 ;;
    *)
        echo "usage: check.sh [--quick]" >&2
        exit 2
        ;;
esac

LOGS="$(mktemp -d)"
trap 'rm -rf "$LOGS"' EXIT
FAILURE_LINES=25
ORDER=()
declare -A NAME
declare -A SKIPPED
declare -A LIMIT
QUICK_TARGET=60

declare_step() { ORDER+=("$1"); NAME["$1"]="$2"; LIMIT["$1"]="$3"; }

# run_step KEY FUNCTION — output goes to a log, the result is "status seconds". Always returns 0:
# a failed step is reported at the end and does not stop the group it runs in. Every step has a hard
# ceiling (LIMIT[key] seconds, CHECK_STEP_LIMIT overrides all of them); a step that exceeds it is killed and fails
# as "hung or too slow". Without `timeout` on the machine there is no ceiling.
run_step() {
    local key="$1" function="$2" start status=0
    start=$SECONDS
    if command -v timeout >/dev/null 2>&1; then
        timeout -k 10 "${CHECK_STEP_LIMIT:-${LIMIT[$key]}}" bash -c "$function" >"$LOGS/$key.log" 2>&1 || status=$?
        if [ "$status" -eq 124 ]; then
            echo "hung or too slow: $function exceeded ${CHECK_STEP_LIMIT:-${LIMIT[$key]}} s and was killed" >>"$LOGS/$key.log"
        fi
    else
        bash -c "$function" >"$LOGS/$key.log" 2>&1 || status=$?
    fi
    echo "$status $((SECONDS - start))" >"$LOGS/$key.result"
}

build_server() { (cd "$ROOT" && dotnet build Suwpptx.slnx --nologo -v quiet); }

format_server() { (cd "$ROOT" && dotnet tool restore >/dev/null && dotnet csharpier check .); }

local_database() { "$ROOT/scripts/.internal/db.sh" up >/dev/null; }

test_server() { (cd "$ROOT" && dotnet test --solution Suwpptx.slnx --no-build --no-progress); }

shell_scripts() { (cd "$ROOT" && { find scripts -name "*.sh"; echo .githooks/pre-commit; } | xargs shellcheck -S warning); }

web_typecheck() { cd "$ROOT/Suwpptx.Web" && pnpm exec tsc --noEmit -p tsconfig.app.json && pnpm exec tsc --noEmit -p tsconfig.node.json; }

web_eslint() { cd "$ROOT/Suwpptx.Web" && pnpm exec eslint . --max-warnings 0; }

web_vitest() { cd "$ROOT/Suwpptx.Web" && pnpm exec vitest run; }

web_prettier() { cd "$ROOT/Suwpptx.Web" && pnpm exec prettier --check .; }

web_knip() { cd "$ROOT/Suwpptx.Web" && pnpm exec knip; }

api_types() { "$ROOT/scripts/.internal/api-types.sh" --check; }

acceptance_criteria() { "$ROOT/scripts/.internal/criteria.sh"; }

compose_files() {
    cd "$ROOT"
    docker compose config --quiet
    docker compose --profile db config --quiet
}

secrets_in_git() {
    cd "$ROOT"
    if git rev-parse --verify HEAD >/dev/null 2>&1; then
        gitleaks git . --no-banner --redact --log-level warn
    fi
    gitleaks dir . --no-banner --redact --log-level warn --max-target-megabytes 1
}

export ROOT
export -f build_server format_server local_database test_server shell_scripts secrets_in_git web_typecheck web_eslint web_vitest web_prettier web_knip api_types acceptance_criteria compose_files
declare_step build "build server (compiler, analyzers, banned APIs, NuGet audit)" 300
declare_step format "format server (csharpier check)" 120
if [ -z "${CI:-}" ]; then declare_step postgres "postgres for tests (db.sh up)" 120; fi
declare_step test "test server (xunit, repository guards, real PostgreSQL)" 600
declare_step tsc "typecheck Suwpptx.Web (tsc: app, e2e tests, config)" 120
declare_step eslint "lint Suwpptx.Web (eslint strict + import contracts)" 180
declare_step vitest "test Suwpptx.Web (vitest)" 180
if [ "$QUICK" -eq 0 ]; then
    declare_step compose "compose stack resolves, with and without the db profile (docker compose config)" 120
    declare_step prettier "format Suwpptx.Web (prettier --check)" 120
    declare_step knip "dead code Suwpptx.Web (knip)" 180
    declare_step apitypes "API types up to date (api-types.sh --check)" 300
    declare_step criteria "acceptance criteria guarded (criteria.sh)" 60
    declare_step shellcheck "shell scripts (shellcheck)" 120
    declare_step gitleaks "secrets in git (gitleaks)" 300
    docker compose version >/dev/null 2>&1 || SKIPPED[compose]="docker compose not installed"
    command -v shellcheck >/dev/null 2>&1 || SKIPPED[shellcheck]="shellcheck not installed"
    command -v gitleaks >/dev/null 2>&1 || SKIPPED[gitleaks]="gitleaks not installed"
    if ! command -v python3 >/dev/null 2>&1; then
        SKIPPED[criteria]="python3 not installed"
    else
        fetched=0
        "$ROOT/scripts/.internal/criteria.sh" --fetch >/dev/null 2>&1 || fetched=$?
        # Why: only a missing download is a SKIP; a checksum mismatch runs the step, which fails and says why
        if [ "$fetched" -eq 3 ]; then SKIPPED[criteria]="the pinned vdoc.py cannot be downloaded (no network?)"; fi
    fi
fi

if command -v pnpm >/dev/null 2>&1; then
    [ -d "$ROOT/Suwpptx.Web/node_modules" ] || (cd "$ROOT/Suwpptx.Web" && pnpm install --frozen-lockfile --silent)
else
    for key in tsc eslint vitest prettier knip apitypes; do
        if [ -n "${LIMIT[$key]+x}" ]; then SKIPPED[$key]="pnpm not installed"; fi
    done
fi

GATE_START=$SECONDS
PIDS=()
(
    run_step build build_server
    if [ -z "${CI:-}" ]; then run_step postgres local_database; fi
    run_step test test_server
    if [ "$QUICK" -eq 0 ] && [ -z "${SKIPPED[apitypes]+x}" ]; then run_step apitypes api_types; fi
) &
PIDS+=($!)
launch() {
    local key="$1"
    if [ -z "${SKIPPED[$key]+x}" ]; then
        run_step "$key" "$2" &
        PIDS+=($!)
    fi
}
launch format format_server
launch tsc web_typecheck
launch eslint web_eslint
launch vitest web_vitest
if [ "$QUICK" -eq 0 ]; then
    launch prettier web_prettier
    launch knip web_knip
    launch criteria acceptance_criteria
    launch compose compose_files
    launch shellcheck shell_scripts
    launch gitleaks secrets_in_git
fi
for pid in "${PIDS[@]}"; do wait "$pid" || true; done
TOTAL=$((SECONDS - GATE_START))

FAILED=0
NOT_VERIFIED=()
REPORT=()
FAILURES=()
for key in "${ORDER[@]}"; do
    if [ -n "${SKIPPED[$key]+x}" ]; then
        if [ -n "${CI:-}" ]; then
            REPORT+=("FAIL        ${NAME[$key]} — ${SKIPPED[$key]} (CI: a skipped step fails)")
            FAILED=1
        else
            REPORT+=("SKIP        ${NAME[$key]} — ${SKIPPED[$key]}")
            NOT_VERIFIED+=("$key")
        fi
        continue
    fi
    read -r status secs <"$LOGS/$key.result"
    if [ "$status" -eq 0 ]; then
        REPORT+=("$(printf 'PASS %4ss  %s' "$secs" "${NAME[$key]}")")
    else
        REPORT+=("$(printf 'FAIL %4ss  %s' "$secs" "${NAME[$key]}")")
        FAILURES+=("${NAME[$key]}" "$LOGS/$key.log")
        FAILED=1
    fi
done

for ((i = 0; i < ${#FAILURES[@]}; i += 2)); do
    echo
    echo "==================== failed: ${FAILURES[i]} (last $FAILURE_LINES lines) ===================="
    tail -n "$FAILURE_LINES" "${FAILURES[i + 1]}"
done

echo
for line in "${REPORT[@]}"; do
    echo "  $line"
done
echo "  total ${TOTAL}s"
if [ "$QUICK" -eq 1 ] && [ "$TOTAL" -gt "$QUICK_TARGET" ]; then
    echo "WARN: the quick gate took ${TOTAL}s, the target is ${QUICK_TARGET}s (a warning: wall time depends on the machine)"
fi
if [ ${#NOT_VERIFIED[@]} -gt 0 ]; then
    echo "NOT VERIFIED: ${NOT_VERIFIED[*]}"
fi
exit "$FAILED"
