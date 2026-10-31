---
name: make
description: Build or rebuild a PowerPoint deck from a Suwpptx Studio workspace (its context.md, resources and a template) and check the rendered slides. Use when the owner says "make the deck", "build the presentation", "rebuild", "new version", "/suwpptx:make", or asks to change slides of a workspace deck.
argument-hint: "<workspace>"
---

# Make a deck

Argument: the workspace name (`$ARGUMENTS`). Everything happens on files in the root folder; there is no API.

## 1. Find the workspace

1. Root: the env var `SUWPPTX_ROOT`, else `~/suwpptx`. Check with `echo "${SUWPPTX_ROOT:-$HOME/suwpptx}"`.
2. Workspace: `<root>/workspaces/<name>/`. When the name is missing or matches none or several folders, list `<root>/workspaces/` and ask which one.
3. Read `context.md` (audience, goal, must include, style, slide limits). Names starting with `.` are the app's own files: ignore them.
4. Look at every file in `resources/` (subfolders too): images with Read, text and code with Read, data files (csv, xlsx) by reading them with Python, other decks by their text. Note which resources belong on which slide.

## 2. Pick the style source

- A template named in `context.md` or in the conversation, found in `<root>/templates/`, in a workspace's `resources/` or `versions/`.
- Else, when several decks exist in `templates/` or `resources/`, ask which one.
- Else a plain style; say so.
- When `versions/` already holds decks and the owner iterates, the newest version is the base.

## 3. Check the tooling

Run `python3 -c "import pptx"`. When it fails, tell the owner to run `python3 -m pip install --user python-pptx` (or install it in a venv) and stop.

## 4. Build

Write the build script in a temp dir (`mktemp -d`), never in the workspace.

- Open the template file itself with python-pptx. Remove its example slides, then add slides from ITS layouts and placeholders, so theme, fonts and masters stay exact. Do not recreate the look with hand-set colours and fonts.
- A `.potx` must first become a `.pptx`: copy it to the temp dir and, in the zip, change `[Content_Types].xml` main content type from `presentationml.template.main+xml` to `presentationml.presentation.main+xml`.
- Respect `context.md`: audience, slide limit, every must-include item.
- Put speaker notes where they help.
- Scale images from `resources/` to fit inside their placeholder or the free area, keep the aspect ratio.
- Never overflow a text box: shorten the text or split the slide.

## 5. Save the next version

1. N = the highest `v<N>.pptx` in `<workspace>/versions/` plus 1 (1 when there is none). Gaps are fine.
2. Save to `versions/.v<N>.pptx.tmp`, then rename to `versions/v<N>.pptx`, so the app sees a complete file.

## 6. Check the previews

The app writes `versions/v<N>-previews/` with `01.png`, `02.png`, ... and then `done.json` (`{"slides": N}`), or `error.txt` with the reason.

1. Poll for `done.json` or `error.txt` up to about 120 seconds.
2. `error.txt`: report the reason, fix the deck, save the next version.
3. `done.json`: Read every PNG. Look for overflowing or clipped text, overlaps, empty placeholders, unreadable text or contrast, and consistency with the template.
4. Anything wrong: fix it and save the next version (steps 4 to 6). At most 3 rounds, then report what is left.
5. No previews ever appear: say the app may not be running (start it with `just server` and `just web`, or `just up`; see `docs/development.md` in the Suwpptx repository) and still report the file.

## 7. Report

End with: the version saved (path), the slide count, one line per slide saying what it is, and what the owner might want next (another template, other photos, shorter or longer).
