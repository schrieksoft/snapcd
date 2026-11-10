// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Nonsecured.Federation;

/// <summary>
/// Decides whether an assertion authenticates a Service Principal through one of its Federated
/// Credentials. Shared by the token endpoint and the state backend.
/// </summary>
public class FederatedCredentialAuthenticator
{
    private readonly SnapCdDbContext _dbContext;
    private readonly FederatedAssertionValidator _assertionValidator;

    public FederatedCredentialAuthenticator(SnapCdDbContext dbContext, FederatedAssertionValidator assertionValidator)
    {
        _dbContext = dbContext;
        _assertionValidator = assertionValidator;
    }

    /// <summary>Whether a presented credential is a token rather than a client secret.</summary>
    public bool IsAssertion(string credential)
        => !string.IsNullOrEmpty(_assertionValidator.PeekIssuerAndSubject(credential).Issuer);

    public async Task<bool> AuthenticateAsync(
        ServicePrincipal servicePrincipal,
        string assertion,
        CancellationToken cancellationToken = default)
    {
        if (servicePrincipal.IsDisabled)
            return false;

        // Read before validating, to find which trust record this assertion claims. Nothing is
        // granted until the signature has been checked against that record's issuer.
        var (claimedIssuer, claimedSubject) = _assertionValidator.PeekIssuerAndSubject(assertion);
        if (string.IsNullOrEmpty(claimedIssuer) || string.IsNullOrEmpty(claimedSubject))
            return false;

        var credential = await _dbContext.ServicePrincipalFederatedCredentials
            .FirstOrDefaultAsync(c =>
                c.ServicePrincipalId == servicePrincipal.Id &&
                c.OrganizationId == servicePrincipal.OrganizationId &&
                c.Issuer == claimedIssuer &&
                c.Subject == claimedSubject &&
                !c.IsDisabled, cancellationToken);

        if (credential is null)
            return false;

        var result = await _assertionValidator.ValidateAsync(
            assertion, credential.Issuer, credential.Audience, cancellationToken);

        // The subject is compared again: the record was selected from unvalidated claims.
        return result.Succeeded && string.Equals(result.Subject, credential.Subject, StringComparison.Ordinal);
    }
}
