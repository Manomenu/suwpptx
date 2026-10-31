#!/usr/bin/env bash
# Sourced by check.sh and e2e.sh, never run: a linked git worktree gets its own e2e ports, so checkouts that run the
# browser tests at the same time on one machine do not collide. The main checkout and CI keep the plain ports
# (6311/3311). A linked worktree takes a port pair (20000-29999) from a stable hash of its directory name.
# A value already in the environment wins.
#
# The builds leave nothing running behind them: by default MSBuild keeps worker processes (node reuse) and its
# server alive for the next build, and a day of agent worktrees held gigabytes of RAM doing nothing.
export MSBUILDDISABLENODEREUSE=1
export DOTNET_CLI_USE_MSBUILD_SERVER=0
_checkout_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
_git_dir="$(git -C "$_checkout_root" rev-parse --absolute-git-dir)"
_common_dir="$(git -C "$_checkout_root" rev-parse --path-format=absolute --git-common-dir)"
if [ -z "${CI:-}" ] && [ "$_git_dir" != "$_common_dir" ]; then
    _name="$(basename "$_checkout_root")"
    _base=$((20000 + $(printf '%s' "$_name" | cksum | cut -d' ' -f1) % 5000 * 2))
    export E2E_SERVER_PORT="${E2E_SERVER_PORT:-$_base}"
    export E2E_WEB_PORT="${E2E_WEB_PORT:-$((_base + 1))}"
    echo "checkout $_name: e2e ports $E2E_SERVER_PORT/$E2E_WEB_PORT" >&2
fi
export E2E_SERVER_PORT="${E2E_SERVER_PORT:-6311}"
export E2E_WEB_PORT="${E2E_WEB_PORT:-3311}"
unset _checkout_root _git_dir _common_dir _name _base
