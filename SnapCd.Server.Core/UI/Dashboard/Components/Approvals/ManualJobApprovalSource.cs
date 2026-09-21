// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Approvals;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.UI.Dashboard.Components.Approvals;

/// <summary>
/// Approvals on a manual job. The decisions live in ManualModuleJobApprovals and the threshold is
/// the state-migration one, which a split shares with every other job that writes state.
/// </summary>
public class ManualJobApprovalSource : IApprovalSource
{
    private readonly ManualModuleJobSecuredRepositoryFactory _jobRepoFactory;
    private readonly ManualJobServiceFactory _serviceFactory;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IPrincipalProvider _principalProvider;

    public ManualJobApprovalSource(
        ManualModuleJobSecuredRepositoryFactory jobRepoFactory,
        ManualJobServiceFactory serviceFactory,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IPrincipalProvider principalProvider)
    {
        _jobRepoFactory = jobRepoFactory;
        _serviceFactory = serviceFactory;
        _dbContextFactory = dbContextFactory;
        _principalProvider = principalProvider;
    }

    /// <summary>A decision on a manual job cannot be withdrawn; the job acts on it immediately.</summary>
    public bool CanRemoveApproval => false;

    public string DecisionNoun => "Split";

    public async Task<IReadOnlyList<IJobApproval>> ListApprovals(Guid jobId, Guid moduleId, Guid organizationId)
    {
        using var repo = _jobRepoFactory.Create(_principalProvider);
        var approvals = await repo.ListApprovals(jobId, moduleId, organizationId);
        return approvals.Cast<IJobApproval>().ToList();
    }

    public async Task<bool> IsWaitingForApproval(Guid jobId, Guid moduleId, Guid organizationId)
    {
        using var repo = _jobRepoFactory.Create(_principalProvider);
        var job = await repo.Get(jobId, moduleId, organizationId);
        return job.WaitingForApproval == true;
    }

    public async Task<int?> ResolveThreshold(Guid jobId, Guid moduleId, Guid organizationId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        return await StateMigrationApprovalThreshold.Resolve(moduleId, organizationId, db);
    }

    public async Task Approve(Guid jobId, Guid moduleId, Guid organizationId)
    {
        using var service = _serviceFactory.Create(_principalProvider);
        await service.Decide(jobId, moduleId, organizationId, declined: false);
    }

    public async Task Decline(Guid jobId, Guid moduleId, Guid organizationId)
    {
        using var service = _serviceFactory.Create(_principalProvider);
        await service.Decide(jobId, moduleId, organizationId, declined: true);
    }

    public Task RemoveApproval(Guid approvalId, Guid organizationId) =>
        throw new NotSupportedException("A decision on a manual job cannot be withdrawn.");
}
