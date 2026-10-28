// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.OrganizationContext;

public class OrganizationContext : IOrganizationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _cachedOrganizationId;

    public const string CookieName = "CurrentOrganizationId";

    public OrganizationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CurrentOrganizationId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var cookieValue = httpContext.Request.Cookies[CookieName];
                if (Guid.TryParse(cookieValue, out var orgId))
                {
                    _cachedOrganizationId = orgId;
                    return orgId;
                }
                return null;
            }

            // Fallback to cached value when HttpContext is unavailable (Blazor Server SignalR)
            return _cachedOrganizationId;
        }
    }

    public static void SetOrganizationCookie(HttpResponse response, Guid organizationId)
    {
        response.Cookies.Append(CookieName, organizationId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            // Lax, matching the Identity cookie. An external login returns through a redirect
            // chain that began on the provider's site; the browser treats every hop in that
            // chain as cross-site, so a Strict cookie set mid-chain is not sent on the request
            // that follows it. The organization gate then sees no cookie and redirects back.
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });
    }

    public static void ClearOrganizationCookie(HttpResponse response)
    {
        response.Cookies.Delete(CookieName);
    }
}
