// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace SnapCd.Server.Core.Services.Nonsecured.Federation;

public class FederatedAssertionResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public string? Issuer { get; init; }
    public string? Subject { get; init; }
    public ClaimsPrincipal? Principal { get; init; }

    public static FederatedAssertionResult Fail(string error) => new() { Succeeded = false, Error = error };
}

/// <summary>
/// Validates a token issued by an external platform (a Kubernetes cluster, a cloud identity
/// provider) against that issuer's published signing keys, so a workload can authenticate as a
/// Service Principal without a client secret.
/// </summary>
public class FederatedAssertionValidator
{
    private readonly FederationKeyProvider _keyProvider;
    private readonly ILogger<FederatedAssertionValidator> _logger;

    // Issuer and subject are read before anything is trusted, only to find the candidate trust
    // record. Nothing is granted on their basis until the signature has been checked.
    private static readonly JwtSecurityTokenHandler Handler = new() { MapInboundClaims = false };

    public FederatedAssertionValidator(FederationKeyProvider keyProvider, ILogger<FederatedAssertionValidator> logger)
    {
        _keyProvider = keyProvider;
        _logger = logger;
    }

    /// <summary>
    /// Reads the issuer and subject from an unvalidated assertion. The caller uses them to select
    /// a trust record and must then call <see cref="ValidateAsync"/> before granting anything.
    /// </summary>
    public (string? Issuer, string? Subject) PeekIssuerAndSubject(string assertion)
    {
        try
        {
            if (!Handler.CanReadToken(assertion))
                return (null, null);

            var token = Handler.ReadJwtToken(assertion);
            return (token.Issuer, token.Subject);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Assertion could not be read");
            return (null, null);
        }
    }

    public async Task<FederatedAssertionResult> ValidateAsync(
        string assertion,
        string issuer,
        string audience,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<SecurityKey> keys;
        try
        {
            keys = await _keyProvider.GetSigningKeysAsync(issuer, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Signing keys for issuer {Issuer} could not be retrieved", issuer);
            return FederatedAssertionResult.Fail("The signing keys of the credential's issuer could not be retrieved.");
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keys,
            ClockSkew = TimeSpan.FromMinutes(2),
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };

        try
        {
            var principal = Handler.ValidateToken(assertion, parameters, out var validatedToken);
            var jwt = (JwtSecurityToken)validatedToken;

            if (string.IsNullOrEmpty(jwt.Subject))
                return FederatedAssertionResult.Fail("The presented token carries no subject.");

            return new FederatedAssertionResult
            {
                Succeeded = true,
                Issuer = jwt.Issuer,
                Subject = jwt.Subject,
                Principal = principal
            };
        }
        catch (SecurityTokenException e)
        {
            // Debug: a rejected assertion is the caller's problem, and the reply says so without
            // naming which check failed.
            _logger.LogDebug(e, "Assertion from issuer {Issuer} was rejected", issuer);
            return FederatedAssertionResult.Fail("The presented token was rejected.");
        }
    }
}
