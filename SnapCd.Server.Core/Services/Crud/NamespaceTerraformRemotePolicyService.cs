// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceTerraformRemotePolicies;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceTerraformRemotePolicyService : GenericCrudService<NamespaceTerraformRemotePolicy, NamespaceTerraformRemotePolicyMetadata, NamespaceTerraformRemotePolicyCreateDto, NamespaceTerraformRemotePolicyUpdateDto, NamespaceTerraformRemotePolicyReadDto, NamespaceTerraformRemotePolicyMetadataReadDto,
    NamespaceTerraformRemotePolicySecuredRepository, NamespaceTerraformRemotePolicyRepository, NamespaceTerraformRemotePolicyCreatedEvent,
    NamespaceTerraformRemotePolicyUpdatedEvent, NamespaceTerraformRemotePolicyDeletedEvent, NamespaceTerraformRemotePolicyRepositorySettings>
{
    public NamespaceTerraformRemotePolicyService(
        NamespaceTerraformRemotePolicySecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceTerraformRemotePolicy MapToEntity(NamespaceTerraformRemotePolicyCreateDto dto, Guid organizationId)
    {
        return NamespaceTerraformRemotePolicyMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceTerraformRemotePolicyReadDto MapToDto(NamespaceTerraformRemotePolicy entity)
    {
        return NamespaceTerraformRemotePolicyMapper.ToDto(entity);
    }

    protected override NamespaceTerraformRemotePolicyMetadataReadDto MapToMetadataDto(NamespaceTerraformRemotePolicyMetadata view)
    {
        return NamespaceTerraformRemotePolicyMapper.ToMetadataDto(view);
    }

    protected override void UpdateEntityFromDto(NamespaceTerraformRemotePolicy entity, NamespaceTerraformRemotePolicyUpdateDto dto)
    {
        NamespaceTerraformRemotePolicyMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceTerraformRemotePolicyReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(namespaceId, name, organizationId);
        return NamespaceTerraformRemotePolicyMapper.ToDto(entity);
    }

    public async Task<NamespaceTerraformRemotePolicyMetadataReadDto> GetMetadata(Guid namespaceId, string name, Guid organizationId)
    {
        var view = await SecuredRepository.GetMetadata(namespaceId, name, organizationId);
        return NamespaceTerraformRemotePolicyMapper.ToMetadataDto(view);
    }
}
