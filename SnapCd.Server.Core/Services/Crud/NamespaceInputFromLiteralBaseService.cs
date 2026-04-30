using SnapCd.Contracts;
using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Secured;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceInputFromLiteralBaseService
{
    private readonly NamespaceInputSecuredRepository _repo;

    public NamespaceInputFromLiteralBaseService(
        NamespaceInputSecuredRepository repo)
    {
        _repo = repo;
    }

    public async Task<NamespaceInputFromLiteralReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await _repo.Get(id, organizationId);
        var dto = entity switch
        {
            NamespaceParamFromLiteral param => NamespaceInputFromLiteralMapper.ToDto(param),
            NamespaceEnvVarFromLiteral envVar => NamespaceInputFromLiteralMapper.ToDto(envVar),
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        dto.InputKind = entity switch
        {
            NamespaceParamFromLiteral => InputKind.Param,
            NamespaceEnvVarFromLiteral => InputKind.EnvVar,
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        return dto;
    }

    public async Task<NamespaceInputFromLiteralReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await _repo.Get(namespaceId, name, organizationId);
        var dto = entity switch
        {
            NamespaceParamFromLiteral param => NamespaceInputFromLiteralMapper.ToDto(param),
            NamespaceEnvVarFromLiteral envVar => NamespaceInputFromLiteralMapper.ToDto(envVar),
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        dto.InputKind = entity switch
        {
            NamespaceParamFromLiteral => InputKind.Param,
            NamespaceEnvVarFromLiteral => InputKind.EnvVar,
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        return dto;
    }

    public async Task Delete(Guid id, Guid organizationId)
    {
        await _repo.Delete(id, organizationId);
    }
}