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
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });
    }

    public static void ClearOrganizationCookie(HttpResponse response)
    {
        response.Cookies.Delete(CookieName);
    }
}
