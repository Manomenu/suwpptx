---
name: template
description: Describe the look of a PowerPoint deck or template (slide size, theme colours, fonts, slide masters, layouts and placeholders) and optionally record it as the Style of a workspace. Use when the owner asks "what does this template look like", "analyse this deck", "use this deck's style", "/suwpptx:template".
argument-hint: "<deck>"
---

# Read a deck's look

Argument: `$ARGUMENTS`, the deck (a file name or path).

1. Root: env var `SUWPPTX_ROOT`, else `~/suwpptx`. Find the deck in `<root>/templates/`, in a workspace's `resources/` or `versions/`. When several match or none, list the candidates and ask.
2. Check `python3 -c "import pptx"`; when it fails, tell the owner to run `python3 -m pip install --user python-pptx` (or use a venv) and stop.
3. Write a short script in a temp dir (`mktemp -d`, never in the root folder). A `.potx` first becomes a `.pptx`: copy it and in the zip change `[Content_Types].xml` main content type from `presentationml.template.main+xml` to `presentationml.presentation.main+xml`. The script extracts:
   - slide size (EMU converted to inches and the aspect ratio);
   - theme colours (the `a:clrScheme` of the theme part XML: dk1, lt1, dk2, lt2, accent1 to accent6, hlink);
   - major and minor fonts (`a:majorFont`, `a:minorFont`);
   - slide masters and their layouts with every placeholder: type, idx, position and size;
   - for the example slides, which layout each uses and what it puts in it.
4. Answer in plain words (mood, colour use, typography, how busy the slides are, which layouts to use for what) plus a table of layouts and placeholders.
5. Only when the owner asks, record a short Style summary (colours, fonts, template file name, the layouts to use) under `## Style` in a workspace's `context.md`, keeping what is there, so `/suwpptx:make` reuses it.
