// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceHooks;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceHookService : GenericCrudService<NamespaceHook, NamespaceHookCreateDto, NamespaceHookUpdateDto, NamespaceHookReadDto, NamespaceHookSecuredRepository, NamespaceHookRepository,
    NamespaceHookCreatedEvent, NamespaceHookUpdatedEvent, NamespaceHookDeletedEvent, NamespaceHookRepositorySettings>
{
    public NamespaceHookService(
        NamespaceHookSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceHook MapToEntity(NamespaceHookCreateDto dto, Guid organizationId)
    {
        return NamespaceHookMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceHookReadDto MapToDto(NamespaceHook entity)
    {
        return NamespaceHookMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespaceHook entity, NamespaceHookUpdateDto dto)
    {
        NamespaceHookMapper.UpdateEntity(entity, dto);
    }
}
