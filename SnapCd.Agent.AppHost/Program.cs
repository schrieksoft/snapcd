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

var claudeSidecar = builder.AddUvicornApp("claude-sidecar", "../SnapCd.Agent/Sidecars/Claude", "main:app")
    .WithUv()
    .WithHttpEndpoint(env: "UVICORN_PORT")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("SNAPCD_BASE_URL", snapcdBaseUrl);

if (!string.IsNullOrWhiteSpace(claudeCodeOAuthToken))
    claudeSidecar.WithEnvironment("CLAUDE_CODE_OAUTH_TOKEN", claudeCodeOAuthToken);
if (!string.IsNullOrWhiteSpace(anthropicApiKey))
    claudeSidecar.WithEnvironment("ANTHROPIC_API_KEY", anthropicApiKey);

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
