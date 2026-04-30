using SnapCd.Contracts;
using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Secured;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceInputFromSecretBaseService
{
    private readonly NamespaceInputSecuredRepository _repo;

    public NamespaceInputFromSecretBaseService(
        NamespaceInputSecuredRepository repo)
    {
        _repo = repo;
    }

    public async Task<NamespaceInputFromSecretReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await _repo.Get(id, organizationId);
        var dto = entity switch
        {
            NamespaceParamFromSecret param => NamespaceInputFromSecretMapper.ToDto(param),
            NamespaceEnvVarFromSecret envVar => NamespaceInputFromSecretMapper.ToDto(envVar),
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        dto.InputKind = entity switch
        {
            NamespaceParamFromSecret => InputKind.Param,
            NamespaceEnvVarFromSecret => InputKind.EnvVar,
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        return dto;
    }

    public async Task<NamespaceInputFromSecretReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await _repo.Get(namespaceId, name, organizationId);
        var dto = entity switch
        {
            NamespaceParamFromSecret param => NamespaceInputFromSecretMapper.ToDto(param),
            NamespaceEnvVarFromSecret envVar => NamespaceInputFromSecretMapper.ToDto(envVar),
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        dto.InputKind = entity switch
        {
            NamespaceParamFromSecret => InputKind.Param,
            NamespaceEnvVarFromSecret => InputKind.EnvVar,
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        return dto;
    }

    public async Task Delete(Guid id, Guid organizationId)
    {
        await _repo.Delete(id, organizationId);
    }
}