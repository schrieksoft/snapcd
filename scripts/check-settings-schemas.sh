#!/usr/bin/env bash
set -euo pipefail

export PATH="$HOME/.dotnet:$PATH"

# Verifies that applications/snapcd/schemas/<component>.schema.yaml matches what
# the settings command of generators/SnapCd.Generators would emit from the current settings
# POCOs and their XML doc summaries. Exits non-zero if any schema file would change —
# call with --write to regenerate, then commit the result.
#
# Usage:
#   scripts/check-settings-schemas.sh           # check
#   scripts/check-settings-schemas.sh --write   # regenerate
#
# Invoked by pre-commit when any settings POCO under SnapCd.<Component>/Settings/ changes.
#
# The generators emit schemas/<component>.schema.json (untracked intermediates); the
# committed artifacts are YAML (readable diffs), converted via scripts/json-to-yaml.py.

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

MODE="--check"
if [[ ${1:-} == "--write" ]]; then
    MODE="--write"
fi

# check-generated-artifacts.sh builds the generator upfront in one MSBuild invocation and
# sets SNAPCD_GENERATORS_PREBUILT so the run here skips its own build.
NO_BUILD=()
if [[ ${SNAPCD_GENERATORS_PREBUILT:-0} == 1 ]]; then
    NO_BUILD=(--no-build)
fi

# Always regenerate the JSON intermediates; the YAML comparison below decides
# staleness against the committed artifacts.
dotnet run "${NO_BUILD[@]}" --project generators/SnapCd.Generators/SnapCd.Generators.csproj -c Release -- settings

failed=0
for component in runner agent server; do
    json="schemas/${component}.schema.json"
    target="schemas/${component}.schema.yaml"
    generated="$(mktemp)"
    python3 scripts/json-to-yaml.py "$json" "$generated"

    if [[ "$MODE" == "--write" ]]; then
        if cmp -s "$generated" "$target"; then
            echo "check-settings-schemas: $target already up to date"
        else
            cp "$generated" "$target"
            chmod 644 "$target"
            echo "check-settings-schemas: wrote $target"
        fi
    elif ! cmp -s "$generated" "$target"; then
        echo "check-settings-schemas: $target is stale — run scripts/check-settings-schemas.sh --write" >&2
        failed=1
    fi
    rm -f "$generated"
done

exit $failed
