// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Constants;

namespace SnapCd.Server.Core.Services.IdentityAccess;

public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User>
{
    private readonly SnapCdDbContext _dbContext;

    public CustomUserClaimsPrincipalFactory(
        UserManager<User> userManager,
        IOptions<IdentityOptions> optionsAccessor,
        SnapCdDbContext dbContext)
        : base(userManager, optionsAccessor)
    {
        _dbContext = dbContext;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        // Get the default claims (Subject, Name, Email, etc.)
        var identity = await base.GenerateClaimsAsync(user);

        // Add principal discriminator claim for identity cookies
        identity.AddClaim(new Claim(ClaimTypeConstants.PrincipalDiscriminatorClaimType, "User"));

        // Add organization claims
        var organizationIds = await _dbContext.OrganizationUsers
            .Where(ou => ou.UserId == user.Id && !ou.IsDeactivated && ou.InvitationCompleted)
            .Select(ou => ou.OrganizationId.ToString())
            .ToListAsync();

        if (organizationIds.Any()) identity.AddClaim(new Claim(ClaimTypeConstants.OrganizationClaimType, string.Join(",", organizationIds)));

        return identity;
    }
}