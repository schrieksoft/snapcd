// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleTerraformLocalPolicies;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleTerraformLocalPolicyService : GenericCrudService<ModuleTerraformLocalPolicy, ModuleTerraformLocalPolicyCreateDto, ModuleTerraformLocalPolicyUpdateDto, ModuleTerraformLocalPolicyReadDto,
    ModuleTerraformLocalPolicySecuredRepository, ModuleTerraformLocalPolicyRepository, ModuleTerraformLocalPolicyCreatedEvent,
    ModuleTerraformLocalPolicyUpdatedEvent, ModuleTerraformLocalPolicyDeletedEvent, ModuleTerraformLocalPolicyRepositorySettings>
{
    public ModuleTerraformLocalPolicyService(
        ModuleTerraformLocalPolicySecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleTerraformLocalPolicy MapToEntity(ModuleTerraformLocalPolicyCreateDto dto, Guid organizationId)
    {
        return ModuleTerraformLocalPolicyMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleTerraformLocalPolicyReadDto MapToDto(ModuleTerraformLocalPolicy entity)
    {
        return ModuleTerraformLocalPolicyMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleTerraformLocalPolicy entity, ModuleTerraformLocalPolicyUpdateDto dto)
    {
        ModuleTerraformLocalPolicyMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleTerraformLocalPolicyReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(moduleId, name, organizationId);
        return ModuleTerraformLocalPolicyMapper.ToDto(entity);
    }
}
