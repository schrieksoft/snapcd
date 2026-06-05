// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Configuration;
using SnapCd.Agent.Configuration;
using SnapCd.Agent.Hub;
using SnapCd.Agent.Services;
using SnapCd.Agent.Services.Sidecars;

var builder = Host.CreateApplicationBuilder(args);

// Config: appsettings.json + appsettings.{Environment}.json + Agent__* environment variables.
builder.Services
    .AddOptions<AgentOptions>()
    .Bind(builder.Configuration.GetSection(AgentOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<ServerSettings>()
    .Bind(builder.Configuration.GetSection(ServerSettings.SectionName))
    .ValidateOnStart();

builder.Services.AddHttpClient();

var configuredOptions = builder.Configuration.GetSection(AgentOptions.SectionName).Get<AgentOptions>() ?? new AgentOptions();
foreach (var sidecar in configuredOptions.Sidecars)
{
    var sidecarClient = builder.Services.AddHttpClient($"sidecar:{sidecar.Name}");
    if (builder.Environment.IsDevelopment())
    {
        sidecarClient.ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    }
}

builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<SidecarRegistry>();
builder.Services.AddSingleton<SnapCd.Agent.Missions.Missions>();
builder.Services.AddSingleton<AgentHubConnection>();

builder.Services.AddHostedService<AgentSessionHostedService>();
builder.Services.AddHostedService<SidecarSupervisor>();

var host = builder.Build();
await host.RunAsync();
