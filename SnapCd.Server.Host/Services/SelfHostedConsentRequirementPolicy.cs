using SnapCd.Server.Core.Services.Edition;

namespace SnapCd.Server.Host.Services;

/// <summary>
/// CE / SelfHosted has no Terms or Privacy pages — the consent checkbox is
/// hidden on registration/invitation forms.
/// </summary>
public class SelfHostedConsentRequirementPolicy : IConsentRequirementPolicy
{
    public bool RequiresTermsAcceptance => false;
}
