#!/usr/bin/env bash
set -euo pipefail

export PATH="$HOME/.dotnet:$PATH"

# Verifies that applications/snapcd/schemas/openapi.yaml matches what
# SnapCd.OpenApi.Generator would emit from the current controllers, DTOs and OpenAPI
# configuration. Exits non-zero if the file would change — call with --write to
# regenerate, then commit the result.
#
# Usage:
#   scripts/check-openapi-document.sh           # check
#   scripts/check-openapi-document.sh --write   # regenerate
#
# Invoked by pre-commit when a controller, DTO or the OpenAPI setup changes.
#
# The document is produced by the build-time `dotnet-getdocument` tool
# (Microsoft.Extensions.ApiDescription.Server) against the generator project, which
# registers only the controllers and the OpenAPI configuration — no database, bus or
# network — so this runs anywhere without infrastructure. The tool emits JSON; the
# committed artifact is YAML (readable diffs), converted via scripts/json-to-yaml.py.

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

PROJECT="generators/SnapCd.OpenApi.Generator/SnapCd.OpenApi.Generator.csproj"
TARGET="schemas/openapi.yaml"

OUT_DIR="$(mktemp -d)"
trap 'rm -rf "$OUT_DIR"' EXIT

# The tool skips regeneration when its cache looks current; drop it so --check always
# compares against a freshly emitted document.
rm -f generators/SnapCd.OpenApi.Generator/obj/*.OpenApiFiles.cache

dotnet build "$PROJECT" -c Release --nologo -v q \
    /p:OpenApiGenerateDocuments=true \
    /p:OpenApiDocumentsDirectory="$OUT_DIR" >/dev/null

GENERATED_JSON="$OUT_DIR/SnapCd.OpenApi.Generator.json"
if [[ ! -f "$GENERATED_JSON" ]]; then
    echo "check-openapi-document: no document was generated" >&2
    exit 1
fi

GENERATED_YAML="$OUT_DIR/openapi.yaml"
python3 scripts/json-to-yaml.py "$GENERATED_JSON" "$GENERATED_YAML"

# The artifact is committed and diffed, so generation must be deterministic: emit a
# second document and fail on any difference.
mv "$GENERATED_JSON" "$OUT_DIR/first.json"
rm -f generators/SnapCd.OpenApi.Generator/obj/*.OpenApiFiles.cache
dotnet build "$PROJECT" -c Release --nologo -v q \
    /p:OpenApiGenerateDocuments=true \
    /p:OpenApiDocumentsDirectory="$OUT_DIR" >/dev/null
if ! cmp -s "$OUT_DIR/first.json" "$GENERATED_JSON"; then
    echo "check-openapi-document: generation is not deterministic — two runs produced different documents" >&2
    diff <(python3 -m json.tool "$OUT_DIR/first.json") <(python3 -m json.tool "$GENERATED_JSON") | head -40 >&2
    exit 1
fi

if [[ ${1:-} == "--write" ]]; then
    if cmp -s "$GENERATED_YAML" "$TARGET"; then
        echo "check-openapi-document: $TARGET already up to date"
    else
        cp "$GENERATED_YAML" "$TARGET"
        echo "check-openapi-document: wrote $TARGET"
    fi
elif ! cmp -s "$GENERATED_YAML" "$TARGET"; then
    echo "check-openapi-document: $TARGET is stale — run scripts/check-openapi-document.sh --write" >&2
    exit 1
fi
