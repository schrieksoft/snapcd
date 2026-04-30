using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Constants;

namespace SnapCd.Server.Core.Misc.Utils.Helpers;

public static class ClaimsIdentityExtensions
{
    public static async Task<ClaimsIdentity> SetDefaultClaims(
        this ClaimsIdentity identity,
        UserManager<User> userManager,
        User user,
        string? clientId,
        SnapCdDbContext dbContext)
    {
        identity
            .SetClaim(OpenIddictConstants.Claims.Subject, await userManager.GetUserIdAsync(user))
            .SetClaim(OpenIddictConstants.Claims.Email, await userManager.GetEmailAsync(user))
            .SetClaim(OpenIddictConstants.Claims.Name, await userManager.GetUserNameAsync(user))
            .SetClaim(OpenIddictConstants.Claims.PreferredUsername, await userManager.GetUserNameAsync(user))
            .SetClaim(OpenIddictConstants.Claims.ClientId, clientId)
            .SetClaim("principal_discriminator", "User");

        // Add organization claims for User
        var organizationIds = await dbContext.OrganizationUsers
            .Where(ou => ou.UserId == user.Id && !ou.IsDeactivated)
            .Select(ou => ou.OrganizationId.ToString())
            .ToListAsync();

        if (organizationIds.Any()) identity.SetClaim(ClaimTypeConstants.OrganizationClaimType, string.Join(",", organizationIds));

        //identity.SetClaims(OpenIddictConstants.Claims.Role, (await userManager.GetRolesAsync(user)).ToImmutableArray());

        var claims = await userManager.GetClaimsAsync(user);
        var firstNameClaim = claims.FirstOrDefault(c => c.Type == "first_name");
        var lastNameClaim = claims.FirstOrDefault(c => c.Type == "last_name");
        if (firstNameClaim != null && lastNameClaim != null)
        {
            identity.SetClaim("first_name", firstNameClaim.Value);
            identity.SetClaim("last_name", lastNameClaim.Value);
        }

        var phoneNumberClaim = claims.FirstOrDefault(c => c.Type == "phone_number");
        if (phoneNumberClaim != null) identity.SetClaim("phone_number", phoneNumberClaim.Value);

        return identity;
    }

    public static async Task<ClaimsIdentity> SetDefaultClientCredentialGrantClaims(
        this ClaimsIdentity identity,
        IOpenIddictApplicationManager applicationManager,
        object application,
        SnapCdDbContext dbContext)
    {
        var clientId = await applicationManager.GetClientIdAsync(application);

        // Find the ServicePrincipal by ClientId to get the actual ServicePrincipal.Id and OrganizationId
        var servicePrincipal = await dbContext.ServicePrincipals
            .FirstOrDefaultAsync(sp => sp.ClientId == clientId);

        if (servicePrincipal == null)
            throw new InvalidOperationException($"ServicePrincipal with ClientId {clientId} not found");

        identity.SetClaim(OpenIddictConstants.Claims.Subject, servicePrincipal.Id.ToString());
        identity.SetClaim(OpenIddictConstants.Claims.Name, await applicationManager.GetDisplayNameAsync(application));
        identity.SetClaim(OpenIddictConstants.Claims.PreferredUsername, clientId);
        identity.SetClaim("principal_discriminator", "ServicePrincipal");

        // Add organization claim for ServicePrincipal (single organization)
        identity.SetClaim(ClaimTypeConstants.OrganizationClaimType, servicePrincipal.OrganizationId.ToString());

        return identity;
    }
}