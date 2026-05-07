namespace SnapCd.Server.Core.Services.Edition;

/// <summary>
/// Whether completing the invitation registration form should automatically accept the
/// org invitation in the same step. Self-Hosted: true (single-org deployment, the accept
/// step would be ceremonial). SaaS: false (users may belong to multiple orgs; explicit
/// accept-or-decline is meaningful).
/// </summary>
public interface IInvitationAutoAcceptPolicy
{
    bool AutoAcceptOnRegistration { get; }
}
