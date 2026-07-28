# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.

#!/usr/bin/env python3
"""Deterministic JSON -> YAML conversion for the committed schema artifacts.

Key order is preserved (sort_keys=False) so the output is stable across runs and
diffs stay minimal. Used by check-settings-schemas.sh and check-openapi-document.sh.
"""
import json
import sys

import yaml


class Dumper(yaml.SafeDumper):
    pass


def _str_representer(dumper, value):
    # Multi-line strings (markdown descriptions) as literal blocks: real single
    # line breaks in the source instead of quoted-and-folded blank-line pairs.
    if "\n" in value:
        value = "\n".join(line.rstrip() for line in value.splitlines())
        return dumper.represent_scalar("tag:yaml.org,2002:str", value, style="|")
    return dumper.represent_scalar("tag:yaml.org,2002:str", value)


Dumper.add_representer(str, _str_representer)

with open(sys.argv[1]) as f:
    data = json.load(f)

with open(sys.argv[2], "w") as f:
    yaml.dump(data, f, Dumper=Dumper, sort_keys=False, allow_unicode=True, width=120)
