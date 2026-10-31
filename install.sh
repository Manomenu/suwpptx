#!/usr/bin/env bash
# Installs Suwpptx Studio for the current user, on Windows (Git Bash) and on Linux. Safe to run again:
# every step skips what is already done, and a second run updates the plugin and the app.
#
#   curl -fsSL https://github.com/Manomenu/suwpptx/releases/latest/download/install.sh | bash
#
# What it does: creates the root folder (~/suwpptx or SUWPPTX_ROOT) with templates/ and workspaces/;
# installs the Claude Code plugin suwpptx at project scope in that folder (so it is on only there);
# downloads the app for this system into the plugin's data folder; adds a "Suwpptx Studio" shortcut;
# then checks LibreOffice and Python with python-pptx and prints the command for each one missing.
#
# Exit codes: 0 ready, 1 stopped (the last line says why), 3 installed, but tools are missing.
# Settings (all optional): SUWPPTX_ROOT, SUWPPTX_MARKETPLACE (default Manomenu/suwpptx),
# SUWPPTX_RELEASE_URL, SUWPPTX_APP_ZIP (a local zip instead of the download),
# SUWPPTX_NO_SHORTCUT=1, CLAUDE_CONFIG_DIR.
set -euo pipefail

stop() {
  echo "Suwpptx install stopped: $*" >&2
  exit 1
}

case "$(uname -s)" in
  MINGW* | MSYS* | CYGWIN*)
    os=windows
    rid=win-x64
    exe=suwpptx.exe
    ;;
  Linux)
    os=linux
    rid=linux-x64
    exe=suwpptx
    ;;
  *) stop "this system ($(uname -s)) is not supported: Suwpptx Studio runs on Windows and Linux." ;;
esac

root="${SUWPPTX_ROOT:-$HOME/suwpptx}"
marketplace="${SUWPPTX_MARKETPLACE:-Manomenu/suwpptx}"
release_url="${SUWPPTX_RELEASE_URL:-https://github.com/Manomenu/suwpptx/releases/latest/download}"
data="${CLAUDE_CONFIG_DIR:-$HOME/.claude}/plugins/data/suwpptx-suwpptx"

command -v claude > /dev/null 2>&1 || stop "Claude Code is not installed: see https://code.claude.com/docs/en/setup"
command -v curl > /dev/null 2>&1 || stop "curl is not installed: install curl and run this again."
if [ -z "${SUWPPTX_APP_ZIP:-}" ] || [ ! -d "$marketplace" ]; then
  curl -fsS -m 15 -o /dev/null -I https://github.com || stop "can't reach github.com: check the internet connection and run this again."
fi

to_native() { if [ "$os" = windows ]; then cygpath -w "$1"; else printf '%s' "$1"; fi; }

# --- 1. the root folder
mkdir -p "$root/templates" "$root/workspaces"
echo "Folder ready: $root"

# --- 2. the plugin, at project scope in the root
(
  cd "$root"
  if claude plugin install suwpptx --marketplace "$marketplace" --scope project > /dev/null 2>&1; then
    echo "Plugin installed in $root"
  else
    claude plugin marketplace update suwpptx > /dev/null 2>&1 || true
    claude plugin update suwpptx@suwpptx --scope project > /dev/null 2>&1 \
      || stop "the plugin could not be installed: run 'claude plugin install suwpptx --marketplace $marketplace --scope project' in $root to see why."
    echo "Plugin up to date in $root"
  fi
) || exit 1
grep -q 'suwpptx@suwpptx' "$root/.claude/settings.json" 2> /dev/null \
  || stop "the plugin is not switched on in $root/.claude/settings.json: run 'claude plugin install suwpptx --marketplace $marketplace --scope project' in $root."

# --- 3. the app
mkdir -p "$data"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
zip="${SUWPPTX_APP_ZIP:-}"
if [ -z "$zip" ]; then
  zip="$work/suwpptx-$rid.zip"
  echo "Downloading suwpptx-$rid.zip ..."
  curl -fsSL -o "$zip" "$release_url/suwpptx-$rid.zip" \
    || stop "the app could not be downloaded from $release_url/suwpptx-$rid.zip: check the internet connection, then run this again."
fi

unpack() { # zip, folder
  if command -v unzip > /dev/null 2>&1; then
    unzip -q -o "$1" -d "$2"
  elif [ "$os" = windows ]; then
    powershell -NoProfile -Command "Expand-Archive -LiteralPath '$(cygpath -w "$1")' -DestinationPath '$(cygpath -w "$2")' -Force"
  else
    stop "unzip is missing: run 'sudo dnf install unzip' and run this again."
  fi
}

staging="$data/app.new"
rm -rf "$staging"
mkdir -p "$staging"
unpack "$zip" "$staging"
[ -f "$staging/$exe" ] || stop "the zip holds no $exe: download suwpptx-$rid.zip again."
if [ -f "$staging/VERSION" ]; then
  version="$(tr -d '\r\n' < "$staging/VERSION")"
else
  version="sha256:$(sha256sum "$zip" | cut -d' ' -f1)"
fi

if [ -f "$data/app/$exe" ] && [ "$(cat "$data/app/.version" 2> /dev/null || true)" = "$version" ]; then
  rm -rf "$staging"
  echo "App is current ($version)"
else
  if [ -f "$data/app.pid" ]; then
    pid="$(tr -d '\r\n' < "$data/app.pid")"
    if [ "$os" = windows ]; then taskkill //PID "$pid" //F > /dev/null 2>&1 || true; else kill "$pid" > /dev/null 2>&1 || true; fi
    rm -f "$data/app.pid"
    sleep 1
  fi
  rm -rf "$data/app"
  mv "$staging" "$data/app"
  printf '%s\n' "$version" > "$data/app/.version"
  [ "$os" = windows ] || chmod +x "$data/app/$exe"
  echo "App installed ($version)"
fi

# --- 4. the shortcut
if [ "${SUWPPTX_NO_SHORTCUT:-}" != 1 ]; then
  if [ "$os" = windows ]; then
    LNK_ROOT="$(cygpath -w "$root")" powershell -NoProfile -Command '
      $shell = New-Object -ComObject WScript.Shell
      $link = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath("Desktop")) "Suwpptx Studio.lnk"))
      $link.TargetPath = "cmd.exe"
      $link.Arguments = "/k cd /d `"$env:LNK_ROOT`" && claude"
      $link.WorkingDirectory = $env:LNK_ROOT
      $link.Save()' > /dev/null
    echo "Shortcut: Suwpptx Studio on the Desktop"
  else
    mkdir -p "$HOME/.local/share/applications"
    cat > "$HOME/.local/share/applications/suwpptx-studio.desktop" << DESKTOP
[Desktop Entry]
Type=Application
Name=Suwpptx Studio
Comment=Build PowerPoint decks with Claude Code
Exec=sh -c 'cd "$root" && exec claude'
Terminal=true
Categories=Office;
DESKTOP
    echo "Shortcut: Suwpptx Studio in the application menu"
  fi
fi

# --- 5. the tools the app and the skills need
missing=()
soffice_found=0
if command -v soffice > /dev/null 2>&1; then
  soffice_found=1
else
  candidates=(/usr/bin/soffice /usr/lib/libreoffice/program/soffice /usr/lib64/libreoffice/program/soffice)
  if [ "$os" = windows ]; then
    candidates=()
    for base in "$(printenv ProgramFiles || true)" "$(printenv 'ProgramFiles(x86)' || true)" "C:\\Program Files" "C:\\Program Files (x86)"; do
      [ -n "$base" ] && candidates+=("$(cygpath -u "$base")/LibreOffice/program/soffice.exe")
    done
  fi
  for candidate in "${candidates[@]}"; do
    [ -x "$candidate" ] && soffice_found=1
  done
fi
if [ "$soffice_found" = 0 ]; then
  if [ "$os" = windows ]; then
    missing+=("LibreOffice (draws the slide previews): winget install TheDocumentFoundation.LibreOffice")
  else
    missing+=("LibreOffice (draws the slide previews): sudo dnf install libreoffice-impress")
  fi
fi

python_ran=0
python_ready=0
for candidate in python3 python "py -3"; do
  # shellcheck disable=SC2086 # Why: "py -3" is a command and its argument.
  if $candidate -c pass > /dev/null 2>&1; then
    python_ran=1
    # shellcheck disable=SC2086 # Why: same.
    if $candidate -c "import pptx" > /dev/null 2>&1; then
      python_ready=1
      break
    fi
  fi
done
if [ "$python_ready" = 0 ]; then
  if [ "$os" = windows ]; then
    if [ "$python_ran" = 0 ]; then
      missing+=("Python (Claude builds the decks with it): winget install Python.Python.3.12")
    fi
    missing+=("python-pptx (after installing Python, in a new terminal): py -3 -m pip install --user python-pptx")
  else
    if [ "$python_ran" = 0 ]; then
      missing+=("Python and pip: sudo dnf install python3-pip")
    fi
    missing+=("python-pptx (Claude builds the decks with it): python3 -m pip install --user python-pptx")
  fi
fi

if [ "${#missing[@]}" -gt 0 ]; then
  echo
  echo "Suwpptx Studio is installed, but needs these tools:"
  for line in "${missing[@]}"; do echo "  - $line"; done
  exit 3
fi

echo
echo "Everything is ready. Open Suwpptx Studio from the shortcut, or run: cd \"$root\" && claude"
