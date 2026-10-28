// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
        "/Account/AcceptInvitation",
        "/Account/CompleteInvitation",
        "/Account/CompleteInvitationExternalLogin",
        "/Account/CompleteOrganizationInvitation",
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

}