using SnapCd.Server.Core.Factories;

namespace SnapCd.Server.Core.Services.Dashboard;

public sealed class PostAuthRedirectResolver(
    IdentityRedirectManager redirectManager,
    MemberServiceFactory memberServiceFactory)
{
    public async Task RedirectAfterAuthAsync(Guid userId, string? returnUrl)
    {
        if (IsSafeLocalUrl(returnUrl))
        {
            redirectManager.RedirectTo(returnUrl!);
        }

        using var scope = memberServiceFactory.Create();
        var pendingToken = await scope.Service.GetPendingInvitationTokenForUserAsync(userId);
        if (pendingToken is not null)
        {
            redirectManager.RedirectTo(
                "Account/CompleteOrganizationInvitation",
                new Dictionary<string, object?>
                {
                    ["token"] = pendingToken,
                    ["returnUrl"] = "/Home"
                });
        }

        redirectManager.RedirectTo("/Home");
    }

    private static bool IsSafeLocalUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && url.StartsWith('/')
        && !url.StartsWith("//", StringComparison.Ordinal)
        && !url.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase);
}
