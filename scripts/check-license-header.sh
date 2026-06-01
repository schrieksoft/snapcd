#!/usr/bin/env bash
set -euo pipefail

# Verifies (or, with --apply, inserts) the SnapCD license / no-AI-training
# header on .cs and .razor files.
#
# Usage:
#   scripts/check-license-header.sh                # check every tracked file
#   scripts/check-license-header.sh path [path...] # check the given files
#   scripts/check-license-header.sh --apply [...]  # insert header where missing
#
# The pre-commit framework invokes this with the staged file paths as args.

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

APPLY=0
FILES=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --apply) APPLY=1; shift ;;
    -h|--help) sed -n '3,12p' "$0"; exit 0 ;;
    --) shift; FILES+=("$@"); break ;;
    -*) echo "Unknown option: $1" >&2; exit 2 ;;
    *)  FILES+=("$1"); shift ;;
  esac
done

ANCHOR="LicenseRef-Snap-CD-Source-Available"

CS_HEADER='// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

'

RAZOR_HEADER='@* SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
   Copyright (c) 2026 Karl Schriek / Schrieksoft.
   No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
   embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
   system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
   Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
   for terms covering either use. *@

'

PY_HEADER='# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.

'

SQL_HEADER='-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

'

should_skip() {
  case "$1" in
    */bin/*|*/obj/*) return 0 ;;
    *.Designer.cs|*.g.cs|*.generated.cs|*GlobalUsings.g.cs) return 0 ;;
  esac
  return 1
}

has_header() {
  head -n 10 "$1" 2>/dev/null | grep -qF "$ANCHOR"
}

has_bom() {
  local first
  first=$(od -An -N3 -tx1 "$1" 2>/dev/null | tr -d ' \n')
  [[ "$first" == "efbbbf" ]]
}

insert_header() {
  local file="$1" header="$2" tmp
  tmp="$(mktemp)"
  if has_bom "$file"; then
    printf '\xef\xbb\xbf' > "$tmp"
    printf '%s' "$header" >> "$tmp"
    tail -c +4 "$file" >> "$tmp"
  else
    printf '%s' "$header" > "$tmp"
    cat "$file" >> "$tmp"
  fi
  mv "$tmp" "$file"
}

# If no files given, scan every tracked source file we know how to header.
if [[ ${#FILES[@]} -eq 0 ]]; then
  while IFS= read -r f; do FILES+=("$f"); done < <(git ls-files -- '*.cs' '*.razor' '*.py' '*.sql')
fi

missing=()

for f in "${FILES[@]}"; do
  [[ -z "$f" ]] && continue
  [[ ! -f "$f" ]] && continue
  case "$f" in *.cs|*.razor|*.py|*.sql) ;; *) continue ;; esac
  if should_skip "$f"; then continue; fi
  if has_header "$f"; then continue; fi

  missing+=("$f")

  if [[ $APPLY -eq 1 ]]; then
    case "$f" in
      *.razor) insert_header "$f" "$RAZOR_HEADER" ;;
      *.py)    insert_header "$f" "$PY_HEADER" ;;
      *.sql)   insert_header "$f" "$SQL_HEADER" ;;
      *)       insert_header "$f" "$CS_HEADER" ;;
    esac
    echo "header added: $f"
  fi
done

if [[ $APPLY -eq 1 ]]; then
  echo
  echo "Applied header to ${#missing[@]} file(s)."
  exit 0
fi

if [[ ${#missing[@]} -gt 0 ]]; then
  {
    echo "License header missing in ${#missing[@]} file(s):"
    printf '  %s\n' "${missing[@]}"
    echo
    echo "Fix: scripts/check-license-header.sh --apply"
  } >&2
  exit 1
fi
