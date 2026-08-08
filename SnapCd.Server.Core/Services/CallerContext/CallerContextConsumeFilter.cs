// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;

namespace SnapCd.Server.Core.Services.CallerContext;

/// <summary>
/// Opens a System caller scope around every bus message consumption. Humans act over HTTP and
/// SignalR, never as message consumers, so any write made while consuming — saga activities,
/// cascade consumers, handler consumers — is machine-driven and exempt from the maintenance gate.
/// </summary>
public class CallerContextConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        using var _ = CallerContext.Begin(CallerKind.System);
        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("caller-context");
    }
}
