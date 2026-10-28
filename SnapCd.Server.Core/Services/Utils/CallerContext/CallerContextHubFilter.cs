// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR;
using SnapCd.Server.Core.Hubs;

namespace SnapCd.Server.Core.Services.CallerContext;

/// <summary>
/// Opens the matching caller scope around every hub invocation and connection event: runner
/// traffic on RunnerHub, agent traffic on AgentHub. Other hubs get no scope and stay gated.
/// </summary>
public class CallerContextHubFilter : IHubFilter
{
    private static CallerKind? KindFor(Hub hub) => hub switch
    {
        RunnerHub => CallerKind.Runner,
        AgentHub => CallerKind.Agent,
        _ => null
    };

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (KindFor(invocationContext.Hub) is not { } kind) return await next(invocationContext);
        using var _ = CallerContext.Begin(kind);
        return await next(invocationContext);
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        if (KindFor(context.Hub) is not { } kind)
        {
            await next(context);
            return;
        }

        using var _ = CallerContext.Begin(kind);
        await next(context);
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        if (KindFor(context.Hub) is not { } kind)
        {
            await next(context, exception);
            return;
        }

        using var _ = CallerContext.Begin(kind);
        await next(context, exception);
    }
}
