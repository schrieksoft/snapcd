# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.

"""MCP server wiring passed to claude-agent-sdk: SnapCd (per-call bearer) + external servers."""
from __future__ import annotations

from typing import Any


def snapcd_mcp_server(base_url: str, token: str) -> dict[str, Any]:
    return {
        "type": "http",
        "url": f"{base_url.rstrip('/')}/mcp",
        "headers": {"Authorization": f"Bearer {token}"},
    }


def build_mcp_servers(base_url: str, token: str, external: dict[str, Any]) -> dict[str, Any]:
    servers: dict[str, Any] = {"snapcd": snapcd_mcp_server(base_url, token)}
    servers.update(external)
    return servers
