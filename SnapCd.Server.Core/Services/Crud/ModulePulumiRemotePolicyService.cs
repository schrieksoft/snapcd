// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModulePulumiRemotePolicies;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModulePulumiRemotePolicyService : GenericCrudService<ModulePulumiRemotePolicy, ModulePulumiRemotePolicyCreateDto, ModulePulumiRemotePolicyUpdateDto, ModulePulumiRemotePolicyReadDto,
    ModulePulumiRemotePolicySecuredRepository, ModulePulumiRemotePolicyRepository, ModulePulumiRemotePolicyCreatedEvent,
    ModulePulumiRemotePolicyUpdatedEvent, ModulePulumiRemotePolicyDeletedEvent, ModulePulumiRemotePolicyRepositorySettings>
{
    public ModulePulumiRemotePolicyService(
        ModulePulumiRemotePolicySecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModulePulumiRemotePolicy MapToEntity(ModulePulumiRemotePolicyCreateDto dto, Guid organizationId)
    {
        return ModulePulumiRemotePolicyMapper.ToEntity(dto, organizationId);
    }

    protected override ModulePulumiRemotePolicyReadDto MapToDto(ModulePulumiRemotePolicy entity)
    {
        return ModulePulumiRemotePolicyMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModulePulumiRemotePolicy entity, ModulePulumiRemotePolicyUpdateDto dto)
    {
        ModulePulumiRemotePolicyMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModulePulumiRemotePolicyReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(moduleId, name, organizationId);
        return ModulePulumiRemotePolicyMapper.ToDto(entity);
    }
}
