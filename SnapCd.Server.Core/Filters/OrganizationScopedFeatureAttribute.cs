namespace SnapCd.Server.Core.Filters;

/// <summary>
/// Surface-category marker: the controller or page is a billable product feature
/// (Modules / Namespaces / Stacks / Runners / Jobs / Logs / source-change webhook /
/// etc.). Each consumer interprets the marker for itself; the SaaS subscription
/// filter, for example, gates these endpoints behind an active cloud subscription
/// while the CE host registers no consumer at all.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class OrganizationScopedFeatureAttribute : Attribute;
