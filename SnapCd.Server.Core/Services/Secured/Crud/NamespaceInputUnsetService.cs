// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Interfaces.Crud;
using SnapCd.Server.Core.Services.Secured.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Services.Secured.Crud;

public class NamespaceInputUnsetService<TEntity> : GenericCrudService<
    TEntity,
    NamespaceInputUnsetMetadata,
    NamespaceInputUnsetCreateDto,
    NamespaceInputUnsetUpdateDto,
    NamespaceInputUnsetReadDto,
    NamespaceInputUnsetMetadataReadDto,
    NamespaceInputUnsetSecuredRepository<TEntity>,
    NamespaceInputUnsetRepository<TEntity>,
    NamespaceInputUnsetCreatedEvent,
    NamespaceInputUnsetUpdatedEvent,
    NamespaceInputUnsetDeletedEvent,
    NamespaceInputUnsetRepositorySettings>, INamespaceInputUnsetService
    where TEntity : NamespaceInput, INamespaceInputUnset, new()
{
    public NamespaceInputUnsetService(
        NamespaceInputUnsetSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(NamespaceInputUnsetCreateDto dto, Guid organizationId)
    {
        return NamespaceInputUnsetMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override NamespaceInputUnsetReadDto MapToDto(TEntity entity)
    {
        return NamespaceInputUnsetMapper.ToDto(entity);
    }

    protected override NamespaceInputUnsetMetadataReadDto MapToMetadataDto(NamespaceInputUnsetMetadata view)
    {
        return NamespaceInputUnsetMapper.ToMetadataDto(view);
    }

    protected override void UpdateEntityFromDto(TEntity entity, NamespaceInputUnsetUpdateDto dto)
    {
        NamespaceInputUnsetMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceInputUnsetReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(namespaceId, name, organizationId));
    }

    public async Task<Dictionary<string, NamespaceInputUnsetReadDto>> GetLiterals(
        Guid namespaceId,
        List<string> paramNames,
        Guid organizationId)
    {
        var result = await SecuredRepository.GetLiterals(namespaceId, paramNames, organizationId);

        Dictionary<string, NamespaceInputUnsetReadDto> resultDict = new();
        foreach (var kvp in result) resultDict[kvp.Key] = NamespaceInputUnsetMapper.ToDto(kvp.Value);

        return resultDict;
    }
}