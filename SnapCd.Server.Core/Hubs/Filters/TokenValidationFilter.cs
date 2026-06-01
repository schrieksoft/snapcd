// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;

namespace SnapCd.Server.Core.Hubs.Filters;

/// <summary>
/// Hub filter that validates JWT token expiration on every hub method invocation.
/// Throws HubException if token is expired, allowing runner to detect and reconnect.
/// </summary>
public class TokenValidationFilter : IHubFilter
{
    private readonly ILogger<TokenValidationFilter> _logger;

    public TokenValidationFilter(ILogger<TokenValidationFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var user = invocationContext.Context.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            // Get the expiration claim from the JWT token
            var expClaim = user.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

            if (!string.IsNullOrEmpty(expClaim) && long.TryParse(expClaim, out var exp))
            {
                // Convert Unix timestamp to DateTime
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                var now = DateTime.UtcNow;

                if (now >= expirationTime)
                {
                    _logger.LogWarning(
                        "Token expired for connection {ConnectionId}. Expiration: {Expiration}, Now: {Now}",
                        invocationContext.Context.ConnectionId,
                        expirationTime,
                        now);

                    throw new HubException("TokenExpired: The authentication token has expired. Please reconnect with a fresh token.");
                }
            }
        }

        // Token is valid, proceed with the invocation
        return await next(invocationContext);
    }
}
