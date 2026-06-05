# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.

"""Resolves a skill name + parameters into a rendered prompt.

The SnapCd MCP server is the single source of truth: `prompts/get` returns the
skill body with `{{placeholder}}` arguments already substituted server-side.
"""
from __future__ import annotations

from mcp import ClientSession
from mcp.client.streamable_http import streamablehttp_client


class McpSkillResolver:
    def __init__(self, base_url: str) -> None:
        self._url = f"{base_url.rstrip('/')}/mcp"

    async def resolve(self, name: str, params: dict[str, str], token: str | None = None) -> str | None:
        headers = {"Authorization": f"Bearer {token}"} if token else {}
        async with streamablehttp_client(self._url, headers=headers) as (read, write, _):
            async with ClientSession(read, write) as session:
                await session.initialize()
                result = await session.get_prompt(name, dict(params))
        parts = [text for message in result.messages if (text := getattr(message.content, "text", None))]
        return "\n\n".join(parts) or None
