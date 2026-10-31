#!/usr/bin/env bash
# Builds the release zip of the app for one OS: package.sh <win-x64|linux-x64> [<out dir>]   (default .artifacts/release)
# Result: <out>/suwpptx-<rid>.zip holding, at its root, `suwpptx` (Linux) or `suwpptx.exe` (Windows), `wwwroot/` (the built
# web app, which the server serves) and `VERSION` (the tag without the leading v, or 0.0.0-dev+<short sha>).
# install.sh and the plugin's start hook rely on exactly this layout. The app is self-contained (the .NET runtime and
# the native PDFium library sit next to the executable), so users install no .NET. Not single-file: that pulls in an
# ILLink package whose version follows the SDK patch, and CI's locked restore then fails. Needs dotnet, pnpm and zip.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
RID="${1:-}"
case "$RID" in
    win-x64) EXE="suwpptx.exe" PUBLISHED="Suwpptx.Server.exe" ;;
    linux-x64) EXE="suwpptx" PUBLISHED="Suwpptx.Server" ;;
    *)
        echo "usage: package.sh <win-x64|linux-x64> [<out dir>]" >&2
        exit 2
        ;;
esac
OUT="${2:-$ROOT/.artifacts/release}"
mkdir -p "$OUT"
OUT="$(cd "$OUT" && pwd)"

if ! command -v zip >/dev/null 2>&1; then
    echo "package.sh: zip is missing; install it (Debian/Ubuntu: sudo apt-get install zip)" >&2
    exit 1
fi

if TAG="$(git -C "$ROOT" describe --tags --exact-match --match 'v*' 2>/dev/null)"; then
    VERSION="${TAG#v}"
else
    VERSION="0.0.0-dev+$(git -C "$ROOT" rev-parse --short HEAD)"
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

# The web build does not depend on the rid; building it again per call keeps the script simple.
cd "$ROOT/Suwpptx.Web"
[ -d node_modules ] || pnpm install --frozen-lockfile
pnpm build

dotnet publish "$ROOT/Suwpptx.Server" -c Release -r "$RID" --self-contained true \
    -o "$WORK/publish" --nologo -v q

STAGE="$WORK/stage"
mkdir -p "$STAGE"
cp -r "$WORK/publish/." "$STAGE/"
find "$STAGE" -name '*.pdb' -delete
mv "$STAGE/$PUBLISHED" "$STAGE/$EXE"
chmod +x "$STAGE/$EXE"
cp -r "$ROOT/Suwpptx.Web/dist" "$STAGE/wwwroot"
printf '%s\n' "$VERSION" >"$STAGE/VERSION"

ZIP="$OUT/suwpptx-$RID.zip"
rm -f "$ZIP"
(cd "$STAGE" && zip -q -r "$ZIP" .)
echo "package.sh: $ZIP (version $VERSION, $(du -h "$ZIP" | cut -f1))"
