// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

var builder = DistributedApplication.CreateBuilder(args);

var snapcdBaseUrl = builder.Configuration["Sidecar:SnapcdBaseUrl"]
    ?? throw new InvalidOperationException("Sidecar:SnapcdBaseUrl is not set (appsettings.json).");
var claudeCodeOAuthToken = builder.Configuration["Sidecar:ClaudeCodeOAuthToken"];
var anthropicApiKey = builder.Configuration["Sidecar:AnthropicApiKey"];
// GitHub PAT the sidecar's pre-installed `gh`/`git` use for the AutoFix code path (clone + open PR).
// Read by gh straight from the process env as GITHUB_TOKEN — nothing in config.py consumes it.
var gitHubToken = builder.Configuration["Sidecar:GitHubToken"];

// Run the sidecar as a container built from its Dockerfile (not a host uvicorn process) so the
// image's git + gh are present for the AutoFix code path — dev matches prod. The container listens on
// $PORT (default 7001); Aspire injects it and proxies the endpoint.
var claudeSidecar = builder.AddDockerfile("claude-sidecar", "../SnapCd.Agent/Sidecars/Claude")
    .WithHttpEndpoint(targetPort: 7001, env: "PORT")
    .WithHttpHealthCheck("/health")
    // The sidecar reaches the host-run SnapCd server via host.docker.internal; on Linux that needs the
    // host-gateway mapping. SnapcdBaseUrl points at the server's plain-HTTP dev port (AllowHttp=true), so
    // there's no host dev TLS cert to trust from inside the container.
    .WithContainerRuntimeArgs("--add-host=host.docker.internal:host-gateway")
    .WithEnvironment("SNAPCD_BASE_URL", snapcdBaseUrl);

if (!string.IsNullOrWhiteSpace(claudeCodeOAuthToken))
    claudeSidecar.WithEnvironment("CLAUDE_CODE_OAUTH_TOKEN", claudeCodeOAuthToken);
if (!string.IsNullOrWhiteSpace(anthropicApiKey))
    claudeSidecar.WithEnvironment("ANTHROPIC_API_KEY", anthropicApiKey);
if (!string.IsNullOrWhiteSpace(gitHubToken))
    claudeSidecar.WithEnvironment("GITHUB_TOKEN", gitHubToken);

builder.AddProject<Projects.SnapCd_Agent>("snapcd-agent")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("Agent__Sidecars__0__Name", "claude")
    .WithEnvironment("Agent__Sidecars__0__BaseUrl", claudeSidecar.GetEndpoint("http"))
    // DCP injects SSL_CERT_DIR pointing at its own per-resource cert dir (and that override wins over
    // WithEnvironment), which makes OpenSSL — and thus .NET on Linux — stop consulting the system CA
    // store, so the orchestrator can't validate the externally-run SnapCd server's dev cert
    // (PartialChain). DCP does NOT set SSL_CERT_FILE, so pointing it at the system CA bundle (which
    // includes the dev cert via update-ca-certificates) restores trust *alongside* DCP's dir rather
    // than fighting it. The sidecar HTTPS peer is validated by a separate Development-only
    // accept-any handler, so this only affects the orchestrator→server hop.
    .WithEnvironment("SSL_CERT_FILE", "/etc/ssl/certs/ca-certificates.crt")
    .WaitFor(claudeSidecar);

builder.Build().Run();
