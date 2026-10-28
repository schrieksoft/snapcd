// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleExtraFiles;
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

public class ModuleExtraFileService : GenericCrudService<ModuleExtraFile, ModuleExtraFileMetadata, ModuleExtraFileCreateDto, ModuleExtraFileUpdateDto, ModuleExtraFileReadDto, ModuleExtraFileMetadataReadDto, ModuleExtraFileSecuredRepository, ModuleExtraFileRepository, ModuleExtraFileCreatedEvent,
    ModuleExtraFileUpdatedEvent, ModuleExtraFileDeletedEvent, ModuleExtraFileRepositorySettings>
{
    public ModuleExtraFileService(
        ModuleExtraFileSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleExtraFile MapToEntity(ModuleExtraFileCreateDto dto, Guid organizationId)
    {
        return ModuleExtraFileMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleExtraFileReadDto MapToDto(ModuleExtraFile entity)
    {
        return ModuleExtraFileMapper.ToDto(entity);
    }

    protected override ModuleExtraFileMetadataReadDto MapToMetadataDto(ModuleExtraFileMetadata view)
    {
        return ModuleExtraFileMapper.ToMetadataDto(view);
    }

    protected override void UpdateEntityFromDto(ModuleExtraFile entity, ModuleExtraFileUpdateDto dto)
    {
        ModuleExtraFileMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleExtraFileReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(moduleId, name, organizationId);
        return ModuleExtraFileMapper.ToDto(entity);
    }

    public async Task<ModuleExtraFileMetadataReadDto> GetMetadata(Guid moduleId, string name, Guid organizationId)
    {
        var view = await SecuredRepository.GetMetadata(moduleId, name, organizationId);
        return ModuleExtraFileMapper.ToMetadataDto(view);
    }
}