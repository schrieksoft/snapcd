# SnapCd.Agent.AppHost

Dev-time [Aspire](https://aspire.dev) orchestrator. Running this brings up the
`claude-sidecar` (Python/uvicorn, launched via `uv`) and then `SnapCd.Agent`,
waiting for the sidecar's `/health` before the agent starts dispatching. It's a
dev convenience only — production lifecycle is owned by the deployment layer, not
this AppHost.

## Prerequisites

- .NET 10 SDK
- `uv` on `PATH` (the toolkit runs the sidecar with `uv run`; `uv` provisions the
  Python env from the sidecar's `pyproject.toml` on first run)
- The Claude Code CLI on `PATH` (`claude`), authenticated — see below
- A running SnapCd server reachable at `Sidecar:SnapcdBaseUrl`

> Both `uv` and `claude` must be visible to the GUI process tree if you launch
> from Rider — see `~/.config/environment.d` in the install notes.

## Claude auth

The sidecar runs the Claude Code CLI, so it authenticates the way the CLI does.
Pick one:

- **Subscription (Pro/Max):** run `claude` → `/login` once and choose your Claude
  account. The CLI stores credentials under `~/.claude`, which the sidecar reuses —
  no token in config. (Or `claude setup-token` → put it in `appsettings.Development.json`
  as `Sidecar:ClaudeCodeOAuthToken`.)
- **Console API key (per-token billing):** put it in `appsettings.Development.json`
  as `Sidecar:AnthropicApiKey`.

`Program.cs` only injects an env var if the corresponding key is set; with neither
set it relies on the ambient `~/.claude` login.

## Run

```bash
dotnet run --project SnapCd.Agent.AppHost
```

The console prints the Aspire dashboard URL (with a login token); both resources,
their logs, and health show up there. `Sidecar:SnapcdBaseUrl` defaults to
`https://localhost:20002` (`appsettings.json`). Use the `https` launch profile
only if you've trusted the .NET dev cert; otherwise the default profile is `http`.

## Wiring

`Program.cs` assigns the sidecar a port (`WithHttpEndpoint(env: "UVICORN_PORT")`),
passes `SNAPCD_BASE_URL` (+ optional Claude auth) into it, and injects the
sidecar's resolved URL into the agent as `Agent__Sidecars__0__BaseUrl` (with
`Agent__Sidecars__0__Name=claude`), so the agent's `SidecarRegistry` finds it at
the Aspire-allocated address. The agent's own settings (SnapCd server URL, org /
agent ids, client credentials) come from its appsettings as usual.
