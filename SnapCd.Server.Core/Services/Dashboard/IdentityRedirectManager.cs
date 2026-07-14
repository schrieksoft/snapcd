// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace SnapCd.Server.Core.Services.Dashboard;

public class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5)
    };

    [DoesNotReturn]
    public void RedirectTo(string? uri)
    {
        uri ??= "";

        // Prevent open redirects.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative)) uri = navigationManager.ToBaseRelativePath(uri);

        // During static rendering, NavigateTo throws a NavigationException which is handled by the framework as a redirect.
        // So as long as this is called from a statically rendered Identity component, the InvalidOperationException is never thrown.
        navigationManager.NavigateTo(uri);
        throw new InvalidOperationException($"{nameof(IdentityRedirectManager)} can only be used during static rendering.");
    }

    [DoesNotReturn]
    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    [DoesNotReturn]
    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    /// <summary>
    /// The page currently being rendered, <em>including</em> its query string.
    /// </summary>
    /// <remarks>
    /// The query string must be preserved: a page like /Account/CompleteInvitation
    /// carries its invitation token in the query, and redirecting back to it on a
    /// failed submit (a rejected password, say) has to return the user to a URL that
    /// still has the token. Stripping it left the retry with no token, so a second
    /// attempt failed with "No invitation token provided." even when the input was
    /// valid.
    /// </remarks>
    private string CurrentPageUri => navigationManager.Uri;

    [DoesNotReturn]
    public void RedirectToCurrentPage()
    {
        RedirectTo(CurrentPageUri);
    }

    [DoesNotReturn]
    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
    {
        RedirectToWithStatus(CurrentPageUri, message, context);
    }
}