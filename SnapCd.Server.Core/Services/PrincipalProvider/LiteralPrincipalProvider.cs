using SnapCd.Contracts;
using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.Services.PrincipalProvider;

public class LiteralPrincipalProvider : IPrincipalProvider
{
    private readonly Guid _principalId;
    private readonly PrincipalDiscriminator _principalDiscriminator;
    private readonly List<Guid> _organizations;


    public LiteralPrincipalProvider(Guid principalId, PrincipalDiscriminator principalDiscriminator, List<Guid> organizations)
    {
        _principalId = principalId;
        _principalDiscriminator = principalDiscriminator;
        _organizations = organizations;
    }


    public Guid GetSystemSubject()
    {
        return _principalId;
    }


    public Guid GetSubject(Guid organizationId)
    {
        if (_organizations.Any() && !_organizations.Contains(organizationId))
            throw new PrincipalNotAuthorizedException($"Principal not member of organization {organizationId}");

        return _principalId;
    }

    public PrincipalDiscriminator GetPrincipalDiscriminator()
    {
        return _principalDiscriminator;
    }

    public List<Guid> GetOrganizations()
    {
        return _organizations;
    }

    public Guid GetUserId()
    {
        // This method should only be called for Users, not ServicePrincipals
        if (_principalDiscriminator != PrincipalDiscriminator.User)
            throw new PrincipalNotAuthorizedException("GetUserId() can only be called for User principals, not ServicePrincipals");

        return _principalId;
    }

    public Guid GetSubjectOrDefault(Guid organizationId)
    {
        // For LiteralPrincipalProvider, we always have a principal, so this behaves the same as GetSubject
        if (_organizations.Any() && !_organizations.Contains(organizationId))
            return Guid.Empty;

        return _principalId;
    }

    public Guid GetSystemSubjectOrDefault()
    {
        // For LiteralPrincipalProvider, we always have a principal
        return _principalId;
    }

    public PrincipalDiscriminator? GetPrincipalDiscriminatorOrDefault()
    {
        // For LiteralPrincipalProvider, we always have a principal, so this returns the actual discriminator
        return _principalDiscriminator;
    }
}