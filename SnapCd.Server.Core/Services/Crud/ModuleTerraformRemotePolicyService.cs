// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleTerraformRemotePolicies;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleTerraformRemotePolicyService : GenericCrudService<ModuleTerraformRemotePolicy, ModuleTerraformRemotePolicyCreateDto, ModuleTerraformRemotePolicyUpdateDto, ModuleTerraformRemotePolicyReadDto,
    ModuleTerraformRemotePolicySecuredRepository, ModuleTerraformRemotePolicyRepository, ModuleTerraformRemotePolicyCreatedEvent,
    ModuleTerraformRemotePolicyUpdatedEvent, ModuleTerraformRemotePolicyDeletedEvent, ModuleTerraformRemotePolicyRepositorySettings>
{
    public ModuleTerraformRemotePolicyService(
        ModuleTerraformRemotePolicySecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleTerraformRemotePolicy MapToEntity(ModuleTerraformRemotePolicyCreateDto dto, Guid organizationId)
    {
        return ModuleTerraformRemotePolicyMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleTerraformRemotePolicyReadDto MapToDto(ModuleTerraformRemotePolicy entity)
    {
        return ModuleTerraformRemotePolicyMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleTerraformRemotePolicy entity, ModuleTerraformRemotePolicyUpdateDto dto)
    {
        ModuleTerraformRemotePolicyMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleTerraformRemotePolicyReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(moduleId, name, organizationId);
        return ModuleTerraformRemotePolicyMapper.ToDto(entity);
    }
}
