#!/usr/bin/env bash
set -euo pipefail

export PATH="$HOME/.dotnet:$PATH"

# Verifies that SnapCd.Server.Core/AI/Mcp/Generated/*.cs matches what the mcp command of generators/SnapCd.Generators
# would emit from the current controller annotations. Exits non-zero if any generated
# file would change — call `dotnet run --project generators/SnapCd.Generators -- mcp` to fix.
#
# Usage:
#   scripts/check-mcp-surface.sh           # check
#   scripts/check-mcp-surface.sh --write   # regenerate (used during `pre-commit run --hook-stage manual`)
#
# Invoked by pre-commit when any .cs or .csproj change might affect the generated surface.

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

MODE="--check"
if [[ ${1:-} == "--write" ]]; then
    MODE=""
fi

# check-generated-artifacts.sh builds the generator upfront in one MSBuild invocation and
# sets SNAPCD_GENERATORS_PREBUILT so the run here skips its own build.
NO_BUILD=()
if [[ ${SNAPCD_GENERATORS_PREBUILT:-0} == 1 ]]; then
    NO_BUILD=(--no-build)
fi

exec dotnet run "${NO_BUILD[@]}" --project generators/SnapCd.Generators/SnapCd.Generators.csproj -c Release -- mcp $MODE
