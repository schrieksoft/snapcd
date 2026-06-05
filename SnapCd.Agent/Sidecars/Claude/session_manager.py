# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.

"""Per-mission agent session lifecycle: ephemeral vs persistent + rotation.

Ephemeral missions run a fresh agent loop per event. Persistent missions resume
the same claude-agent-sdk session (so prompt caching and context carry across
events for the same mission row), serialised per key and rotated by token /
event / age budget.
"""
from __future__ import annotations

import asyncio
import time
from collections.abc import AsyncIterator
from dataclasses import dataclass, field
from typing import Any

from claude_agent_sdk import (
    AssistantMessage,
    ClaudeAgentOptions,
    ResultMessage,
    TextBlock,
    ToolUseBlock,
    query,
)

SYSTEM_PROMPT = (
    "You are SnapCd's autonomous automation agent. You act on a single SnapCd "
    "domain event with no human in the loop. Use the provided MCP tools to gather "
    "context and take action; never ask questions. Follow the task instructions "
    "exactly and finish with a one-line summary of what you did."
)


@dataclass
class AgentResult:
    success: bool
    summary: str | None
    session_id: str | None
    duration_seconds: float
    tool_calls: list[dict[str, Any]]
    tokens: dict[str, Any]
    error: str | None = None
    detail: str | None = None


@dataclass
class _PersistentSession:
    session_id: str | None = None
    created_at: float = field(default_factory=time.monotonic)
    event_count: int = 0
    total_tokens: int = 0


def _parse_tool_use(block: ToolUseBlock) -> dict[str, Any]:
    if block.name.startswith("mcp__"):
        _, server, *rest = block.name.split("__")
        return {"target": server, "kind": "mcp", "tool": "__".join(rest) or server}
    return {"target": "local", "kind": "builtin", "tool": block.name}


def _seconds(spec: str) -> float:
    spec = spec.strip().lower()
    units = {"s": 1, "m": 60, "h": 3600, "d": 86400}
    if spec and spec[-1] in units:
        return float(spec[:-1]) * units[spec[-1]]
    return float(spec)


def _to_result(result_msg: ResultMessage | None, text_parts: list[str], tool_calls: list[dict[str, Any]]) -> AgentResult:
    summary = text_parts[-1].strip() if text_parts else None
    if result_msg is None:
        return AgentResult(False, summary, None, 0.0, tool_calls, {}, error="NoResult")

    usage = result_msg.usage or {}
    tokens = {
        "input": int(usage.get("input_tokens", 0) or 0),
        "output": int(usage.get("output_tokens", 0) or 0),
        "cache_hit": int(usage.get("cache_read_input_tokens", 0) or 0) > 0,
    }
    success = not result_msg.is_error
    return AgentResult(
        success=success,
        summary=result_msg.result or summary,
        session_id=result_msg.session_id,
        duration_seconds=round(result_msg.duration_ms / 1000.0, 3),
        tool_calls=tool_calls,
        tokens=tokens,
        error=None if success else (result_msg.subtype or "AgentError"),
    )


def _result_to_dict(result: AgentResult) -> dict[str, Any]:
    return {
        "success": result.success,
        "summary": result.summary,
        "error": result.error,
        "detail": result.detail,
        "duration_seconds": result.duration_seconds,
        "tool_calls": result.tool_calls,
        "tokens_used": result.tokens,
        "session_id": result.session_id,
    }


class SessionManager:
    def __init__(self, settings) -> None:
        self._settings = settings
        self._sessions: dict[str, _PersistentSession] = {}
        self._locks: dict[str, asyncio.Lock] = {}

    async def run_streaming(
        self, key: str, mode: str, rotation: dict[str, str] | None, prompt: str, servers: dict[str, Any]
    ) -> AsyncIterator[dict[str, Any]]:
        """Yields `{"type": "log", ...}` events as the agent works, then a final `{"type": "result", ...}`."""
        if mode == "persistent":
            lock = self._locks.setdefault(key, asyncio.Lock())
            async with lock:
                async for event in self._run_persistent_streaming(key, rotation, prompt, servers):
                    yield event
        else:
            async for event in self._run_once_streaming(prompt, servers, resume=None):
                yield event

    async def _run_persistent_streaming(
        self, key: str, rotation: dict[str, str] | None, prompt: str, servers: dict[str, Any]
    ) -> AsyncIterator[dict[str, Any]]:
        session = self._sessions.get(key)
        if session and self._should_rotate(session, rotation):
            session = None

        result_dict: dict[str, Any] | None = None
        async for event in self._run_once_streaming(prompt, servers, resume=session.session_id if session else None):
            if event["type"] == "result":
                result_dict = event["result"]
            yield event

        if session is None:
            session = _PersistentSession()
            self._sessions[key] = session
        if result_dict is not None:
            session.session_id = result_dict.get("session_id") or session.session_id
            session.event_count += 1
            tokens = result_dict.get("tokens_used") or {}
            session.total_tokens += int(tokens.get("input", 0)) + int(tokens.get("output", 0))

    def _should_rotate(self, session: _PersistentSession, rotation: dict[str, str] | None) -> bool:
        if not rotation:
            return False
        max_tokens = rotation.get("maxTokens")
        if max_tokens and session.total_tokens >= int(max_tokens):
            return True
        max_events = rotation.get("maxEvents")
        if max_events and session.event_count >= int(max_events):
            return True
        max_age = rotation.get("maxAge")
        if max_age and (time.monotonic() - session.created_at) >= _seconds(max_age):
            return True
        return False

    async def _run_once_streaming(
        self, prompt: str, servers: dict[str, Any], resume: str | None
    ) -> AsyncIterator[dict[str, Any]]:
        options = ClaudeAgentOptions(
            model=self._settings.model,
            system_prompt=SYSTEM_PROMPT,
            mcp_servers=servers,
            allowed_tools=[f"mcp__{name}" for name in servers],
            permission_mode=self._settings.permission_mode,
            max_turns=self._settings.max_turns,
            setting_sources=None,
            resume=resume,
        )

        text_parts: list[str] = []
        tool_calls: list[dict[str, Any]] = []
        result_msg: ResultMessage | None = None
        async for message in query(prompt=prompt, options=options):
            if isinstance(message, AssistantMessage):
                for block in message.content:
                    if isinstance(block, TextBlock):
                        text_parts.append(block.text)
                        yield {"type": "log", "level": "info", "message": block.text}
                    elif isinstance(block, ToolUseBlock):
                        call = _parse_tool_use(block)
                        tool_calls.append(call)
                        yield {"type": "log", "level": "info", "message": f"tool → {call['target']}.{call['tool']}"}
            elif isinstance(message, ResultMessage):
                result_msg = message

        yield {"type": "result", "result": _result_to_dict(_to_result(result_msg, text_parts, tool_calls))}
