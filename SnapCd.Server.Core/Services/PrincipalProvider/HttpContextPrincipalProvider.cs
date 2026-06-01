// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.Services.PrincipalProvider;

public class HttpContextPrincipalProvider : IPrincipalProvider
{
    private readonly HttpContext? _httpContext;
    private readonly ClaimsIdentity? _claimsIdentity;

    public HttpContextPrincipalProvider(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor.HttpContext != null) _httpContext = httpContextAccessor.HttpContext;
        if (_httpContext != null) _claimsIdentity = _httpContext.User.Identity as ClaimsIdentity;
    }

    public Guid GetSystemSubject()
    {
        if (_claimsIdentity != null)
        {
            var stringClaim = _claimsIdentity.Claims.SingleOrDefault(c => c.Type == ClaimTypeConstants.SubjectClaimType)
                ?.Value;

            if (stringClaim != null)
            {
                var principalId = new Guid(stringClaim);
                return principalId;
            }
        }

        throw new PrincipalNotAuthorizedException("No principal found");
    }


    public Guid GetSubject(Guid organizationId)
    {
        if (_claimsIdentity != null)
        {
            var stringClaim = _claimsIdentity.Claims.SingleOrDefault(c => c.Type == ClaimTypeConstants.SubjectClaimType)
                ?.Value;

            if (stringClaim != null)
            {
                var principalId = new Guid(stringClaim);

                // Fast path: check token claim
                var organizations = GetOrganizations();
                if (organizations.Contains(organizationId))
                    return principalId;

                // Fallback: check database for current membership (handles post-token organization assignment)
                if (CheckOrganizationMembershipInDatabase(principalId, organizationId))
                    return principalId;

                throw new PrincipalNotAuthorizedException($"Principal not member of organization {organizationId}");
            }
        }

        throw new PrincipalNotAuthorizedException("No principal found");
    }

    private bool CheckOrganizationMembershipInDatabase(Guid principalId, Guid organizationId)
    {
        if (_httpContext == null)
            return false;

        var discriminator = GetPrincipalDiscriminator();

        // Get DbContext from request services
        var dbContextFactory = _httpContext.RequestServices.GetService<IDbContextFactory<SnapCdDbContext>>();
        if (dbContextFactory == null)
            return false;

        using var dbContext = dbContextFactory.CreateDbContext();

        if (discriminator == PrincipalDiscriminator.User)
        {
            // Check OrganizationUser table for user membership
            return dbContext.Set<OrganizationUser>()
                .Any(ou => ou.UserId == principalId
                        && ou.OrganizationId == organizationId
                        && !ou.IsDeactivated
                        && ou.InvitationCompleted);
        }
        else if (discriminator == PrincipalDiscriminator.ServicePrincipal)
        {
            // Service principals are tied to a single organization
            return dbContext.ServicePrincipals
                .Any(sp => sp.Id == principalId && sp.OrganizationId == organizationId);
        }

        return false;
    }

    public List<Guid> GetOrganizations()
    {
        if (_claimsIdentity != null)
        {
            var organizationsClaim = _claimsIdentity.Claims.SingleOrDefault(c => c.Type == ClaimTypeConstants.OrganizationClaimType)
                ?.Value;

            if (organizationsClaim != null)
                return organizationsClaim.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(org => new Guid(org.Trim()))
                    .ToList();
        }

        return new List<Guid>();
    }

    public PrincipalDiscriminator GetPrincipalDiscriminator()
    {
        if (_claimsIdentity != null)
        {
            var discriminatorString = _claimsIdentity.Claims
                .SingleOrDefault(c => c.Type == ClaimTypeConstants.PrincipalDiscriminatorClaimType)?.Value;
            switch (discriminatorString)
            {
                case "ServicePrincipal":
                    return PrincipalDiscriminator.ServicePrincipal;
                case "User":
                    return PrincipalDiscriminator.User;
                case null:
                    return PrincipalDiscriminator.User;
                default:
                    throw new NotImplementedException(
                        $"Not implemented for PrincipalDiscriminator of type \"{discriminatorString}\".");
            }
        }

        return PrincipalDiscriminator.User;
    }

    public Guid GetUserId()
    {
        // This method should only be called for Users, not ServicePrincipals
        var discriminator = GetPrincipalDiscriminator();
        if (discriminator != PrincipalDiscriminator.User)
            throw new PrincipalNotAuthorizedException("GetUserId() can only be called for User principals, not ServicePrincipals");

        if (_claimsIdentity != null)
        {
            var stringClaim = _claimsIdentity.Claims.SingleOrDefault(c => c.Type == ClaimTypeConstants.SubjectClaimType)
                ?.Value;

            if (stringClaim != null) return new Guid(stringClaim);
        }

        throw new PrincipalNotAuthorizedException("No user principal found");
    }

    public Guid GetSubjectOrDefault(Guid organizationId)
    {
        if (_claimsIdentity != null)
        {
            var stringClaim = _claimsIdentity.Claims.SingleOrDefault(c => c.Type == ClaimTypeConstants.SubjectClaimType)
                ?.Value;

            if (stringClaim != null)
            {
                var principalId = new Guid(stringClaim);

                // Fast path: check token claim
                var organizations = GetOrganizations();
                if (organizations.Contains(organizationId))
                    return principalId;

                // Fallback: check database for current membership (handles post-token organization assignment)
                if (CheckOrganizationMembershipInDatabase(principalId, organizationId))
                    return principalId;
            }
        }

        return Guid.Empty;
    }

    public Guid GetSystemSubjectOrDefault()
    {
        if (_claimsIdentity != null)
        {
            var stringClaim = _claimsIdentity.Claims.SingleOrDefault(c => c.Type == ClaimTypeConstants.SubjectClaimType)
                ?.Value;

            if (stringClaim != null) return new Guid(stringClaim);
        }

        return Guid.Empty;
    }

    public PrincipalDiscriminator? GetPrincipalDiscriminatorOrDefault()
    {
        if (_claimsIdentity != null)
        {
            var discriminatorString = _claimsIdentity.Claims
                .SingleOrDefault(c => c.Type == ClaimTypeConstants.PrincipalDiscriminatorClaimType)?.Value;
            switch (discriminatorString)
            {
                case "ServicePrincipal":
                    return PrincipalDiscriminator.ServicePrincipal;
                case "User":
                    return PrincipalDiscriminator.User;
            }
        }

        return null;
    }
}