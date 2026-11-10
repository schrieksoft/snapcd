# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.


"""Credentials read from files, for a vault-backed volume such as a Secrets Store CSI mount."""
from __future__ import annotations

import os
from pathlib import Path

# Read by the Claude Code CLI, gh and git, which inherit the sidecar's environment.
CREDENTIAL_VARIABLES = ("CLAUDE_CODE_OAUTH_TOKEN", "ANTHROPIC_API_KEY", "GITHUB_TOKEN")


def load_credential_files(environ: dict[str, str] | None = None) -> list[str]:
    """Set each credential variable from the file named in `<NAME>_FILE`, unless the variable is already set.

    Returns the names that were loaded.
    """
    env = os.environ if environ is None else environ
    loaded = []
    for name in CREDENTIAL_VARIABLES:
        path = env.get(f"{name}_FILE")
        if not path or env.get(name):
            continue
        env[name] = Path(path).read_text(encoding="utf-8").strip()
        loaded.append(name)
    return loaded
