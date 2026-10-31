# Entry point for humans: `just` lists every recipe, grouped. Logic longer than a line or
# two lives in scripts/.internal/, which is also what CI and agents call directly.

set shell := ["bash", "-euo", "pipefail", "-c"]

# The local PostgreSQL — `just db` lists its recipes (.just/db.just)
mod db '.just/db.just'

[private]
default:
    @just --list --unsorted

# HTTP API on :6310, rebuilt on every change — needs `just db up`; its JSON log is also written to .artifacts/logs/server.jsonl
[group('run')]
server *args:
    mkdir -p .artifacts/logs
    dotnet watch --quiet --project Suwpptx.Server run {{ args }} 2>&1 | tee .artifacts/logs/server.jsonl

# Why did it fail? The last warnings and errors of the newest server log, one line each (--all, --trace <id>, --compose, -n N)
[group('run')]
[positional-arguments]
logs *args:
    @./scripts/.internal/logs.sh "$@"

# Which guardrails keep each acceptance criterion of the specs true: id, kind, file:line (or one: just criteria AC-<feature>-<n>)
[group('run')]
[positional-arguments]
criteria *args:
    @./scripts/.internal/criteria.sh "$@"

# Web UI on :3310, proxies /api to the server — run `just server` alongside
[group('run')]
web *args:
    cd Suwpptx.Web && { [ -d node_modules ] || pnpm install; } && pnpm dev {{ args }}

# Build and start the whole stack with its own PostgreSQL on http://127.0.0.1:8200 (WEB_PORT in .env); waits until every service is healthy
[group('infra')]
up:
    docker compose --profile db up -d --build --wait
    ./scripts/.internal/smoke.sh http://127.0.0.1:${WEB_PORT:-8200}

# Stop the stack (images and the database volume stay)
[group('infra')]
down:
    docker compose --profile db down --remove-orphans

# Regenerate the web app's TypeScript types from the server's OpenAPI document (after changing a record the API sends or receives)
[group('dev')]
api-types:
    @./scripts/.internal/api-types.sh

# Browser tests (Playwright) against a real server and an empty database of their own; not part of `just check`
[group('maintenance')]
e2e *args:
    @./scripts/.internal/e2e.sh {{ args }}

# The whole gate: build with analyzers, csharpier, tests, shellcheck, gitleaks (the same CI runs)
[group('maintenance')]
check:
    @./scripts/.internal/check.sh

# The quick gate: build with analyzers, csharpier, server tests (what the Claude Code Stop hook runs)
[group('maintenance')]
check-quick:
    @./scripts/.internal/check.sh --quick

# Format the code: CSharpier for C#, Prettier for the web app
[group('maintenance')]
fmt:
    dotnet tool restore >/dev/null && dotnet csharpier format .
    cd Suwpptx.Web && pnpm exec prettier --write --log-level warn .

# Mutation testing of the server code changed since master (Stryker.NET): a surviving mutant is a test that is too weak
[group('maintenance')]
mutate:
    @./scripts/.internal/db.sh up >/dev/null
    dotnet tool restore >/dev/null
    cd Suwpptx.Server.Tests && dotnet tool run dotnet-stryker

# Pack the dotnet new template as a NuGet package into .artifacts/nupkg
[group('dev')]
pack-template:
    dotnet pack pack/Solid.DotnetApp.Template.csproj -o .artifacts/nupkg

# Install tools and packages from the lockfiles, and enable the git hooks
[group('maintenance')]
sync:
    git config core.hooksPath .githooks
    dotnet tool restore
    dotnet restore Suwpptx.slnx
    cd Suwpptx.Web && pnpm install --frozen-lockfile
