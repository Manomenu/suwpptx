# Development

## Requirements

- .NET SDK 10 (`global.json` pins the band), `just`, Docker (it runs the local PostgreSQL).
- A POSIX shell: on Windows use **Git Bash** (the scripts in `scripts/.internal/` are bash; run
  them with `bash scripts/.internal/check.sh`). Agents call the scripts directly, humans use `just`.
- Node 24 and pnpm 11 (`corepack enable`), for `Suwpptx.Web`; once per machine `pnpm exec playwright install chromium` in `Suwpptx.Web/` for the browser tests.
- For the full gate: `shellcheck` and `gitleaks`. A missing tool is reported as SKIP (a failure
  in CI), and the report ends with `NOT VERIFIED: <steps>`.

## Everyday commands

```sh
just                      # every recipe, grouped
just db up                # PostgreSQL on localhost:5543 (container suwpptx-postgres)
just server               # the API on http://127.0.0.1:6310 (dotnet watch)
just web                  # the web app on http://localhost:3310, proxies /api to the server
just e2e                  # browser tests (own server, own database, own ports; not part of the gate)
just logs                 # why did it fail? last warnings/errors of the newest server log (see below)
just api-types            # regenerate Suwpptx.Web/src/api/openapi.d.ts after changing a record the API exposes
just mutate               # Stryker.NET (MTP runner, preview) on the server code changed since master; needs a master with commits
just pack-template        # the dotnet new template as a NuGet package in .artifacts/nupkg
just up / just down      # the container stack on http://127.0.0.1:8200, then the smoke test
just check-quick          # build, csharpier, server tests, tsc, eslint, vitest: about 20 seconds
just check                # the whole gate
just fmt                  # csharpier format
```

The same without `just`: `bash scripts/.internal/check.sh --quick`, `bash scripts/.internal/check.sh`,
`bash scripts/.internal/db.sh up`.

## Settings

Defaults live in `appsettings.json`, laptop values in `appsettings.Development.json`, the rest
come from the environment (`Section:Key` is `Section__Key`: `ConnectionStrings__Default`). A
setting is an options class of the feature that reads it, validated with `ValidateOnStart()`.

## Worktrees

A linked git worktree gets its own test database (`suwpptx_test_<dir>`), derived from its
directory name by `checkout-env.sh`; its databases are left behind, not dropped.

## Claude Code hooks

`.claude/settings.json` runs `scripts/.internal/check.sh --quick` when Claude stops, and blocks
the stop while it fails (the loop is cut after one retry). Edits to migrations that already
exist, to lockfiles and to other generated files are refused with a message saying what to run.

## Personal agent rules

Rules that are yours alone (how you commit, which tools you use) go in `CLAUDE.local.md`:
Claude Code loads it, and git ignores it.

## The container stack

`just up` builds both images and starts db, server and web (`docker compose --profile db up -d --build --wait`),
then runs `scripts/.internal/smoke.sh` against it. Settings come from `.env` (copy `.env.example`); every value
has a default. With an external PostgreSQL, set `ConnectionStrings__Default` in `.env` and run
`docker compose up -d --build --wait` (no `db` profile). Use `127.0.0.1`, not `localhost`, in connection strings:
on Windows `localhost` tries IPv6 first and every new connection waits seconds.

## Logs

The server writes JSON log lines (the built-in console JSON formatter, `Logging:Console` in `appsettings.json`).
`just server` also writes them to `.artifacts/logs/server.jsonl`, `e2e.sh` to `.artifacts/logs/e2e-server.jsonl`;
the container stack is read with `--compose`. `just logs` prints the last warnings and errors of the newest log, one
line each; `just logs --trace <traceId>` shows every line of one request, with the `traceId` any error response
carries. `--all` shows every level, `-n N` the last N lines.

## Editors

VS Code: accept the recommended extensions (`.vscode/extensions.json`); formatting, tasks and debug launch are set up.
Rider: install the CSharpier plugin; everything else comes from `.editorconfig` and the repo's configs.
Personal files (`.idea/`, `*.DotSettings.user`) are ignored.
