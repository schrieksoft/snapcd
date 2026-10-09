// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Namespaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Services.Crud;

public class
    NamespaceService : GenericCrudService<
    Entities.Definition.Namespace, 
    NamespaceMetadata,NamespaceCreateDto, NamespaceUpdateDto, 
    NamespaceReadDto, 
    NamespaceSecuredRepository, 
    NamespaceRepository, 
    NamespaceCreatedEvent, 
    NamespaceUpdatedEvent,
    NamespaceDeletedEvent, 
    NamespaceRepositorySettings>
{
    public NamespaceService(
        NamespaceSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override Entities.Definition.Namespace MapToEntity(NamespaceCreateDto dto, Guid organizationId)
    {
        return NamespaceMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceReadDto MapToDto(Entities.Definition.Namespace entity)
    {
        return NamespaceMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(Entities.Definition.Namespace entity, NamespaceUpdateDto dto)
    {
        NamespaceMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceReadDto> Get(Guid stackId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(stackId, name, organizationId));
    }

    public async Task<List<NamespaceMetadataReadDto>> ListMetadata(Guid organizationId)
    {
        var views = await SecuredRepository.ListMetadata(organizationId);
        return views.Select(NamespaceMapper.ToMetadataDto).ToList();
    }

    public async Task<NamespaceMetadataReadDto> GetMetadata(Guid stackId, string name, Guid organizationId)
    {
        var view = await SecuredRepository.GetMetadata(stackId, name, organizationId);
        return NamespaceMapper.ToMetadataDto(view);
    }

    public async Task<NamespaceMetadataReadDto> GetMetadata(string stackName, string name, Guid organizationId)
    {
        var view = await SecuredRepository.GetMetadata(stackName, name, organizationId);
        return NamespaceMapper.ToMetadataDto(view);
    }

}