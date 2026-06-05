// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using ModelContextProtocol.Protocol;
using SnapCd.Server.Core.Services.Ai.Mcp;

namespace SnapCd.Server.Core.Startup;

public static class SnapCdMcpServer
{
    public static IServiceCollection AddSnapCdMcpServer(this IServiceCollection services)
    {
        services.AddSingleton<PromptRegistry>();

        services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly(typeof(SnapCdMcpServer).Assembly)
            .WithResourcesFromAssembly(typeof(SnapCdMcpServer).Assembly)
            .WithListPromptsHandler((ctx, _) =>
            {
                var registry = ctx.Services!.GetRequiredService<PromptRegistry>();
                return ValueTask.FromResult(new ListPromptsResult { Prompts = [.. registry.ListPrompts()] });
            })
            .WithGetPromptHandler((ctx, _) =>
            {
                var registry = ctx.Services!.GetRequiredService<PromptRegistry>();
                var name = ctx.Params?.Name
                    ?? throw new InvalidOperationException("prompts/get requires a 'name' parameter.");

                if (!registry.TryGet(name, ctx.Params?.Arguments, out var result, out var error))
                    throw new InvalidOperationException(error);

                return ValueTask.FromResult(result!);
            });

        return services;
    }
}
