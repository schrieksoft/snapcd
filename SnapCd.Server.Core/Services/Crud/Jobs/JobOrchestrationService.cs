// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Jobs;
using SnapCd.Contracts.Dto.ModuleJobApprovals;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Logging;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud.Jobs;

/// <summary>
/// Coordinates multi-step Job operations (Apply/Destroy/Approve/Decline/Cancel) that don't fit
/// the GenericCrudService shape because they touch multiple repositories and require pre-dispatch
/// validation against the live module/job state. Lives outside the generic CRUD pipeline by design.
/// </summary>
public class JobOrchestrationService
{
    private readonly SecuredJobServiceFactory _securedJobServiceFactory;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ModuleJobApprovalSecuredRepositoryFactory _approvalRepoFactory;
    private readonly IPrincipalProvider _principalProvider;
    private readonly ILogRedactor _logRedactor;

    public JobOrchestrationService(
        SecuredJobServiceFactory securedJobServiceFactory,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ModuleJobApprovalSecuredRepositoryFactory approvalRepoFactory,
        IPrincipalProvider principalProvider,
        ILogRedactor logRedactor)
    {
        _securedJobServiceFactory = securedJobServiceFactory;
        _dbContextFactory = dbContextFactory;
        _approvalRepoFactory = approvalRepoFactory;
        _principalProvider = principalProvider;
        _logRedactor = logRedactor;
    }

    public async Task<Guid> Apply(Guid moduleId, Guid organizationId, Guid? correlationId)
    {
        await EnsureModuleExists(moduleId, organizationId);
        using var gatekeeping = _securedJobServiceFactory.Create();
        var jobId = correlationId ?? Guid.NewGuid();
        await gatekeeping.Apply(moduleId, organizationId, jobId);
        return jobId;
    }

    public async Task<Guid> Destroy(Guid moduleId, Guid organizationId, Guid? correlationId)
    {
        await EnsureModuleExists(moduleId, organizationId);
        using var gatekeeping = _securedJobServiceFactory.Create();
        var jobId = correlationId ?? Guid.NewGuid();
        await gatekeeping.Destroy(moduleId, organizationId, jobId);
        return jobId;
    }

    public Task Approve(Guid jobId, Guid organizationId, string? reason)
        => RecordApprovalDecision(jobId, organizationId, declined: false, reason);

    public Task Decline(Guid jobId, Guid organizationId, string reason)
        => RecordApprovalDecision(jobId, organizationId, declined: true, reason);

    public async Task Cancel(Guid jobId, Guid organizationId, CancellationType cancellationType)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var job = await dbContext.ModuleJobs
            .Where(j => j.Id == jobId && j.OrganizationId == organizationId)
            .Select(j => new { j.ModuleId, ModuleNamespaceId = j.Module.NamespaceId })
            .FirstOrDefaultAsync();

        if (job is null) throw new EntityNotFoundException($"Job '{jobId}' not found");

        using var gatekeeping = _securedJobServiceFactory.Create();
        await gatekeeping.Cancel(jobId, job.ModuleId, job.ModuleNamespaceId, organizationId, cancellationType);
    }

    public async Task<string> GetLogs(Guid jobId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var logs = await dbContext.ModuleJobs
            .Where(j => j.Id == jobId && j.OrganizationId == organizationId)
            .Select(j => j.Logs)
            .FirstOrDefaultAsync();

        if (logs is null) throw new EntityNotFoundException($"Job '{jobId}' not found");
        return _logRedactor.Redact(logs);
    }

    public async Task<ModuleJobStatusDto> GetStatus(Guid jobId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var dto = await dbContext.ModuleJobs
            .Where(j => j.Id == jobId && j.OrganizationId == organizationId)
            .Select(j => new ModuleJobStatusDto
            {
                Id = j.Id,
                ModuleId = j.ModuleId,
                JobType = j.JobType,
                WaitingForApproval = j.WaitingForApproval,
                ActualStateHeadline = j.ActualStateHeadline != null ? j.ActualStateHeadline.ToString() : null,
                ServerSideErrorHeader = j.ServerSideErrorHeader,
                ServerSideError = j.ServerSideError,
                OutputsUnchangedList = j.OutputsUnchangedList,
                OutputsCreateList = j.OutputsCreateList,
                OutputsModifyList = j.OutputsModifyList,
                OutputsDestroyList = j.OutputsDestroyList,
                OutputsRecreateList = j.OutputsRecreateList
            })
            .FirstOrDefaultAsync();

        if (dto is null) throw new EntityNotFoundException($"Job '{jobId}' not found");
        return dto;
    }

    public async Task<List<ModuleJobApprovalReadDto>> GetApprovals(Guid jobId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Ensure the job exists in the org before returning approvals — distinguish "no decisions yet"
        // (return []) from "job doesn't exist" (404).
        var jobExists = await dbContext.ModuleJobs
            .AnyAsync(j => j.Id == jobId && j.OrganizationId == organizationId);
        if (!jobExists) throw new EntityNotFoundException($"Job '{jobId}' not found");

        return await dbContext.ModuleJobApprovals
            .Where(a => a.ModuleJobId == jobId && a.OrganizationId == organizationId)
            .OrderBy(a => a.DecisionDateTime)
            .Select(a => new ModuleJobApprovalReadDto
            {
                Id = a.Id,
                ModuleJobId = a.ModuleJobId,
                PrincipalId = a.PrincipalId,
                PrincipalDiscriminator = a.PrincipalDiscriminator,
                DecisionDateTime = a.DecisionDateTime,
                Declined = a.Declined
            })
            .ToListAsync();
    }

    private async Task EnsureModuleExists(Guid moduleId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var exists = await dbContext.Modules.AnyAsync(m => m.Id == moduleId && m.OrganizationId == organizationId);
        if (!exists) throw new EntityNotFoundException($"Module '{moduleId}' not found");
    }

    private async Task RecordApprovalDecision(Guid jobId, Guid organizationId, bool declined, string? reason)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var exists = await dbContext.ModuleJobs.AnyAsync(j => j.Id == jobId && j.OrganizationId == organizationId);
        if (!exists) throw new EntityNotFoundException($"Job '{jobId}' not found");

        using var approvalRepo = _approvalRepoFactory.Create();
        var approval = new ModuleJobApproval
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleJobId = jobId,
            DecisionDateTime = DateTime.UtcNow,
            Declined = declined,
            PrincipalId = _principalProvider.GetSubject(organizationId),
            PrincipalDiscriminator = _principalProvider.GetPrincipalDiscriminator(),
            AgentId = _principalProvider.GetAgentId(),
            Reason = reason
        };
        await approvalRepo.Create(approval);
    }
}
