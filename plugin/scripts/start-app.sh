#!/usr/bin/env bash
# SessionStart hook of the suwpptx plugin: makes sure Suwpptx Studio runs, and returns within about 2 seconds.
# It never downloads or installs anything. Its output reaches Claude as context, so each line is meant for the user.
# SUWPPTX_URL (default http://localhost:6310) is for tests; SUWPPTX_NO_BROWSER=1 skips opening the browser.
set -u

url="${SUWPPTX_URL:-http://localhost:6310}"
data="${CLAUDE_PLUGIN_DATA:-${CLAUDE_CONFIG_DIR:-$HOME/.claude}/plugins/data/suwpptx-suwpptx}"
project="${CLAUDE_PROJECT_DIR:-$PWD}"
case "$(uname -s)" in MINGW* | MSYS* | CYGWIN*) windows=1 ;; *) windows=0 ;; esac

probe() { # prints the HTTP status, 000 when nothing answers
  curl -s -m 1 -o "$data/probe.json" -w '%{http_code}' "$url/api/health" 2> /dev/null || true
}
is_ours() { grep -Eq '"status"[[:space:]]*:[[:space:]]*"ok"' "$data/probe.json" 2> /dev/null; }

mkdir -p "$data"
code="$(probe)"
if [ "$code" != 000 ]; then
  if is_ours; then exit 0; fi
  echo "Port 6310 is used by another program: close it, then start a new session."
  exit 0
fi

exe="$data/app/suwpptx"
[ "$windows" = 1 ] && exe="$data/app/suwpptx.exe"
if [ ! -f "$exe" ]; then
  echo "Suwpptx Studio isn't installed yet: ask Claude to install Manomenu/suwpptx."
  exit 0
fi

port_url="${url%/}"
if [ "$windows" = 1 ]; then
  # Why: Start-Process makes a process of its own, not a child of this shell, so it outlives the hook; -PassThru gives its Windows PID for taskkill.
  SUWPPTX_ROOT="$(cygpath -w "$project")" ASPNETCORE_URLS="$port_url" APP_EXE="$(cygpath -w "$exe")" APP_LOG="$(cygpath -w "$data/app.log")" APP_ERR="$(cygpath -w "$data/app.err.log")" \
    powershell -NoProfile -Command '(Start-Process -FilePath $env:APP_EXE -WindowStyle Hidden -PassThru -RedirectStandardOutput $env:APP_LOG -RedirectStandardError $env:APP_ERR).Id' \
    | tr -d '\r' > "$data/app.pid"
else
  launcher=(nohup)
  command -v setsid > /dev/null 2>&1 && launcher=(setsid nohup)
  SUWPPTX_ROOT="$project" ASPNETCORE_URLS="$port_url" "${launcher[@]}" "$exe" > "$data/app.log" 2>&1 < /dev/null &
  echo "$!" > "$data/app.pid"
  disown 2> /dev/null || true
fi

for _ in 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15; do
  [ "$(probe)" != 000 ] && break
  sleep 0.1
done

if [ "${SUWPPTX_NO_BROWSER:-}" != 1 ]; then
  if [ "$windows" = 1 ]; then
    cmd //c start "" "$url" > /dev/null 2>&1 || true
  elif command -v xdg-open > /dev/null 2>&1; then
    xdg-open "$url" > /dev/null 2>&1 &
    disown 2> /dev/null || true
  fi
fi
echo "Suwpptx Studio runs at $url"
