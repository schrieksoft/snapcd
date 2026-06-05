// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Auth;

public class AgentClaimAuditMiddleware
{
    public const string HttpContextItemKey = "AgentId";

    private readonly RequestDelegate _next;

    public AgentClaimAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var claim = context.User?.FindFirst("agent_id")?.Value;
        if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var agentId))
        {
            context.Items[HttpContextItemKey] = agentId;
        }
        await _next(context);
    }
}
