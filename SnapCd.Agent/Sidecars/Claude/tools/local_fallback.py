# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.

"""Optional dev-mode SnapCd tools over REST, for offline use when the SnapCd MCP
endpoint isn't reachable but the REST API is.

Not registered by default. To enable, build the server and merge it into the
agent's ``mcp_servers`` (an in-process SDK MCP server). The REST paths below are
placeholders — align them with the server's actual routes before relying on this.
"""
from __future__ import annotations

import httpx
from claude_agent_sdk import create_sdk_mcp_server, tool


def build_local_fallback_server(base_url: str, token: str):
    headers = {"Authorization": f"Bearer {token}"}

    async def _get(path: str) -> str:
        async with httpx.AsyncClient(base_url=base_url, headers=headers, timeout=30) as client:
            response = await client.get(path)
            response.raise_for_status()
            return response.text

    @tool("get_job", "Fetch a SnapCd module job by id.", {"jobId": str})
    async def get_job(args):
        return {"content": [{"type": "text", "text": await _get(f"/api/module-jobs/{args['jobId']}")}]}

    @tool("get_job_logs", "Fetch redacted logs for a module job.", {"jobId": str})
    async def get_job_logs(args):
        return {"content": [{"type": "text", "text": await _get(f"/api/module-jobs/{args['jobId']}/logs")}]}

    return create_sdk_mcp_server(name="snapcd_local", version="1.0.0", tools=[get_job, get_job_logs])
