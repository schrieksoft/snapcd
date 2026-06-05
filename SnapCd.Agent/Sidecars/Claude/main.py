# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.

"""claude-sidecar: the FastAPI front door the SnapCd Agent orchestrator invokes.

POST /invoke runs one mission (resolve skill -> render prompt -> run agent loop
against the SnapCd MCP server with the supplied bearer token) and streams the run
back as Server-Sent Events: a `log` event per assistant text / tool-use, then a
final `result` event. GET /health is a liveness probe used by the orchestrator's
SidecarSupervisor.
"""
from __future__ import annotations

import json
from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.responses import StreamingResponse
from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel

from config import get_settings
from mcp_clients import build_mcp_servers
from reports import ReportCapture, build_reports_server
from session_manager import SessionManager
from skill_resolver import McpSkillResolver


class _CamelModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="ignore")


class SessionSpec(_CamelModel):
    mode: str = "ephemeral"
    key: str = "default"
    rotation: dict[str, str] | None = None


class InvokeRequest(_CamelModel):
    mission: str
    skill: str
    parameters: dict[str, str] = Field(default_factory=dict)
    session: SessionSpec = Field(default_factory=SessionSpec)
    snapcd_mcp_token: str
    correlation_id: str | None = None


def _strip_bearer(token: str) -> str:
    return token[7:] if token.lower().startswith("bearer ") else token


@asynccontextmanager
async def lifespan(app: FastAPI):
    settings = get_settings()
    app.state.settings = settings
    app.state.resolver = McpSkillResolver(settings.snapcd_base_url)
    app.state.sessions = SessionManager(settings)
    app.state.external = settings.external_mcp_servers()
    yield


app = FastAPI(title="claude-sidecar", lifespan=lifespan)


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}


def _sse(event: dict) -> str:
    return f"data: {json.dumps(event)}\n\n"


def _unwrap_exception(ex: BaseException) -> tuple[str, str]:
    """Drill into ExceptionGroup wrappers so the orchestrator sees the real cause.

    asyncio's TaskGroup wraps any child failure in ExceptionGroup whose top-level
    message is the useless "unhandled errors in a TaskGroup (N sub-exception)"; the
    actual exception lives in `.exceptions`. We recurse into the first sub-exception
    (the SDK only ever raises one in practice) so the result event carries the real
    error type and message instead of the wrapper's.
    """
    current: BaseException = ex
    while isinstance(current, BaseExceptionGroup) and current.exceptions:
        current = current.exceptions[0]
    return type(current).__name__, str(current) or repr(current)


@app.post("/invoke")
async def invoke(req: InvokeRequest) -> StreamingResponse:
    settings = app.state.settings
    token = _strip_bearer(req.snapcd_mcp_token)

    async def event_stream() -> AsyncIterator[str]:
        try:
            prompt = await app.state.resolver.resolve(req.skill, req.parameters, token)
            if not prompt:
                yield _sse({"type": "result", "result": {"success": False, "error": "SkillNotResolved", "detail": req.skill}})
                return

            servers = build_mcp_servers(settings.snapcd_base_url, token, app.state.external)
            # Per-invocation structured-report capture; the report MCP server's tools mutate this object,
            # and we splice the captured values into the final SSE result event below.
            capture = ReportCapture()
            servers["reports"] = build_reports_server(capture)

            async for event in app.state.sessions.run_streaming(
                key=req.session.key,
                mode=req.session.mode,
                rotation=req.session.rotation,
                prompt=prompt,
                servers=servers,
            ):
                if event.get("type") == "result":
                    event["result"]["diagnosis_category"] = capture.diagnosis_category
                yield _sse(event)
        except Exception as ex:  # noqa: BLE001 - surfaced to the orchestrator as a failed invocation
            error, detail = _unwrap_exception(ex)
            yield _sse({"type": "result", "result": {"success": False, "error": error, "detail": detail}})

    return StreamingResponse(event_stream(), media_type="text/event-stream")
