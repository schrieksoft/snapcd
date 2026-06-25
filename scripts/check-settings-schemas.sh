#!/usr/bin/env bash
set -euo pipefail

export PATH="$HOME/.dotnet:$PATH"

# Verifies that applications/snapcd/schemas/<component>.schema.json matches what the matching
# SnapCd.Settings.Generator.<Component> would emit from the current settings POCOs and their
# XML doc summaries. Exits non-zero if any schema file would change — call without --check
# (or run the per-component generator directly) to regenerate, then commit the result.
#
# Usage:
#   scripts/check-settings-schemas.sh           # check
#   scripts/check-settings-schemas.sh --write   # regenerate (used during `pre-commit run --hook-stage manual`)
#
# Invoked by pre-commit when any settings POCO under SnapCd.<Component>/Settings/ changes.

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

MODE="--check"
if [[ ${1:-} == "--write" ]]; then
    MODE=""
fi

GENERATORS=(
    "generators/SnapCd.Settings.Generator.Runner/SnapCd.Settings.Generator.Runner.csproj"
    "generators/SnapCd.Settings.Generator.Agent/SnapCd.Settings.Generator.Agent.csproj"
    "generators/SnapCd.Settings.Generator.Server/SnapCd.Settings.Generator.Server.csproj"
)

failed=0
for project in "${GENERATORS[@]}"; do
    if ! dotnet run --project "$project" -c Release -- $MODE; then
        failed=1
    fi
done

exit $failed
