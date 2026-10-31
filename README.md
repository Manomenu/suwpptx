
# Suwpptx

An ASP.NET Core minimal-API server (Npgsql, Dapper, hand-written SQL, grate migrations), a React 19 + shadcn/ui + Tailwind
web app with types generated from the server's OpenAPI document, and a Notes feature as the pattern end to end.

## What the gate guarantees

`bash scripts/.internal/check.sh` (`--quick` while working) fails on: any compiler or analyzer warning, banned
APIs, a layer used against its order, dead types and packages, vague names, stray comments, `dynamic`, unpinned
images, stale generated types, an unresolvable compose file. The full list, each with its fix:
[docs/architecture.md, "What the gate forbids"](docs/architecture.md#what-the-gate-forbids).
Browser tests run separately (`scripts/.internal/e2e.sh`), and `just up` starts the container stack and
smoke-tests it.

## Specs and acceptance criteria

Development is spec-driven. A spec in `docs/visual/spec/` (written with the suwagent
`/suwagent:visual-spec` skill) says what to build and why. Each of its acceptance criteria gets an id,
`AC-<feature>-<n>`, and outside the spec it lives only as a guardrail: a test or a rule the gate runs,
which names the id. A criterion that no automated check can prove is reworded into plain description
or dropped from the spec.

```text
$ just criteria AC-notes-1
AC-notes-1       server test (xunit)          Suwpptx.Server.Tests/Notes/NoteEndpointsTests.cs:14
AC-notes-1       e2e test (playwright)        Suwpptx.Web/src/notes/NotesScreen.e2e.ts:4
```

The full gate fails when a criterion has no guardrail, a guardrail names an id no spec defines, an id
stands where nothing is checked (production code, a config no tool reads), or a spec breaks the format
(no acceptance section, a criterion without an id or with two, an id defined twice). It cannot see
whether a guardrail would really fail; `/suwagent:visual-guardrails` checks that before a merge.

| Kind | Where | How it names the id |
| --- | --- | --- |
| `server test (xunit)` | `Suwpptx.Server.Tests/<Feature>/` | `[Trait("criterion", "AC-notes-1")]` on the test |
| `architecture test (xunit)` | `Suwpptx.Server.Tests/` root: the repository guards | the same `Trait` |
| `web test (vitest)` | `Suwpptx.Web/src/**/*.test.ts(x)` | in the title: `it("AC-notes-1: a blank note is refused", …)` |
| `e2e test (playwright)` | `Suwpptx.Web/src/**/*.e2e.ts` | in the title: `test("AC-notes-1: …", …)` |
| `lint (eslint)` | `Suwpptx.Web/eslint.config.js` | a comment `// AC-notes-1` on the line above the rule |
| `type check (tsc)` | `Suwpptx.Web/tsconfig*.json` | a comment `// AC-notes-1` above the option |
| `lint (.NET analyzer)` | `.editorconfig` | `# AC-notes-1` on its own line above `dotnet_diagnostic.….severity = error` |
| `banned API` | `BannedSymbols.txt` | in the message, so it shows in the build error: `P:System.DateTime.Now;AC-notes-1 use TimeProvider` |
| `build rule` | `Directory.Build.props` | an XML comment `<!-- AC-notes-1 -->` above the property |

## Requirements and commands

See [docs/development.md](docs/development.md). Rules for contributors and agents: [AGENTS.md](AGENTS.md).
