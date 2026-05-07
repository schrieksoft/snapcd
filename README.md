# Snap CD

Snap CD is a CI/CD platform for infrastructure. It drives Terraform and Pulumi
engines to plan and apply changes across modules, stacks, and namespaces, with
approval flows, audit trails, and access controls.

## Components

- **SnapCd.Server.Host** — main HTTP + Blazor application. Hosts the dashboard,
  API, and admin UI; orchestrates engine runs.
- **SnapCd.Runner** — worker process that executes engine operations on
  behalf of the server. Deployed per-environment; connects back over SignalR.
- **SnapCd.Contracts / SnapCd.Server.Core** — shared DTOs, contracts, and
  server-side utilities. Published as NuGet packages for downstream consumers.

## License

Source-available under the **Snap CD Source-Available License 1.0**. See [`LICENSE.md`](./LICENSE.md).

## Getting started

Requirements:

- .NET 10 SDK (preview accepted until GA)
- Docker + Docker Compose
- SQL Server (via the provided `docker-compose.yml`)

Build:

    dotnet build SnapCd.sln

Run locally:

    docker compose up -d
    dotnet run --project SnapCd.Server

The server listens on `https://localhost:20002` by default.

See `SECRETS.md` for how to configure local credentials, and
`CONTRIBUTING.md` for how to propose changes.

## Docker

Images published to GHCR on every release:

- `ghcr.io/schrieksoft/snapcd/snapcd-server:<version>`
- `ghcr.io/schrieksoft/snapcd/snapcd-runner:<version>`
- `ghcr.io/schrieksoft/snapcd/snapcd-runner-azure:<version>` — runner image
  with the Azure CLI preinstalled.

## Reporting issues

- Bugs and feature requests: GitHub Issues.
- Security issues: see `SECURITY.md` — please email, do not file a public issue.
