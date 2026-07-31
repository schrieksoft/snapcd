// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceAdditionalTriggerPaths;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceAdditionalTriggerPathService : GenericCrudService<NamespaceAdditionalTriggerPath, NamespaceAdditionalTriggerPathCreateDto, NamespaceAdditionalTriggerPathUpdateDto, NamespaceAdditionalTriggerPathReadDto,
    NamespaceAdditionalTriggerPathSecuredRepository, NamespaceAdditionalTriggerPathRepository, NamespaceAdditionalTriggerPathCreatedEvent,
    NamespaceAdditionalTriggerPathUpdatedEvent, NamespaceAdditionalTriggerPathDeletedEvent, NamespaceAdditionalTriggerPathRepositorySettings>
{
    public NamespaceAdditionalTriggerPathService(
        NamespaceAdditionalTriggerPathSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceAdditionalTriggerPath MapToEntity(NamespaceAdditionalTriggerPathCreateDto dto, Guid organizationId)
    {
        return NamespaceAdditionalTriggerPathMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceAdditionalTriggerPathReadDto MapToDto(NamespaceAdditionalTriggerPath entity)
    {
        return NamespaceAdditionalTriggerPathMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespaceAdditionalTriggerPath entity, NamespaceAdditionalTriggerPathUpdateDto dto)
    {
        NamespaceAdditionalTriggerPathMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceAdditionalTriggerPathReadDto> Get(Guid namespaceId, string path, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(namespaceId, path, organizationId);
        return NamespaceAdditionalTriggerPathMapper.ToDto(entity);
    }
}
