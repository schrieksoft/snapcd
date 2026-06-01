// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Secured;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleInputFromNamespaceBaseService
{
    private readonly ModuleInputSecuredRepository _repo;

    public ModuleInputFromNamespaceBaseService(
        ModuleInputSecuredRepository repo)
    {
        _repo = repo;
    }

    public async Task<ModuleInputFromNamespaceReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await _repo.Get(id, organizationId);
        var dto = entity switch
        {
            ModuleParamFromNamespace param => ModuleInputFromNamespaceMapper.ToDto(param),
            ModuleEnvVarFromNamespace envVar => ModuleInputFromNamespaceMapper.ToDto(envVar),
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        dto.InputKind = entity switch
        {
            ModuleParamFromNamespace => InputKind.Param,
            ModuleEnvVarFromNamespace => InputKind.EnvVar,
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        return dto;
    }

    public async Task<ModuleInputFromNamespaceReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await _repo.Get(moduleId, name, organizationId);
        var dto = entity switch
        {
            ModuleParamFromNamespace param => ModuleInputFromNamespaceMapper.ToDto(param),
            ModuleEnvVarFromNamespace envVar => ModuleInputFromNamespaceMapper.ToDto(envVar),
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        dto.InputKind = entity switch
        {
            ModuleParamFromNamespace => InputKind.Param,
            ModuleEnvVarFromNamespace => InputKind.EnvVar,
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        return dto;
    }

    public async Task Delete(Guid id, Guid organizationId)
    {
        await _repo.Delete(id, organizationId);
    }
}