using SnapCd.Server.Core.Services.Edition;

namespace SnapCd.Server.Host.Services;

public class SelfHostedInvitationAutoAcceptPolicy : IInvitationAutoAcceptPolicy
{
    public bool AutoAcceptOnRegistration => true;
}
