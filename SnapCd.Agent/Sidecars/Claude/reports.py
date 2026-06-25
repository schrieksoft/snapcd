# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.

"""In-process MCP server exposing the structured-report tools the agent must call before terminating.

Only AutoDiagnose / AutoFix skills tell the agent to invoke ``report_diagnosis_category`` — other
skills will simply not call it. The values come from ``SnapCd.Contracts.DiagnosisCategory``; the tool's
``input_schema`` declares them as a JSON-Schema ``enum`` so the SDK rejects anything off-list before
the call even reaches Python.
"""
from __future__ import annotations

from typing import Any

from claude_agent_sdk import create_sdk_mcp_server, tool


# Mirrors SnapCd.Contracts.DiagnosisCategory. Wire values are PascalCase so the orchestrator can parse
# them with Enum.TryParse without a converter. Add at the end; never rename.
DIAGNOSIS_CATEGORIES = [
    "Unknown",
    "ProviderTransient",
    "ProviderAuth",
    "ModuleCode",
    "Configuration",
    "StateDrift",
    "Dependency",
    "Quota",
    "DeclinedApproval",
    "ExternalMutation",
]


class ReportCapture:
    """Per-invocation mutable holder for everything the agent reports through these tools."""

    def __init__(self) -> None:
        self.diagnosis_category: str | None = None


def build_reports_server(capture: ReportCapture) -> dict[str, Any]:
    """Build an in-process SDK MCP server whose tools mutate ``capture``."""

    @tool(
        "report_diagnosis_category",
        "Commit your final diagnosis category. Call this exactly once before producing your final "
        "summary. Pick the single value from the enum that best matches your conclusion; if nothing "
        "fits, use Unknown.",
        {
            "type": "object",
            "properties": {
                "category": {
                    "type": "string",
                    "enum": DIAGNOSIS_CATEGORIES,
                    "description": "One of the listed categories.",
                },
            },
            "required": ["category"],
        },
    )
    async def report_diagnosis_category(args: dict[str, Any]) -> dict[str, Any]:
        capture.diagnosis_category = args["category"]
        return {"content": [{"type": "text", "text": f"Recorded diagnosis category: {args['category']}"}]}

    @tool(
        "report_milestone",
        "Post a short progress milestone the human watching can see live (e.g. 'investigating', "
        "'diagnosed', 'opened PR'). Call this at each meaningful checkpoint of your work — it does NOT "
        "end the mission. Keep the message to one human-readable line.",
        {
            "type": "object",
            "properties": {
                "message": {
                    "type": "string",
                    "description": "One human-readable line describing what just happened / is happening.",
                },
                "kind": {
                    "type": "string",
                    "description": "Optional short label for the checkpoint, e.g. investigating / diagnosed / fixing / pr_opened / retried / blocked.",
                },
            },
            "required": ["message"],
        },
    )
    async def report_milestone(args: dict[str, Any]) -> dict[str, Any]:
        # The milestone is streamed out by the session manager when it observes this tool-use
        # (see session_manager._run_once_streaming); here we just acknowledge to the agent.
        return {"content": [{"type": "text", "text": "Milestone recorded."}]}

    return create_sdk_mcp_server("reports", "1.0", [report_diagnosis_category, report_milestone])
