using SnapCd.Contracts;

namespace SnapCd.Server.Core.UI;

/// <summary>
/// Marks a Razor page so that <see cref="Dashboard.Layout.OrganizationMainLayout"/> and
/// <see cref="Dashboard.Layout.NavMenu"/> evaluate role-based access for both rendering
/// the page and showing its navigation entry. With no roles, the marker only opts the
/// page into navigation visibility (everyone authenticated may navigate to it). With
/// one or more roles, the page is restricted to users holding any of them in the
/// current organization (or a System Administrator).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AuthorizeOnNavigationPermission : Attribute
{
    public OrganizationRole[] AnyOf { get; }

    public AuthorizeOnNavigationPermission(params OrganizationRole[] anyOf)
    {
        AnyOf = anyOf;
    }
}
