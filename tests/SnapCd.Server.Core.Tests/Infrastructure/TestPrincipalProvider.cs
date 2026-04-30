using SnapCd.Contracts;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Tests.Infrastructure;

public class TestPrincipalProvider : IPrincipalProvider
{
    private readonly Guid _principalId;
    private readonly PrincipalDiscriminator _discriminator;
    private readonly Guid _organizationId;

    public TestPrincipalProvider(Guid principalId, PrincipalDiscriminator discriminator, Guid organizationId)
    {
        _principalId = principalId;
        _discriminator = discriminator;
        _organizationId = organizationId;
    }

    public Guid GetSystemSubject()
    {
        return _principalId;
    }

    public Guid GetSubject(Guid organizationId)
    {
        return _principalId;
    }

    public PrincipalDiscriminator GetPrincipalDiscriminator()
    {
        return _discriminator;
    }

    public List<Guid> GetOrganizations()
    {
        return new List<Guid> { _organizationId };
    }

    public Guid GetUserId()
    {
        // For service principals, this would throw or return Guid.Empty
        // For users, this returns the user ID
        if (_discriminator == PrincipalDiscriminator.User) return _principalId;
        return Guid.Empty;
    }

    public Guid GetSubjectOrDefault(Guid organizationId)
    {
        return _principalId;
    }

    public Guid GetSystemSubjectOrDefault()
    {
        return _principalId;
    }

    public PrincipalDiscriminator? GetPrincipalDiscriminatorOrDefault()
    {
        return _discriminator;
    }
}