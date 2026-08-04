// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespacePulumiRemotePolicies;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespacePulumiRemotePolicyService : GenericCrudService<NamespacePulumiRemotePolicy, NamespacePulumiRemotePolicyCreateDto, NamespacePulumiRemotePolicyUpdateDto, NamespacePulumiRemotePolicyReadDto,
    NamespacePulumiRemotePolicySecuredRepository, NamespacePulumiRemotePolicyRepository, NamespacePulumiRemotePolicyCreatedEvent,
    NamespacePulumiRemotePolicyUpdatedEvent, NamespacePulumiRemotePolicyDeletedEvent, NamespacePulumiRemotePolicyRepositorySettings>
{
    public NamespacePulumiRemotePolicyService(
        NamespacePulumiRemotePolicySecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespacePulumiRemotePolicy MapToEntity(NamespacePulumiRemotePolicyCreateDto dto, Guid organizationId)
    {
        return NamespacePulumiRemotePolicyMapper.ToEntity(dto, organizationId);
    }

    protected override NamespacePulumiRemotePolicyReadDto MapToDto(NamespacePulumiRemotePolicy entity)
    {
        return NamespacePulumiRemotePolicyMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespacePulumiRemotePolicy entity, NamespacePulumiRemotePolicyUpdateDto dto)
    {
        NamespacePulumiRemotePolicyMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespacePulumiRemotePolicyReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(namespaceId, name, organizationId);
        return NamespacePulumiRemotePolicyMapper.ToDto(entity);
    }
}
