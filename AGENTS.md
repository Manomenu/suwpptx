# AGENTS.md

Rules for anyone — human or AI agent — changing this repository. `CLAUDE.md` only points here;
this file is the one to edit. Where things live: [docs/architecture.md](docs/architecture.md).
Setup and everyday commands: [docs/development.md](docs/development.md). Not built yet: [TODO.md](TODO.md).
Areas: not used until a second product area appears — how to add them:
[docs/architecture.md#areas-when-a-second-product-area-appears](docs/architecture.md#areas-when-a-second-product-area-appears).

## Commands

| Command | Use it to |
| --- | --- |
| `scripts/.internal/check.sh --quick` | during work: build with analyzers, csharpier, server tests against the real database, tsc, eslint, vitest (about 20 seconds) |
| `scripts/.internal/check.sh` | before calling a change done: quick, plus prettier, knip, API types, acceptance criteria, compose config, shellcheck, gitleaks |
| `scripts/.internal/e2e.sh` | after changing anything a user clicks: Playwright against a real server and its own database (not in the gate) |
| `scripts/.internal/logs.sh` (`just logs`) | why did it fail? the last warnings and errors of the newest server log, one line each; `--trace <id>` takes the `traceId` of an error response, `--all`, `--compose`, `-n N` |
| `scripts/.internal/criteria.sh [AC-<feature>-<n>]` (`just criteria`) | which guardrails keep each acceptance criterion true: id, kind, file:line; fails on an unguarded criterion (the full gate runs it, not `--quick`); `just criteria-update` moves the pinned tool to its newest version |
| `scripts/.internal/api-types.sh` | after changing a record the API exposes: regenerates `openapi.d.ts` (never edit it) |
| `scripts/.internal/db.sh up\|down\|status\|psql` | the local PostgreSQL on `localhost:5543` (container `suwpptx-postgres`) |
| `just fmt` | fix formatting: CSharpier and Prettier |
| `just up` / `just down` | the container stack on port 8200, with its smoke test |

Humans use `just` (lists every recipe); agents call `scripts/.internal/` directly.
Ports of this template: server 6310, web 3310, PostgreSQL 5543, compose web 8200.

## Git

- **The main branch is `master`**, never `main` (`git init -b master`).
- Work on a feature branch, never on `master`. Agents do not rewrite or discard history: no
  amend, reset, rebase, stash, force-push or deleting branches. Merging and releasing are the owner's.
- A change with a red gate is not committed: it stays in the working tree and the summary says why.
- **One task, one branch.** The branch changes only what the task needs to work. Refactors,
  renames, extra logging, config or version bumps and unrelated fixes noticed on the way go to
  their own branch, or into `TODO.md`. Say so once when you reach for one; if the owner confirms
  it belongs here, put it in.
- Personal rules (how you commit, your tools) go in the gitignored `CLAUDE.local.md`, not here.

## KISS and YAGNI — the first rule

The repo grows **only when a real need shows up**; one developer maintains it after hours.

- No dependency, setting, env var, compose service or abstraction "for later": it arrives with
  the first code that uses it. No interface with one implementation; tests use the real thing
  (a real PostgreSQL) or a recorded answer.
- No new component (queue, cache, worker, sidecar) without the feature that needs it.
- When in doubt, leave it out — and say in the change what was left out and why.

You added logic with rules → `just mutate`; surviving mutants → stronger tests.

## Subtract before you add
Plan a change by listing what it makes obsolete, and delete that first, in the same change:
migrate every caller, then remove the old code. No shims, no [Obsolete], no "legacy" path kept just in case.

## Rules live as low as they can
When I correct you, or when you are about to add a rule to AGENTS.md / CLAUDE.md,
first try to make the mistake impossible or loud, in this order:
a type → an analyzer or lint (with the fix in its message) → a test → a hook.
Add a sentence only for what none of these can check, and say in one line why.

A guard's message always states the fix. Never weaken a gate step to make a change pass — fix
the change. A step that cannot run reports SKIP, never PASS, and the report ends with a line listing
the steps that were not verified; in CI a SKIP is a failure.

## Specs: acceptance criteria live as guardrails

Development is spec-driven. A spec (`docs/visual/spec/`) says what and why; its acceptance criteria
sit in `<section data-role="acceptance">`, one `<li>` each, each with its own id `AC-<feature>-<n>`.
An id names one criterion for good: a dropped criterion takes its id with it, the next one gets the
next free number, never a reused or renumbered one (old guardrails would silently guard the new
text). Outside the spec a criterion exists only as a guardrail on the gate
that fails when the criterion breaks: a unit, integration or e2e test, a lint rule, an architecture
test. Not as a checklist, a QA note or a comment. The guardrail names the id where it stands:

| Kind | Where | How it names the id |
| --- | --- | --- |
| `server test (xunit)` | `Suwpptx.Server.Tests/<Feature>/*.cs` | `[Trait("criterion", "AC-notes-1")]` on the test |
| `architecture test (xunit)` | `Suwpptx.Server.Tests/*.cs` (the repository guards) | the same `Trait` |
| `web test (vitest)` | `Suwpptx.Web/src/**/*.test.ts(x)` | the id in the title: `it("AC-notes-1: …")` |
| `e2e test (playwright)` | `Suwpptx.Web/src/**/*.e2e.ts` | the id in the title: `test("AC-notes-1: …")` |
| `lint (eslint)` | `Suwpptx.Web/eslint.config.js` | `// AC-notes-1` on the line above the rule |
| `type check (tsc)` | `Suwpptx.Web/tsconfig*.json` | `// AC-notes-1` on the line above the option |
| `lint (.NET analyzer)` | `.editorconfig` | `# AC-notes-1` on its own line above the rule |
| `banned API` | `BannedSymbols.txt` | the id in the message: `P:…;AC-notes-1 use …` |
| `build rule` | `Directory.Build.props` | `<!-- AC-notes-1 -->` above the property |

`criteria.sh` runs the suwagent plugin's `vdoc.py criteria`, pinned by commit and SHA-256: it lists
each criterion with every guardrail, its kind and file:line, and fails on a criterion no guardrail
names, an id no spec defines, an id where the gate checks nothing, and a spec that breaks the
format above (no section, a criterion without an id or with two, an id defined twice). Files ignored
by git are not read. The kinds are `criteria.json`; a new kind is an entry there and a row in both
tables. `just criteria-update` moves the pin to the plugin's newest version.

The hook `hook-guard-edit.sh` refuses an agent's edit of the gate and the lint configs (`check.sh`,
`criteria.sh`, `criteria.json`, the hook scripts, `.claude/settings.json`, `.github/workflows/`,
`.editorconfig`, `eslint.config.js`, `BannedSymbols.txt`, `Directory.Build.props`, `tsconfig*.json`).
When a change needs one, ask the owner to start a session with `ALLOW_GATE_EDIT=1 claude` and give
them the exact change to make there.

- A criterion no automated check can prove is a wrong criterion, not a missing test. Ask the owner
  whether it becomes plain description in the spec or leaves the spec; never keep it unguarded.
- The script sees only that an id is named, not that the guardrail would fail (why this is a
  sentence): before a spec'd feature is merged, check that with `/suwagent:visual-guardrails`.

## Layout: by feature

The server grows by features: everything that makes one feature work sits in **one folder**
named in the users' terms (`Notes/`), not by technical kind (`Models/`, `Services/`). `Notes/`
is the pattern end to end: route (`NoteEndpoints`) → store with hand-written SQL (`NoteStore`)
→ records (`Note`). Wiring is explicit: `Api/ServerApp.cs` calls every `Map…`.

- A new capability starts as a new folder, or goes into the feature it belongs to — never into
  a shared bucket. Shared code is earned by the second feature that needs it, and is named
  after what it is (`Database`), not after who uses it.
- A new feature gets its layer in `ContractsTests.cs`, a new project its row in
  `ProjectReferencesTests.cs`, a new top-level folder its line in `docs/architecture.md`.
- Web: one folder per feature under `src/` (screen, logic with its test, `api.ts`), shared look in `src/design/` (shadcn, Tailwind), types from the generated `openapi.d.ts`; rules live in plain `.ts` modules, components only wire them.
- Schema changes go into a new numbered script in `Database/Migrations/up/`, never an edited one.
- Records an endpoint sends or receives are a contract with the outside; a row from our own SQL
  is not. Keep them apart once they differ.
- A problem the caller can fix is a ProblemDetails whose `detail` says what to do.

## What no test can check

- Names are chosen word by word: domain words in the users' language (English here), software
  words in technical English. One word per thing, everywhere.
- A comment says *why*, never *what*; if code needs a *what* comment, rename the code.
- Secrets never enter the repo: not in code, settings, tests or history. Local values live in
  user secrets or the gitignored `.env`; a real hit is removed and rotated, never allow-listed.
- Delegate larger, well-defined work (a clear goal, known files, a check that says when done)
  to a cheaper model; keep design decisions and the review of the result.
- When several changes share one branch, build them all, then run the full gate once.
