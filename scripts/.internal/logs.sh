#!/usr/bin/env bash
# Why did it fail? One command, short output, from the server's JSON log lines (no jq needed).
#   logs.sh                 the last warnings and errors of the newest log in .artifacts/logs/
#   logs.sh --all           every level
#   logs.sh --trace <id>    every line of one request: the traceId of an error response (32 hex digits or the W3C form)
#   logs.sh --compose       read the compose server (docker compose logs) instead of a file
#   logs.sh -n 50           the last 50 lines (default 20)
#   logs.sh --file <path>   a specific log file
# One line each: time level category traceId=... message, and for an exception its type and first line.
# The logs are written by `just server` (.artifacts/logs/server.jsonl) and e2e.sh (.artifacts/logs/e2e-server.jsonl).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"

all=0
trace=""
compose=0
count=20
file=""
while [ $# -gt 0 ]; do
    case "$1" in
        --all) all=1 ;;
        --trace)
            trace="${2:?--trace needs a traceId}"
            shift
            ;;
        --compose) compose=1 ;;
        -n)
            count="${2:?-n needs a number}"
            shift
            ;;
        --file)
            file="${2:?--file needs a path}"
            shift
            ;;
        *)
            echo "usage: logs.sh [--all] [--trace <id>] [--compose] [-n N] [--file <path>]" >&2
            exit 2
            ;;
    esac
    shift
done

if [ -n "$trace" ]; then
    trace="$(printf '%s' "$trace" | grep -oiE '[0-9a-f]{32}' | head -n 1 | tr 'A-F' 'a-f' || true)"
    [ -n "$trace" ] || { echo "logs.sh: that is not a traceId (32 hex digits, or 00-<32 hex>-<16 hex>-<2 hex>)" >&2; exit 2; }
    all=1
fi

if [ "$compose" -eq 1 ]; then
    source_name="docker compose logs server"
    read_lines() { (cd "$ROOT" && docker compose --profile db logs --no-log-prefix --no-color server 2>&1); }
else
    if [ -z "$file" ]; then
        file="$(ls -t "$ROOT"/.artifacts/logs/*.jsonl 2>/dev/null | head -n 1 || true)"
    fi
    if [ -z "$file" ] || [ ! -f "$file" ]; then
        echo "logs.sh: no log file yet. Start the server with \`just server\` or run e2e.sh (they write .artifacts/logs/*.jsonl), or read the container stack with --compose." >&2
        exit 1
    fi
    source_name="${file#"$ROOT"/}"
    read_lines() { cat "$file"; }
fi

echo "# $source_name"
read_lines | awk -v all="$all" -v trace="$trace" '
function field(line, name,    start, rest, value, i, ch) {
    start = index(line, "\"" name "\":\"")
    if (start == 0) return ""
    rest = substr(line, start + length(name) + 4)
    value = ""
    for (i = 1; i <= length(rest); i++) {
        ch = substr(rest, i, 1)
        if (ch == "\\") { value = value substr(rest, i, 2); i++; continue }
        if (ch == "\"") break
        value = value ch
    }
    return value
}
substr($0, 1, 1) != "{" { next }
{
    level = field($0, "LogLevel")
    if (trace != "" && index($0, trace) == 0) next
    if (all == 0 && level != "Warning" && level != "Error" && level != "Critical") next
    id = field($0, "TraceId")
    message = field($0, "Message")
    exception = field($0, "Exception")
    if (exception != "") {
        cut = index(exception, "\\r")
        newline = index(exception, "\\n")
        if (cut == 0 || (newline > 0 && newline < cut)) cut = newline
        if (cut > 0) exception = substr(exception, 1, cut - 1)
        exception = " | " substr(exception, 1, 200)
    }
    printf "%s %-11s %s%s %s%s\n", field($0, "Timestamp"), level, field($0, "Category"), (id != "" ? " trace=" id : ""), substr(message, 1, 300), exception
}' | tail -n "$count"
