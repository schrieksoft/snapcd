// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts.Dto.Missions;
using SnapCd.Contracts.Dto.Modules;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleService : GenericCrudService<Module, ModuleCreateDto, ModuleUpdateDto, ModuleReadDto, ModuleSecuredRepository, ModuleRepository, ModuleCreatedEvent, ModuleUpdatedEvent, ModuleDeletedEvent, ModuleRepositorySettings>
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    private readonly ModuleSagaSecuredRepositoryFactory _sagaSecuredRepositoryFactory;

    public ModuleService(
        ModuleSecuredRepository securedRepository,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ModuleSagaSecuredRepositoryFactory sagaSecuredRepositoryFactory
    ) : base(securedRepository)
    {
        _sagaSecuredRepositoryFactory = sagaSecuredRepositoryFactory;
        _dbContextFactory = dbContextFactory;
    }

    protected override Module MapToEntity(ModuleCreateDto dto, Guid organizationId)
    {
        return ModuleMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleReadDto MapToDto(Module entity)
    {
        return ModuleMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(Module entity, ModuleUpdateDto dto)
    {
        ModuleMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleReadDto> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var module = await GetByCriteria(repo => repo.Get(namespaceId, name, organizationId));
        return module;
    }

    public async Task<ModuleReadDto> GetByName(string stackName, string namespaceName, string moduleName, Guid organizationId)
    {
        var module = await GetByCriteria(repo => repo.Get(stackName, namespaceName, moduleName, organizationId));
        return module;
    }

    public async Task<ModuleSourceDto> GetSource(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var dto = await dbContext.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new ModuleSourceDto
            {
                Id = m.Id,
                Name = m.Name,
                NamespaceId = m.NamespaceId,
                SourceType = m.SourceType,
                SourceUrl = m.SourceUrl,
                SourceRevision = m.SourceRevision,
                SourceRevisionType = m.SourceRevisionType,
                SourceSubdirectory = m.SourceSubdirectory,
                Engine = m.Engine
            })
            .FirstOrDefaultAsync();

        if (dto is null) throw new EntityNotFoundException($"Module '{moduleId}' not found");
        return dto;
    }

    public async Task<ModuleStateDto> GetState(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var moduleHeader = await dbContext.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new { m.Id, m.Name, m.NamespaceId })
            .FirstOrDefaultAsync();

        if (moduleHeader is null) throw new EntityNotFoundException($"Module '{moduleId}' not found");

        var lastJob = await dbContext.ModuleJobs
            .Where(j => j.ModuleId == moduleId && j.OrganizationId == organizationId)
            .OrderByDescending(j => j.Id)
            .Select(j => new
            {
                j.Id,
                j.JobType,
                ActualStateHeadline = j.ActualStateHeadline != null ? j.ActualStateHeadline.ToString() : null,
                j.IsCurrent,
                j.WaitingForApproval,
                j.ServerSideErrorHeader
            })
            .FirstOrDefaultAsync();

        return new ModuleStateDto
        {
            Id = moduleHeader.Id,
            Name = moduleHeader.Name,
            NamespaceId = moduleHeader.NamespaceId,
            LastJobId = lastJob?.Id,
            LastJobType = lastJob?.JobType,
            LastActualStateHeadline = lastJob?.ActualStateHeadline,
            LastIsCurrent = lastJob?.IsCurrent,
            LastWaitingForApproval = lastJob?.WaitingForApproval,
            LastServerSideErrorHeader = lastJob?.ServerSideErrorHeader
        };
    }

    public async Task<List<ModuleMissionHistoryEntryDto>> GetMissionHistory(Guid moduleId, Guid organizationId, int take = 5)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var moduleExists = await dbContext.Modules
            .AnyAsync(m => m.Id == moduleId && m.OrganizationId == organizationId);
        if (!moduleExists) throw new EntityNotFoundException($"Module '{moduleId}' not found");

        var entries = await dbContext.ModuleJobMissionRuns
            .Where(run => run.OrganizationId == organizationId)
            .Join(
                dbContext.ModuleJobs.Where(j => j.ModuleId == moduleId),
                run => run.ModuleJobId,
                job => job.Id,
                (run, job) => new ModuleMissionHistoryEntryDto
                {
                    RunId = run.Id,
                    ModuleJobId = run.ModuleJobId,
                    JobType = job.JobType,
                    MissionType = run.MissionType,
                    Status = run.Status,
                    DiagnosisCategory = run.DiagnosisCategory,
                    ResultSummary = run.ResultSummary,
                    DefinitiveRevision = job.DefinitiveRevision,
                    StartedAt = run.StartedAt,
                    CompletedAt = run.CompletedAt
                })
            .OrderByDescending(e => e.CompletedAt)
            .ThenByDescending(e => e.StartedAt)
            .Take(take)
            .ToListAsync();

        if (entries.Count == 0) return entries;

        var runIds = entries.Select(e => e.RunId).ToList();
        var milestones = await dbContext.ModuleJobMissionRunMilestones
            .Where(m => m.OrganizationId == organizationId && runIds.Contains(m.ModuleJobMissionRunId))
            .OrderBy(m => m.ReportedAt)
            .Select(m => new ModuleJobMissionRunMilestoneReadDto
            {
                Id = m.Id,
                ModuleJobMissionRunId = m.ModuleJobMissionRunId,
                Kind = m.Kind,
                Message = m.Message,
                ReportedAt = m.ReportedAt
            })
            .ToListAsync();

        foreach (var entry in entries)
            entry.Milestones = milestones.Where(m => m.ModuleJobMissionRunId == entry.RunId).ToList();

        return entries;
    }

    public async Task<ModulePauseDto> SetPaused(Guid moduleId, Guid organizationId, bool paused, string? reason)
    {
        using var sagaRepo = _sagaSecuredRepositoryFactory.Create();
        var saga = await sagaRepo.SetPaused(moduleId, organizationId, paused, reason);

        return new ModulePauseDto
        {
            ModuleId = saga.CorrelationId,
            Paused = saga.Paused,
            PausedBy = saga.PausedBy,
            PausedAt = saga.PausedAt,
            PauseReason = saga.PauseReason
        };
    }

}