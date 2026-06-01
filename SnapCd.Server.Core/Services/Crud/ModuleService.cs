// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Modules;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleService : GenericCrudService<Module, ModuleCreateDto, ModuleUpdateDto, ModuleReadDto, ModuleSecuredRepository, ModuleRepository, ModuleCreatedEvent, ModuleUpdatedEvent, ModuleDeletedEvent, ModuleRepositorySettings>
{
    public ModuleService(
        ModuleSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override Module MapToEntity(ModuleCreateDto dto, Guid organizationId)
    {
        return ModuleMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleReadDto MapToDto(Module entity)
    {
        return ModuleMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(Module entity, ModuleUpdateDto dto)
    {
        ModuleMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var module = await GetByCriteria(repo => repo.Get(namespaceId, name, organizationId));
        return module;
    }

    public async Task<ModuleReadDto> GetByName(string stackName, string namespaceName, string moduleName, Guid organizationId)
    {
        var module = await GetByCriteria(repo => repo.Get(stackName, namespaceName, moduleName, organizationId));
        return module;
    }
}