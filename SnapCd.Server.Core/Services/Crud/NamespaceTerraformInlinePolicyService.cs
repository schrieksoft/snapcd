// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceTerraformInlinePolicies;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceTerraformInlinePolicyService : GenericCrudService<NamespaceTerraformInlinePolicy, NamespaceTerraformInlinePolicyCreateDto, NamespaceTerraformInlinePolicyUpdateDto, NamespaceTerraformInlinePolicyReadDto,
    NamespaceTerraformInlinePolicySecuredRepository, NamespaceTerraformInlinePolicyRepository, NamespaceTerraformInlinePolicyCreatedEvent,
    NamespaceTerraformInlinePolicyUpdatedEvent, NamespaceTerraformInlinePolicyDeletedEvent, NamespaceTerraformInlinePolicyRepositorySettings>
{
    public NamespaceTerraformInlinePolicyService(
        NamespaceTerraformInlinePolicySecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceTerraformInlinePolicy MapToEntity(NamespaceTerraformInlinePolicyCreateDto dto, Guid organizationId)
    {
        return NamespaceTerraformInlinePolicyMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceTerraformInlinePolicyReadDto MapToDto(NamespaceTerraformInlinePolicy entity)
    {
        return NamespaceTerraformInlinePolicyMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespaceTerraformInlinePolicy entity, NamespaceTerraformInlinePolicyUpdateDto dto)
    {
        NamespaceTerraformInlinePolicyMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceTerraformInlinePolicyReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(namespaceId, name, organizationId);
        return NamespaceTerraformInlinePolicyMapper.ToDto(entity);
    }
}
