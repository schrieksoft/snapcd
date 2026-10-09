// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Runners;
using SnapCd.Server.Core.Entities.Definition;
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

public class RunnerService : GenericCrudService<
    Runner, RunnerMetadata,
    RunnerCreateDto,
    RunnerUpdateDto,
    RunnerReadDto,
    RunnerSecuredRepository,
    RunnerRepository,
    RunnerCreatedEvent,
    RunnerUpdatedEvent,
    RunnerDeletedEvent,
    RunnerRepositorySettings>
{
    public RunnerService(
        RunnerSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override Runner MapToEntity(RunnerCreateDto dto, Guid organizationId)
    {
        return RunnerMapper.ToEntity(dto, organizationId);
    }

    protected override RunnerReadDto MapToDto(Runner entity)
    {
        return RunnerMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(Runner entity, RunnerUpdateDto dto)
    {
        RunnerMapper.UpdateEntity(entity, dto);
    }

    public async Task<RunnerReadDto> GetByName(string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.GetByName(name, organizationId));
    }

    public async Task<List<RunnerMetadataReadDto>> ListMetadata(Guid organizationId)
    {
        var views = await SecuredRepository.ListMetadata(organizationId);
        return views.Select(RunnerMapper.ToMetadataDto).ToList();
    }

    public async Task<RunnerMetadataReadDto> GetMetadataByName(string name, Guid organizationId)
    {
        var view = await SecuredRepository.GetMetadataByName(name, organizationId);
        return RunnerMapper.ToMetadataDto(view);
    }

}