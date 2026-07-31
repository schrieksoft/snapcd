#!/usr/bin/env bash
set -euo pipefail

export PATH="$HOME/.dotnet:$PATH"

# Runs the generated-artifact checks (MCP surface, settings schemas, OpenAPI document) behind a
# single shared build: one MSBuild invocation builds every generator and their common dependency
# graph exactly once, then the individual checks run without building and in parallel. Used by
# pre-commit instead of three individual hooks; the individual scripts remain directly invocable
# (they build for themselves when run standalone).
#
# Which checks run is decided from the staged files, mirroring the per-hook `files:` filters this
# replaced — a settings-only commit must not pay for the MCP generator's Roslyn analysis. With
# nothing staged (manual invocation, CI --all-files) every check runs.
#
# Usage:
#   scripts/check-generated-artifacts.sh           # check
#   scripts/check-generated-artifacts.sh --write   # regenerate

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

MODE="${1:-}"

STAGED="$(git diff --cached --name-only)"

wants() {
    [[ -z "$STAGED" ]] || grep -qE "$1" <<< "$STAGED"
}

# generators.proj or the shared emitter utils affect everything.
ALWAYS='^generators/generators\.proj$|^SnapCd\.Utils/Settings/'

RUN_MCP=0
RUN_OPENAPI=0
RUN_SETTINGS=0
wants "$ALWAYS|^SnapCd\.Server\.Core/Controllers/.*\.cs$|^SnapCd\.Contracts/Mcp/|^generators/SnapCd\.Mcp\.Generator/|^SnapCd\.Server\.Core/AI/Mcp/Generated/" && RUN_MCP=1
wants "$ALWAYS|^SnapCd\.Server\.Core/Controllers/.*\.cs$|^SnapCd\.Contracts/Dto/|^SnapCd\.Server\.Core/Startup/(Scalar|Controllers|CurrentOrganizationOperationTransformer)\.cs$|^generators/SnapCd\.OpenApi\.Generator/|^schemas/openapi\.yaml$" && RUN_OPENAPI=1
wants "$ALWAYS|^SnapCd\.Runner/Settings/|^SnapCd\.Agent/Configuration/|^SnapCd\.Server\.Core/Settings/|^generators/SnapCd\.Settings\.Generator|^schemas/.*\.schema\.yaml$" && RUN_SETTINGS=1

if [[ $RUN_MCP == 0 && $RUN_OPENAPI == 0 && $RUN_SETTINGS == 0 ]]; then
    echo "check-generated-artifacts: no staged files affect generated artifacts"
    exit 0
fi

dotnet build generators/generators.proj -c Release --nologo -v q

export SNAPCD_GENERATORS_PREBUILT=1

pids=()
names=()

if [[ $RUN_MCP == 1 ]]; then
    scripts/check-mcp-surface.sh $MODE &
    pids+=($!); names+=(mcp-surface)
fi

if [[ $RUN_SETTINGS == 1 ]]; then
    scripts/check-settings-schemas.sh $MODE &
    pids+=($!); names+=(settings-schemas)
fi

if [[ $RUN_OPENAPI == 1 ]]; then
    scripts/check-openapi-document.sh $MODE &
    pids+=($!); names+=(openapi-document)
fi

rc=0
for i in "${!pids[@]}"; do
    if ! wait "${pids[$i]}"; then
        echo "check-generated-artifacts: ${names[$i]} failed" >&2
        rc=1
    fi
done
exit $rc
