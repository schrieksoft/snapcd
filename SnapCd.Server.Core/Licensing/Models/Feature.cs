namespace SnapCd.Server.Core.Licensing.Models;

/// <summary>
/// Feature flags consumed by <see cref="Attributes.VerifyLicense"/> and the UI.
/// Each feature is mapped to the set of tiers that include it via
/// <see cref="TierFeatures"/>. Adding a new gated capability means adding a
/// value here and a row in the matrix — nothing else.
/// </summary>
public enum Feature
{
    FinegrainedRbac,
    Sso,
    ApprovalWorkflows,
    Turnstile,
    PremiumMessageBroker,
    PremiumSecretStore,
    PremiumEmailProvider,
}

/// <summary>
/// Single source of truth for which tier unlocks which feature.
/// </summary>
public static class TierFeatures
{
    private static readonly Dictionary<(Tier, Feature), bool> Matrix = new()
    {
        // Community tier — baseline only, no gated features.
        { (Tier.Community, Feature.FinegrainedRbac), false },
        { (Tier.Community, Feature.Sso), false },
        { (Tier.Community, Feature.ApprovalWorkflows), false },
        { (Tier.Community, Feature.Turnstile), false },
        { (Tier.Community, Feature.PremiumMessageBroker), false },
        { (Tier.Community, Feature.PremiumSecretStore), false },
        { (Tier.Community, Feature.PremiumEmailProvider), false },

        // Lite tier — full feature set today; may diverge later.
        { (Tier.Lite, Feature.FinegrainedRbac), true },
        { (Tier.Lite, Feature.Sso), true },
        { (Tier.Lite, Feature.ApprovalWorkflows), true },
        { (Tier.Lite, Feature.Turnstile), true },
        { (Tier.Lite, Feature.PremiumMessageBroker), true },
        { (Tier.Lite, Feature.PremiumSecretStore), true },
        { (Tier.Lite, Feature.PremiumEmailProvider), true },

        // Enterprise tier — full feature set; future tier-exclusive features land here.
        { (Tier.Enterprise, Feature.FinegrainedRbac), true },
        { (Tier.Enterprise, Feature.Sso), true },
        { (Tier.Enterprise, Feature.ApprovalWorkflows), true },
        { (Tier.Enterprise, Feature.Turnstile), true },
        { (Tier.Enterprise, Feature.PremiumMessageBroker), true },
        { (Tier.Enterprise, Feature.PremiumSecretStore), true },
        { (Tier.Enterprise, Feature.PremiumEmailProvider), true },
    };

    public static bool Includes(this Tier tier, Feature feature) =>
        Matrix.TryGetValue((tier, feature), out var enabled) && enabled;

    public static bool Includes(this LicenseInfo? info, Feature feature) =>
        info is { IsValid: true } && info.Tier.Includes(feature);
}
