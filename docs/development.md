# Development

## Requirements

- .NET SDK 10 (`global.json` pins the band), `just`, Docker (for the container stack only).
- A POSIX shell: on Windows use **Git Bash** (the scripts in `scripts/.internal/` are bash; run
  them with `bash scripts/.internal/check.sh`). Agents call the scripts directly, humans use `just`.
- Node 24 and pnpm 11 (`corepack enable`), for `Suwpptx.Web`; once per machine `pnpm exec playwright install chromium` in `Suwpptx.Web/` for the browser tests.
- LibreOffice Impress and poppler-utils: the server draws slide previews with them (Fedora: `sudo dnf install libreoffice-impress poppler-utils`).
- Python 3 with python-pptx (`python3 -m pip install --user python-pptx`) and Claude Code: the skills that Claude runs build the decks.
- For the full gate: `shellcheck` and `gitleaks`. A missing tool is reported as SKIP (a failure
  in CI), and the report ends with `NOT VERIFIED: <steps>`.

## Everyday commands

```sh
just                      # every recipe, grouped
just server               # the API on http://127.0.0.1:6310 (dotnet watch)
just web                  # the web app on http://localhost:3310, proxies /api to the server
just e2e                  # browser tests (own server, own root folder, own ports; not part of the gate)
just logs                 # why did it fail? last warnings/errors of the newest server log (see below)
just api-types            # regenerate Suwpptx.Web/src/api/openapi.d.ts after changing a record the API exposes
just mutate               # Stryker.NET (MTP runner, preview) on the server code changed since master; needs a master with commits
just up / just down      # the container stack on http://127.0.0.1:8200, then the smoke test
just check-quick          # build, csharpier, server tests, tsc, eslint, vitest: about 20 seconds
just check                # the whole gate
just fmt                  # CSharpier and Prettier
```

The same without `just`: `bash scripts/.internal/check.sh --quick`, `bash scripts/.internal/check.sh`.

`just` lists every recipe; run it to see the exact names for starting the app in development (server and web).

## The root folder

All data is one folder: `~/suwpptx`, or the folder in the environment variable `SUWPPTX_ROOT`. The server
creates it when it is missing. Inside: `templates/` (shared `.pptx`/`.potx` decks) and `workspaces/<name>/`
with `context.md`, `resources/` and `versions/v<N>.pptx`. The server writes `versions/v<N>-previews/`
(`01.png`, `02.png`, ..., then `done.json`, or `error.txt` with the reason). Names starting with `.` are
the app's own files. The container stack mounts the same folder into the server.

## The Claude Code plugin

`plugin/` holds the plugin `suwpptx` with the skills `/suwpptx:make <workspace>`, `/suwpptx:brief <workspace> <text>`
and `/suwpptx:template <deck>`. Install it **at project scope** (recommended): `.claude/settings.json` already
enables `suwpptx@suwpptx` for everyone who opens this repository, and each person installs it once, from the
repository root:

```sh
claude plugin marketplace add ./
claude plugin install suwpptx@suwpptx --scope project
```

The marketplace is this repository (`.claude-plugin/marketplace.json`), so Claude Code reads the skills straight
from `plugin/`. To try an edited skill without installing, start one session with `claude --plugin-dir ./plugin`.

After editing a skill, `/reload-plugins` or restart the session. Check the plugin with
`claude plugin validate ./plugin` and the marketplace with `claude plugin validate .`.

## Settings

Defaults live in `appsettings.json` (`Folder:UploadLimitMegabytes`, `AllowedHosts`), the rest
come from the environment (`Section:Key` is `Section__Key`). A setting is an options class of the
feature that reads it, validated with `ValidateOnStart()`.

## Worktrees

A linked git worktree gets its own ports for the browser tests, derived from its directory name by
`checkout-env.sh`.

## Claude Code hooks

`.claude/settings.json` runs `scripts/.internal/check.sh --quick` when Claude stops, and blocks
the stop while it fails (the loop is cut after one retry). Edits to lockfiles and to
other generated files are refused with a message saying what to run.

## Personal agent rules

Rules that are yours alone (how you commit, which tools you use) go in `CLAUDE.local.md`:
Claude Code loads it, and git ignores it.

## The container stack

`just up` builds both images and starts the server and the web app on http://127.0.0.1:8200, with the root
folder mounted into the server, then runs `scripts/.internal/smoke.sh` against it. Settings come from `.env`
(copy `.env.example`); every value has a default.

Rootless Podman (a `docker` shim over its socket) maps your user to root inside the container, so the server cannot
read the mounted folder as `UID:GID`: set `SERVER_USER=0:0` in `.env`. Files it writes are still yours on the host.
On Docker leave it unset. The volume carries `:Z` for SELinux hosts; Docker ignores it elsewhere.

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
