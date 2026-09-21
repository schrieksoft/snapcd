// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using ModuleEntity = SnapCd.Server.Core.Entities.Definition.Module;
using NamespaceEntity = SnapCd.Server.Core.Entities.Definition.Namespace;

namespace SnapCd.Server.Core.UI.Dashboard.Components.Approvals;

/// <summary>
/// Approvals on an apply or destroy job. Which threshold applies depends on the saga that owns the
/// job, so the saga type is part of the source rather than something the view passes in.
/// </summary>
public class DeploymentJobApprovalSource : IApprovalSource
{
    private readonly ModuleSecuredRepositoryFactory _moduleRepoFactory;
    private readonly ModuleJobSecuredRepositoryFactory _jobRepoFactory;
    private readonly ModuleJobApprovalSecuredRepositoryFactory _approvalRepoFactory;
    private readonly IPrincipalProvider _principalProvider;
    private readonly string _jobSagaType;
    private readonly Guid _principalId;
    private readonly PrincipalDiscriminator _principalDiscriminator;

    public DeploymentJobApprovalSource(
        ModuleSecuredRepositoryFactory moduleRepoFactory,
        ModuleJobSecuredRepositoryFactory jobRepoFactory,
        ModuleJobApprovalSecuredRepositoryFactory approvalRepoFactory,
        IPrincipalProvider principalProvider,
        string jobSagaType,
        Guid principalId,
        PrincipalDiscriminator principalDiscriminator)
    {
        _moduleRepoFactory = moduleRepoFactory;
        _jobRepoFactory = jobRepoFactory;
        _approvalRepoFactory = approvalRepoFactory;
        _principalProvider = principalProvider;
        _jobSagaType = jobSagaType;
        _principalId = principalId;
        _principalDiscriminator = principalDiscriminator;
    }

    public bool CanRemoveApproval => true;

    public string DecisionNoun => "Plan";

    public async Task<IReadOnlyList<IJobApproval>> ListApprovals(Guid jobId, Guid moduleId, Guid organizationId)
    {
        using var repo = _approvalRepoFactory.Create(_principalProvider);
        var approvals = await repo.ListByJob(jobId, organizationId);
        return approvals.Cast<IJobApproval>().ToList();
    }

    public async Task<bool> IsWaitingForApproval(Guid jobId, Guid moduleId, Guid organizationId)
    {
        using var repo = _jobRepoFactory.Create(_principalProvider);
        var job = await repo.Get(jobId, organizationId);
        return job.WaitingForApproval.GetValueOrDefault();
    }

    public async Task<int?> ResolveThreshold(Guid jobId, Guid moduleId, Guid organizationId)
    {
        if (_jobSagaType != nameof(ApplyJobSaga) && _jobSagaType != nameof(DestroyJobSaga))
            return null;

        using var repo = _moduleRepoFactory.Create(_principalProvider);

        var module = await repo.Get<ModuleEntity>(moduleId, organizationId, x => x
            .Include(m => m.Namespace)
            .Select(y => new ModuleEntity
            {
                Id = moduleId,
                ApplyApprovalThreshold = y.ApplyApprovalThreshold,
                DestroyApprovalThreshold = y.DestroyApprovalThreshold,
                Namespace = new NamespaceEntity
                {
                    Id = y.Namespace.Id,
                    DefaultApplyApprovalThreshold = y.Namespace.DefaultApplyApprovalThreshold,
                    DefaultDestroyApprovalThreshold = y.Namespace.DefaultDestroyApprovalThreshold
                }
            }));

        return _jobSagaType == nameof(ApplyJobSaga)
            ? module.ApplyApprovalThreshold ?? module.Namespace.DefaultApplyApprovalThreshold ?? 0
            : module.DestroyApprovalThreshold ?? module.Namespace.DefaultDestroyApprovalThreshold ?? 0;
    }

    public Task Approve(Guid jobId, Guid moduleId, Guid organizationId) =>
        Record(jobId, organizationId, declined: false);

    public Task Decline(Guid jobId, Guid moduleId, Guid organizationId) =>
        Record(jobId, organizationId, declined: true);

    private async Task Record(Guid jobId, Guid organizationId, bool declined)
    {
        using var repo = _approvalRepoFactory.Create(_principalProvider);
        await repo.Create(new ModuleJobApproval
        {
            Id = Guid.NewGuid(),
            ModuleJobId = jobId,
            OrganizationId = organizationId,
            DecisionDateTime = DateTime.UtcNow,
            Declined = declined,
            PrincipalId = _principalId,
            PrincipalDiscriminator = _principalDiscriminator
        });
    }

    public async Task RemoveApproval(Guid approvalId, Guid organizationId)
    {
        using var repo = _approvalRepoFactory.Create(_principalProvider);
        await repo.Delete(approvalId, organizationId);
    }
}
