namespace SnapCd.Server.Core.Services.Edition;

/// <summary>
/// Whether anonymous visitors may create their own account via the public registration form.
/// Self-Hosted: false (admin-invited members only). SaaS: true (open sign-up).
/// </summary>
public interface ISelfRegistrationPolicy
{
    bool AllowsSelfRegistration { get; }
}
