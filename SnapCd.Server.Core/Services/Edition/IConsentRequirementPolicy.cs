namespace SnapCd.Server.Core.Services.Edition;

/// <summary>
/// Per-edition switch for the "I agree to the Terms of Service and Privacy Policy"
/// consent gate on registration / invitation flows. SaaS expects the user to tick
/// the checkbox; CE / SelfHosted does not surface Terms or Privacy at all and
/// returns false so the gate is hidden.
/// </summary>
public interface IConsentRequirementPolicy
{
    bool RequiresTermsAcceptance { get; }
}
