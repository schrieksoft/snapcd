using SnapCd.Server.Core.Services.Edition;

namespace SnapCd.Server.Host.Services;

public class SelfHostedSelfRegistrationPolicy : ISelfRegistrationPolicy
{
    public bool AllowsSelfRegistration => false;
}
