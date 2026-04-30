using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Dashboard;

public class IdentityUserAccessor(UserManager<User> userManager, IdentityRedirectManager redirectManager)
{
    public async Task<User> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null) redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);

        return user;
    }

    public async Task<User?> GetUserAsync(AuthenticationState authState)
    {
        if (authState.User.Identity?.IsAuthenticated == true) return await userManager.GetUserAsync(authState.User);
        return null;
    }

    public async Task<User> GetRequiredUserAsync(AuthenticationState authState)
    {
        var user = await GetUserAsync(authState);

        if (user is null) throw new InvalidOperationException($"Unable to load user with ID '{userManager.GetUserId(authState.User)}'.");

        return user;
    }
}