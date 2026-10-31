#!/usr/bin/env bash
# Which guardrails keep each acceptance criterion of the specs true? One line per guardrail: id, kind, file:line.
#   criteria.sh               every criterion of every spec in docs/visual/spec/
#   criteria.sh AC-<feature>-<n>  one criterion
#   criteria.sh --root <dir>  another checkout (CriteriaScriptTests runs it on a sample repository)
# A guardrail names the id of the criterion it proves; where it may stand decides its kind (the case below).
# A spec defines its criteria in <section data-role="acceptance">, one id per <li>; an id anywhere else in a spec
# is a reference. Exit 1, with the fix, when a spec breaks that (no section, a criterion without an id or with two,
# an id defined twice, a reference to an undefined id), a criterion has no guardrail, a guardrail names an id no spec
# defines, or an id stands where nothing is checked (production code, a config the gate does not read).
# Prose (docs/, *.md) is not read.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# Why: sort and uniq order by locale otherwise, and the report must read the same on every machine
export LC_ALL=C

only=""
while [ $# -gt 0 ]; do
    case "$1" in
        --root)
            ROOT="$(cd "${2:?--root needs a directory}" && pwd)"
            shift
            ;;
        AC-*) only="$1" ;;
        *)
            echo "usage: criteria.sh [--root <dir>] [AC-<feature>-<n>]" >&2
            exit 2
            ;;
    esac
    shift
done
cd "$ROOT"

ID='AC-[a-z][a-z0-9]*(-[a-z][a-z0-9]*)*-[0-9]+'

kind_of() {
    case "$1" in
        Suwpptx.Server.Tests/*/*.cs) echo "server test (xunit)" ;;
        Suwpptx.Server.Tests/*.cs) echo "architecture test (xunit)" ;;
        Suwpptx.Web/src/*.e2e.ts) echo "e2e test (playwright)" ;;
        Suwpptx.Web/src/*.test.ts | Suwpptx.Web/src/*.test.tsx) echo "web test (vitest)" ;;
        Suwpptx.Web/eslint.config.js) echo "lint (eslint)" ;;
        Suwpptx.Web/tsconfig*.json) echo "type check (tsc)" ;;
        .editorconfig) echo "lint (.NET analyzer)" ;;
        BannedSymbols.txt) echo "banned API" ;;
        Directory.Build.props) echo "build rule" ;;
        *) echo "" ;;
    esac
}

# One line per definition ("DEF id file"), id of a broken criterion ("SEEN id") or problem ("ERR message") of one spec.
read_spec() {
    awk -v file="$1" -v id="$ID" '
        { text = text $0 " " }
        END {
            start = index(text, "data-role=\"acceptance\"")
            if (!start) {
                print "ERR\t" file ": no acceptance section — add <section data-role=\"acceptance\"> with one <li> per criterion, each starting with its id AC-<feature>-<n>."
                exit
            }
            section = substr(text, start)
            close_at = index(section, "</section>")
            if (close_at) section = substr(section, 1, close_at)
            n = split(section, items, "<li")
            if (n < 2) print "ERR\t" file ": the acceptance section has no criteria — add one <li> per criterion, each starting with its id."
            for (i = 2; i <= n; i++) {
                item = items[i]
                end_at = index(item, "</li>")
                if (end_at) item = substr(item, 1, end_at - 1)
                ids = ""; count = 0; rest = item
                while (match(rest, id)) {
                    found = substr(rest, RSTART, RLENGTH)
                    if (index(" " ids " ", " " found " ") == 0) { ids = ids " " found; count++ }
                    rest = substr(rest, RSTART + RLENGTH)
                }
                plain = item
                sub(/^[^>]*>/, "", plain)
                gsub(/<[^>]*>/, "", plain)
                gsub(/^ +| +$/, "", plain)
                if (count == 0) print "ERR\t" file ": a criterion without an id: \"" substr(plain, 1, 60) "\" — start it with the next free AC-<feature>-<n>."
                else if (count > 1) {
                    print "ERR\t" file ": one criterion has several ids (" substr(ids, 2) "): \"" substr(plain, 1, 60) "\" — split it, one criterion per id."
                    split(substr(ids, 2), several, " ")
                    for (j in several) print "SEEN\t" several[j]
                }
                else print "DEF\t" substr(ids, 2) "\t" file
            }
        }' "$1"
}

problems=()
definitions=""
seen=""
for spec in $(git ls-files --cached --others --exclude-standard -- 'docs/visual/spec/*.html' 2>/dev/null); do
    while IFS='	' read -r tag first second; do
        case "$tag" in
            DEF) definitions+="$first	$second"$'\n' ;;
            SEEN) seen+="$first"$'\n' ;;
            ERR) problems+=("$first") ;;
        esac
    done < <(read_spec "$spec")
done
criteria="$(printf '%s' "$definitions" | cut -f1 | sort -u)"
for id in $(printf '%s' "$definitions" | cut -f1 | sort | uniq -d); do
    problems+=("$id is defined twice ($(printf '%s' "$definitions" | grep "^$id	" | cut -f2 | sort -u | paste -sd, -)) — an id names one criterion for good: give the newer one the next free number.")
done
while IFS= read -r hit; do
    [ -n "$hit" ] || continue
    id="${hit##*:}"
    grep -qxF "$id" <<<"$criteria"$'\n'"$seen" || problems+=("${hit%:*}: mentions $id, which no acceptance section defines — define it there or remove the mention.")
done < <(git grep -noE --untracked -I "$ID" -- 'docs/visual/spec/*.html' 2>/dev/null | sort -u || true)

# Why: CriteriaScriptTests writes ids into its sample repository; they are not guardrails of this one
found="$(git grep -nE --untracked -I "$ID" -- . ':!docs' ':!*.md' ':!Suwpptx.Server.Tests/CriteriaScriptTests.cs' 2>/dev/null || true)"

trace=()
while IFS= read -r hit; do
    [ -n "$hit" ] || continue
    path="${hit%%:*}"
    rest="${hit#*:}"
    line="${rest%%:*}"
    kind="$(kind_of "$path")"
    for id in $(printf '%s\n' "${rest#*:}" | grep -oE "$ID"); do
        if [ -z "$kind" ]; then
            problems+=("$id at $path:$line: not a guardrail — nothing the gate runs reads this file. Move the id to the test, lint rule or config that proves the criterion (kinds: scripts/.internal/criteria.sh).")
        elif ! grep -qxF "$id" <<<"$criteria"; then
            problems+=("$id at $path:$line: no spec defines this criterion — restore it in the spec, or delete or rename the guardrail's id.")
        else
            trace+=("$id	$kind	$path:$line")
        fi
    done
done <<<"$found"

for id in $criteria; do
    if ! printf '%s\n' "${trace[@]}" | grep -q "^$id	"; then
        problems+=("$id: no guardrail — name the id in a test, lint rule or config that fails when the criterion breaks. If no automated check can prove it, ask the owner whether it becomes plain description in the spec or leaves the spec.")
    fi
done

if [ -n "$only" ]; then
    trace=("$(printf '%s\n' "${trace[@]}" | grep "^$only	" || true)")
    mapfile -t problems < <(printf '%s\n' "${problems[@]}" | grep "^${only}[ :]" || true)
fi
printf '%s\n' "${trace[@]}" | sort | while IFS='	' read -r id kind place; do
    if [ -n "$id" ]; then printf '%-16s %-28s %s\n' "$id" "$kind" "$place"; fi
done
if [ ${#problems[@]} -gt 0 ]; then
    printf '%s\n' "${problems[@]}" >&2
    exit 1
fi
