# Contributing to SnapCD

Thanks for your interest in contributing.

## License

SnapCD is licensed under the **Snap CD Source-Available License 1.0** (see
`LICENSE.md`), a license derived from the Elastic License 2.0. It is not the
Elastic License 2.0 and is not endorsed by Elastic N.V.

## Contributor License Agreement

Before we can merge your first pull request, you will be asked to sign our
[Contributor License Agreement](CLA.md) (a bot will prompt you automatically on
your PR). The CLA grants us a broad license to your contribution, including the
right to relicense.

Please note: SnapCD is an open-core product — some features are gated behind
a paid Enterprise Edition license key. While contributions are generally
accepted into the open-source code paths and we do not set out to pull
community contributions into the commercial Enterprise Edition, **the CLA
permits us to do so, and it may happen** — for example, if an open-source
code path later becomes part of an Enterprise feature, or if a contribution
is directly relevant to one. If you are not comfortable with this
possibility, please do not contribute.

If your employer has rights to your contributions, please ensure you have
authorization before submitting.

## Getting started

Requirements:

- .NET 10 SDK (preview is acceptable until GA)
- Docker + Docker Compose
- SQL Server (use the provided `docker-compose.yml`)

Build:

    dotnet build SnapCd.sln

Run locally:

    docker compose up -d
    dotnet run --project SnapCd.Server

Copy `SnapCd.Server/appsettings.json` to
`SnapCd.Server/appsettings.Development.json` (already gitignored) and fill in
local secrets. See `SECRETS.md` for guidance.

## Pull requests

- Fork and branch from `main`.
- Keep PRs focused; one logical change per PR.
- Include tests for behavior changes.
- Follow the existing code style (EditorConfig enforces most of it).
- Pass `dotnet build` and `dotnet test` before requesting review.

## Code of Conduct

See `CODE_OF_CONDUCT.md`.

## Commit messages

Use a clear imperative subject line and explain *why* in the body rather than
restating *what* the diff shows. Reference issues with `#123`. No strict
format is enforced.
