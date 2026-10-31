---
name: brief
description: Record requirements for a Suwpptx Studio workspace in its context.md (audience, goal, must include, style), creating the workspace when needed. Use when the owner says "brief", "note that the deck should...", "add to the requirements", "create a workspace", "/suwpptx:brief".
argument-hint: "<workspace> <what to record>"
---

# Brief a workspace

Arguments: `$ARGUMENTS` is the workspace name, then what to record.

1. Root: env var `SUWPPTX_ROOT`, else `~/suwpptx`. Workspace: `<root>/workspaces/<name>/`.
2. When the folder does not exist, confirm the name with the owner, then create it with `resources/`, `versions/` and this `context.md`:

```markdown
# Requirements

## Audience

## Goal

## Must include

## Style
```

   A name must be a plain folder name: no `/`, no `..`, not empty.
3. Read the existing `context.md`. Merge what the owner said under the right headings, in the owner's words. Keep everything already there: never delete or rewrite an existing line without asking.
4. Anything that fits no heading goes in a short `## Notes` section at the end.
5. Show the resulting change as a few diff lines, and name the next step (`/suwpptx:make <name>`).
