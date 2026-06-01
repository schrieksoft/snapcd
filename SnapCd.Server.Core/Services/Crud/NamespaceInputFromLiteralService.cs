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
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Services.Crud.Interfaces;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespaceInputFromLiteralService<TEntity> : GenericCrudService<
    TEntity,
    NamespaceInputFromLiteralCreateDto,
    NamespaceInputFromLiteralUpdateDto,
    NamespaceInputFromLiteralReadDto,
    NamespaceInputFromLiteralSecuredRepository<TEntity>,
    NamespaceInputFromLiteralRepository<TEntity>,
    NamespaceInputFromLiteralCreatedEvent,
    NamespaceInputFromLiteralUpdatedEvent,
    NamespaceInputFromLiteralDeletedEvent,
    NamespaceInputFromLiteralRepositorySettings>, INamespaceInputFromLiteralService
    where TEntity : NamespaceInputWithType, INamespaceInputFromLiteral, new()
{
    public NamespaceInputFromLiteralService(
        NamespaceInputFromLiteralSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(NamespaceInputFromLiteralCreateDto dto, Guid organizationId)
    {
        return NamespaceInputFromLiteralMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override NamespaceInputFromLiteralReadDto MapToDto(TEntity entity)
    {
        return NamespaceInputFromLiteralMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(TEntity entity, NamespaceInputFromLiteralUpdateDto dto)
    {
        NamespaceInputFromLiteralMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceInputFromLiteralReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(namespaceId, name, organizationId));
    }

    public async Task<Dictionary<string, NamespaceInputFromLiteralReadDto>> GetLiterals(
        Guid namespaceId,
        List<string> paramNames,
        Guid organizationId)
    {
        var result = await SecuredRepository.GetLiterals(namespaceId, paramNames, organizationId);

        Dictionary<string, NamespaceInputFromLiteralReadDto> resultDict = new();
        foreach (var kvp in result) resultDict[kvp.Key] = NamespaceInputFromLiteralMapper.ToDto(kvp.Value);

        return resultDict;
    }
}