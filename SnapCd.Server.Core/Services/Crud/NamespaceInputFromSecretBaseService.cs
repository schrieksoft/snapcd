// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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