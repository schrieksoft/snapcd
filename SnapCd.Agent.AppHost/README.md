# SnapCd.Agent.AppHost

Dev-time [Aspire](https://aspire.dev) orchestrator. Running this builds and starts
the `claude-sidecar` **container** (from `SnapCd.Agent/Sidecars/Claude/Dockerfile`)
and then `SnapCd.Agent`, waiting for the sidecar's `/health` before the agent
starts dispatching. Dev convenience only — production lifecycle is owned by the
deployment layer, not this AppHost.

The sidecar runs as a container (not a host `uv`/uvicorn process) so its image —
Python, the Claude Code CLI, `uv`, **`git`, and `gh`** — is the single runtime,
identical in dev and prod. The AutoFix mission's code path (clone the module
source, open a PR) needs `git`/`gh`, which is why the container is used.

## Prerequisites

- .NET 10 SDK
- A **container runtime** (Docker) running and reachable. `uv`, Python, the Claude
  CLI, `git`, and `gh` all live inside the sidecar image, so they are **not**
  required on the host.
- The SnapCd server running and reachable **from a container over plain HTTP** —
  see "SnapCd server" below.

## SnapCd server (HTTP dev port)

A container can't reach the host's `localhost`, and it doesn't trust the host's
`localhost` dev TLS cert. So in dev the server exposes a plain-HTTP port and the
sidecar talks to it over the host gateway:

- Run `SnapCd.Server.Host` with its dev launch profile — it binds
  `http://0.0.0.0:40002` (alongside `https://localhost:20002`) and sets
  `AllowHttp=true`, which turns off HTTPS redirection/HSTS and relaxes
  `RequireHttpsMetadata` so Bearer-authenticated `/mcp` calls work over HTTP.
- `Sidecar:SnapcdBaseUrl` (in `appsettings.json`) is `http://host.docker.internal:40002`;
  `Program.cs` adds `--add-host=host.docker.internal:host-gateway` so the Linux
  container can resolve the host.

(The browser still uses `https://localhost:20002`, and the `SnapCd.Agent` process —
which runs on the host, not in a container — reaches the server over HTTPS as usual.)

## Claude auth

The sidecar runs the Claude Code CLI inside the container, so it can't use an
ambient `~/.claude` login on your host. Provide a token in
`appsettings.Development.json` (injected into the container as an env var):

- **Subscription (Pro/Max):** `claude setup-token` → `Sidecar:ClaudeCodeOAuthToken`.
- **Console API key (per-token billing):** `Sidecar:AnthropicApiKey`.

`Program.cs` injects whichever is set; in container mode one of them is required.

For the **AutoFix** mission's code path (clone the module source + open a PR), set a
**GitHub PAT** as `Sidecar:GitHubToken` — injected into the container as
`GITHUB_TOKEN`, which the image's `gh`/`git` pick up. Leave it unset if you're not
exercising AutoFix. (Fine-grained PAT needs Contents + Pull requests: read/write on
the target repos.)

## Run

```bash
dotnet run --project SnapCd.Agent.AppHost
```

The console prints the Aspire dashboard URL (with a login token); both resources,
their logs, and health show up there. The **first run builds the sidecar image**, so
it's slower; subsequent runs reuse cached layers.

## Wiring

`Program.cs`:
- `AddDockerfile("claude-sidecar", "../SnapCd.Agent/Sidecars/Claude")` builds + runs
  the sidecar container; `WithHttpEndpoint(targetPort: 7001, env: "PORT")` maps its
  port; `WithContainerRuntimeArgs("--add-host=host.docker.internal:host-gateway")`
  lets it reach the host.
- Injects `SNAPCD_BASE_URL` (+ optional Claude auth + `GITHUB_TOKEN`) into the
  container, and the sidecar's resolved URL into the agent as
  `Agent__Sidecars__0__BaseUrl` (with `Agent__Sidecars__0__Name=claude`) so the
  agent's `SidecarRegistry` finds it at the Aspire-allocated address.
- The agent's own settings (SnapCd server URL, org / agent ids, client credentials)
  come from its appsettings as usual.
