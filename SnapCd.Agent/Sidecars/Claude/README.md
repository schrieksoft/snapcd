# claude-sidecar

Provider-specific AI sidecar for the SnapCd Agent orchestrator (`SnapCd.Agent`). It
exposes the uniform sidecar contract — `POST /invoke` and `GET /health` — and runs
each mission with the [Claude Agent SDK](https://code.claude.com/docs/en/agent-sdk/python)
against the SnapCd MCP server.

## Per-invocation flow

1. `POST /invoke` receives `{ mission, skill, parameters, session, snapcdMcpToken, correlationId }`.
2. The skill is resolved to a rendered prompt via the SnapCd MCP server's
   `prompts/get` (the server is the single source of truth for skill bodies).
3. The agent loop runs with the SnapCd MCP server (bearer = `snapcdMcpToken`, set
   per call) plus any `EXTERNAL_MCP_CONFIG` servers. The Agent SDK discovers the
   tool catalog from those servers — there is no separate catalog step.
4. A structured result is returned (success, summary, duration, tool calls, token
   usage, session id).

`session.mode` is `ephemeral` (fresh loop per event) or `persistent` (resume the
same SDK session per `session.key`, with `rotation` budgets). Persistent sessions
get API prompt-cache reuse automatically.

## Configuration

All via environment (see `.env.example`):

| Var | Default | Notes |
| --- | --- | --- |
| `SNAPCD_BASE_URL` | — | SnapCd server base; MCP is `{base}/mcp`. |
| `CLAUDE_CODE_OAUTH_TOKEN` | — | Pro/Max subscription token (`claude setup-token`). |
| `ANTHROPIC_API_KEY` | — | Console API key alternative (per-token billing). |
| `EXTERNAL_MCP_CONFIG` | `{}` | JSON map `name -> McpServerConfig`, e.g. `{"slack":{"type":"http","url":"…","headers":{"Authorization":"Bearer …"}}}`. |

Auth is handled by the bundled Claude Code CLI, not by the sidecar — it reads
whichever of `ANTHROPIC_API_KEY` / `CLAUDE_CODE_OAUTH_TOKEN` is in the process
environment, or falls back to an ambient `~/.claude` login on the host.
| `CLAUDE_MODEL` | `claude-opus-4-7` | |
| `CLAUDE_MAX_TURNS` | `24` | |
| `CLAUDE_PERMISSION_MODE` | `bypassPermissions` | Headless, no human approval. |

## Run

```bash
uv sync
uv run uvicorn main:app --reload --port 7001
```

```bash
docker build -t claude-sidecar .
docker run --rm -p 7001:7001 --env-file .env claude-sidecar
```

The Agent SDK shells out to the Claude Code CLI, so it must be on `PATH`
(`npm i -g @anthropic-ai/claude-code`); the Docker image installs it.

## Notes / deviations from the plan

- Skills are MCP-only — the server is the single source of truth, so there is no
  bundled/local skill copy and no `SKILL_SOURCE`. (The plan's `LocalSkillResolver`
  fallback was dropped on purpose.)
- Adds two deps beyond the plan's list: `mcp` (for `prompts/get`) and
  `pydantic-settings` (env config).
- `tools/local_fallback.py` is opt-in dev scaffolding; its REST paths are
  placeholders to be aligned with the server's actual routes.
- Dev auto-startup is handled by the `SnapCd.Agent.AppHost` Aspire project, which
  launches this sidecar (via `uv`) alongside the orchestrator.
