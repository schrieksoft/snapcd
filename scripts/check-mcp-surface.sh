#!/usr/bin/env bash
set -euo pipefail

export PATH="$HOME/.dotnet:$PATH"

# Verifies that SnapCd.Server.Core/Mcp/Generated/*.cs matches what SnapCd.Mcp.Generator
# would emit from the current controller annotations. Exits non-zero if any generated
# file would change — call `dotnet run --project SnapCd.Mcp.Generator` to fix.
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

exec dotnet run --project generators/SnapCd.Mcp.Generator/SnapCd.Mcp.Generator.csproj -c Release -- $MODE
