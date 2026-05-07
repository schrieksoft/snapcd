namespace SnapCd.Server.Core.Filters;

/// <summary>
/// Surface-category marker: the controller or page is IAM / org-administration
/// scaffolding (Users / ServicePrincipals / Groups / GroupMembers / org role
/// assignments). Each consumer interprets the marker for itself; the SaaS
/// subscription filter, for example, treats it as an override of any inherited
/// <see cref="OrganizationScopedFeatureAttribute"/>, keeping IAM admin reachable
/// even when the org's billing has lapsed. Other policies (audit, MFA, etc.) may
/// use the same marker differently.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class OrganizationScopedIAMAttribute : Attribute;
