using SnapCd.Contracts;

namespace SnapCd.Server.Core.Services.PrincipalProvider;

public interface IPrincipalProvider
{
    public Guid GetSubject(Guid organizationId);
    public Guid GetSystemSubject();
    public PrincipalDiscriminator GetPrincipalDiscriminator();
    public List<Guid> GetOrganizations();
    public Guid GetUserId();

    /// <summary>
    /// Gets the subject (principal ID) or returns Guid.Empty if no principal is available.
    /// Use this in contexts where a principal may not be available (e.g., saga activities, background jobs).
    /// </summary>
    public Guid GetSubjectOrDefault(Guid organizationId);

    /// <summary>
    /// Gets the system subject (principal ID) or returns Guid.Empty if no principal is available.
    /// Use this in system contexts where a principal may not be available (e.g., saga activities, background jobs).
    /// </summary>
    public Guid GetSystemSubjectOrDefault();

    /// <summary>
    /// Gets the principal discriminator or returns null if no principal is available.
    /// Use this in contexts where a principal may not be available (e.g., saga activities, background jobs).
    /// Returns null to indicate system/automated operations.
    /// </summary>
    public PrincipalDiscriminator? GetPrincipalDiscriminatorOrDefault();
}