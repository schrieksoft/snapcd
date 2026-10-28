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
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Services.Crud.Secrets.Scoped;

public class NamespaceSecretService : GenericCrudService<NamespaceSecret, NamespaceSecretMetadata, NamespaceSecretDto, NamespaceSecretDto, NamespaceSecretDto, NamespaceSecretMetadataReadDto, NamespaceSecretSecuredRepository, NamespaceSecretRepository, NamespaceSecretCreatedEvent,
    NamespaceSecretUpdatedEvent, NamespaceSecretDeletedEvent, NamespaceSecretRepositorySettings>
{
    public NamespaceSecretService(
        NamespaceSecretSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespaceSecret MapToEntity(NamespaceSecretDto dto, Guid organizationId)
    {
        return NamespaceSecretMapper.ToEntity(dto, organizationId);
    }

    protected override NamespaceSecretDto MapToDto(NamespaceSecret entity)
    {
        return NamespaceSecretMapper.ToDto(entity);
    }

    protected override NamespaceSecretMetadataReadDto MapToMetadataDto(NamespaceSecretMetadata view)
    {
        return NamespaceSecretMapper.ToMetadataDto(view);
    }

    protected override void UpdateEntityFromDto(NamespaceSecret entity, NamespaceSecretDto dto)
    {
        NamespaceSecretMapper.UpdateEntity(entity, dto);
    }

    public async Task<NamespaceSecretDto> GetByName(string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.GetByName(name, organizationId, null));
    }

    public async Task<NamespaceSecretMetadataReadDto> GetMetadataByName(string name, Guid organizationId)
    {
        var view = await SecuredRepository.GetMetadataByName(name, organizationId);
        return NamespaceSecretMapper.ToMetadataDto(view);
    }
}