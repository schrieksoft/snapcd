using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace SnapCd.Server.Core.Services.Dashboard;

public static class RenderModeHelper
{
    private enum RenderModeEnum
    {
        StaticServerSideRendering,
        InteractiveServerSideRendering,
        InteractiveServerSideRendering_Without_Prerendering
    }

    private static IComponentRenderMode? GetRenderMode(RenderModeEnum renderMode) => renderMode switch
    {
        RenderModeEnum.StaticServerSideRendering => null,
        RenderModeEnum.InteractiveServerSideRendering => RenderMode.InteractiveServer,
        RenderModeEnum.InteractiveServerSideRendering_Without_Prerendering => new InteractiveServerRenderMode(false),
        _ => null
    };
    
    public static IComponentRenderMode? GetRenderMode(HttpContext? httpContext, NavigationManager navigationManager)
    {
        if (IsStaticServersideRendering(httpContext, navigationManager))
            return GetRenderMode(RenderModeEnum.StaticServerSideRendering);

        if (IsMarketingPage(httpContext, navigationManager))
            return GetRenderMode(RenderModeEnum.StaticServerSideRendering);

        return GetRenderMode(RenderModeEnum.InteractiveServerSideRendering_Without_Prerendering);
    }

    private static readonly string[] IsStaticSsr =
    {
        "/contact",
        "/Account/ExternalLogin",
        "/Account/Login",
        "/Account/LoginWith2fa",
        "/Account/LoginWithRecoveryCode",
        "/Account/Register",
        "/Account/AccessDenied",
        "/Account/ConfirmEmail",
        "/Account/ConfirmEmailChange",
        "/Account/ForgotPassword",
        "/Account/ForgotPasswordConfirmation",
        "/Account/InvalidPasswordReset",
        "/Account/InvalidUser",
        "/Account/Lockout",
        "/Account/RegisterConfirmation",
        "/Account/ResendEmailConfirmation",
        "/Account/ResetPassword",
        "/Account/ResetPasswordConfirmation",
        "/Account/CompleteInvitation",
        "/Account/AssociateExternalLogin",
        "/Account/RegisterExternalLogin",
        "/Account/RemoveExternalLogin",
        "/Account/RemovePassword",
        "/Account/Disable2fa",
        "/Account/Enable2fa",
        "/Account/EnableAuthenticator",
        "/Account/RegisterAuthenticator",
        "/Account/GenerateRecoveryCodes",
        "/Account/ResetAuthenticator",
        "/Account/ConfirmEmailNotCompleted",
        "/Account/SelectOrganization"
    };

    private static readonly string[] MarketingPages =
    {
        "/",
        "/docs",
        "/pricing",
        "/terms",
        "/privacy",
        "/impressum",
        "/Error"
    };

    public static bool IsStaticServersideRendering(HttpContext? httpContext, NavigationManager navigationManager)
    {
        if (httpContext == null)
        {
            var relativePath = navigationManager.ToBaseRelativePath(navigationManager.Uri);
            var fullPath = "/" + relativePath;

            if (IsStaticSsr.Any(x => fullPath.StartsWith(x, StringComparison.OrdinalIgnoreCase))) return true;

            return false;
        }
        else
        {
            if (IsStaticSsr.Any(x => httpContext.Request.Path.StartsWithSegments(x, StringComparison.OrdinalIgnoreCase))) return true;

            return false;
        }
    }

    public static bool IsMarketingPage(HttpContext? httpContext, NavigationManager navigationManager)
    {
        if (httpContext == null)
        {
            var relativePath = navigationManager.ToBaseRelativePath(navigationManager.Uri);
            var fullPath = "/" + relativePath;

            // Exact match for root, or starts with for others
            if (fullPath == "/" || MarketingPages.Any(x => x != "/" && fullPath.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }
        else
        {
            var path = httpContext.Request.Path.Value ?? "";

            // Exact match for root
            if (path == "/" || path == "") return true;

            // Check other marketing pages
            if (MarketingPages.Any(x => x != "/" && httpContext.Request.Path.StartsWithSegments(x, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }
    }
}