// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud.Secrets.Scoped;

public class StackSecretService : GenericCrudService<StackSecret, StackSecretDto, StackSecretDto, StackSecretDto, StackSecretSecuredRepository, StackSecretRepository, StackSecretCreatedEvent, StackSecretUpdatedEvent,
    StackSecretDeletedEvent, StackSecretRepositorySettings>
{
    public StackSecretService(
        StackSecretSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override StackSecret MapToEntity(StackSecretDto dto, Guid organizationId)
    {
        return StackSecretMapper.ToEntity(dto, organizationId);
    }

    protected override StackSecretDto MapToDto(StackSecret entity)
    {
        return StackSecretMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(StackSecret entity, StackSecretDto dto)
    {
        StackSecretMapper.UpdateEntity(entity, dto);
    }

    public async Task<StackSecretDto> GetByName(string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.GetByName(name, organizationId, null));
    }
}