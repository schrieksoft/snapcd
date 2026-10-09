// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespacePulumiInlinePolicies;
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

public class NamespacePulumiInlinePolicyService : GenericCrudService<NamespacePulumiInlinePolicy, NamespacePulumiInlinePolicyMetadata, NamespacePulumiInlinePolicyCreateDto, NamespacePulumiInlinePolicyUpdateDto, NamespacePulumiInlinePolicyReadDto, NamespacePulumiInlinePolicyMetadataReadDto,
    NamespacePulumiInlinePolicySecuredRepository, NamespacePulumiInlinePolicyRepository, NamespacePulumiInlinePolicyCreatedEvent,
    NamespacePulumiInlinePolicyUpdatedEvent, NamespacePulumiInlinePolicyDeletedEvent, NamespacePulumiInlinePolicyRepositorySettings>
{
    public NamespacePulumiInlinePolicyService(
        NamespacePulumiInlinePolicySecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespacePulumiInlinePolicy MapToEntity(NamespacePulumiInlinePolicyCreateDto dto, Guid organizationId)
    {
        return NamespacePulumiInlinePolicyMapper.ToEntity(dto, organizationId);
    }

    protected override NamespacePulumiInlinePolicyReadDto MapToDto(NamespacePulumiInlinePolicy entity)
    {
        return NamespacePulumiInlinePolicyMapper.ToDto(entity);
    }

    protected override NamespacePulumiInlinePolicyMetadataReadDto MapToMetadataDto(NamespacePulumiInlinePolicyMetadata view)
    {
        return NamespacePulumiInlinePolicyMapper.ToMetadataDto(view);
    }

    protected override void UpdateEntityFromDto(NamespacePulumiInlinePolicy entity, NamespacePulumiInlinePolicyUpdateDto dto)
    {
        NamespacePulumiInlinePolicyMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespacePulumiInlinePolicyReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(namespaceId, name, organizationId);
        return NamespacePulumiInlinePolicyMapper.ToDto(entity);
    }

    public async Task<NamespacePulumiInlinePolicyMetadataReadDto> GetMetadata(Guid namespaceId, string name, Guid organizationId)
    {
        var view = await SecuredRepository.GetMetadata(namespaceId, name, organizationId);
        return NamespacePulumiInlinePolicyMapper.ToMetadataDto(view);
    }
}
