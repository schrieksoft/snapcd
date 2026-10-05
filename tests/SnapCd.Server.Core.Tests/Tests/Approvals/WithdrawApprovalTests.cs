// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Approvals;

/// <summary>
/// Withdrawing an approval is allowed only while the job is still waiting for one. The condition
/// lives in the delete statement itself, so it holds even if the job leaves the gate concurrently.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class WithdrawApprovalTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _organizationId;
    private Guid _moduleId;
    private readonly List<Guid> _jobIds = new();

    public WithdrawApprovalTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _organizationId = _fixture.Organizations["0"].Id;
        _moduleId = _fixture.Modules["0111"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.ModuleJobApprovals.Where(a => _jobIds.Contains(a.ModuleJobId)).ExecuteDeleteAsync();
        await db.ModuleJobs.Where(j => _jobIds.Contains(j.Id)).ExecuteDeleteAsync();
        _dbContext?.Dispose();
    }

    [Fact]
    public async Task Withdrawing_Is_Allowed_While_The_Job_Waits()
    {
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;
        var (_, approvalId) = await Seed(waitingForApproval: true, approverId: owner.Id);

        await Repo(owner.Id).Delete(approvalId, _organizationId);

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.ModuleJobApprovals.AnyAsync(a => a.Id == approvalId));
    }

    [Fact]
    public async Task Withdrawing_Is_Refused_Once_The_Job_Has_Left_The_Gate()
    {
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;
        var (_, approvalId) = await Seed(waitingForApproval: false, approverId: owner.Id);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => Repo(owner.Id).Delete(approvalId, _organizationId));

        await using var db = _fixture.CreateDbContext();
        Assert.True(await db.ModuleJobApprovals.AnyAsync(a => a.Id == approvalId));
    }

    [Fact]
    public async Task A_Job_That_Never_Reached_The_Gate_Cannot_Have_Its_Approval_Withdrawn()
    {
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;
        var (_, approvalId) = await Seed(waitingForApproval: null, approverId: owner.Id);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => Repo(owner.Id).Delete(approvalId, _organizationId));
    }

    // The UI only offers the icon on your own approval; this is the server saying the same thing.
    [Fact]
    public async Task Another_Principals_Approval_Cannot_Be_Withdrawn()
    {
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;
        var contributor = _fixture.OrganizationPrincipals["0"][OrganizationRole.Contributor].DirectUser;
        var (_, approvalId) = await Seed(waitingForApproval: true, approverId: contributor.Id);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => Repo(owner.Id).Delete(approvalId, _organizationId));

        await using var db = _fixture.CreateDbContext();
        Assert.True(await db.ModuleJobApprovals.AnyAsync(a => a.Id == approvalId));
    }

    // The nonsecured repository carries the condition in the DELETE, so it holds even when the
    // caller has already satisfied CanDelete. This is what closes the race.
    [Fact]
    public async Task The_Delete_Statement_Itself_Refuses_A_Job_Past_The_Gate()
    {
        var owner = _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser;
        var (jobId, approvalId) = await Seed(waitingForApproval: true, approverId: owner.Id);

        // Stand in for the saga clearing the flag after CanDelete has already said yes.
        await using (var db = _fixture.CreateDbContext())
        {
            await db.ModuleJobs.Where(j => j.Id == jobId)
                .ExecuteUpdateAsync(s => s.SetProperty(j => j.WaitingForApproval, false));
        }

        await Assert.ThrowsAsync<ApprovalNoLongerWithdrawableException>(
            () => Nonsecured(owner.Id).ExecuteDelete(approvalId, _organizationId));

        await using var verify = _fixture.CreateDbContext();
        Assert.True(await verify.ModuleJobApprovals.AnyAsync(a => a.Id == approvalId));
    }

    private ModuleJobApprovalSecuredRepository Repo(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        return new ModuleJobApprovalSecuredRepository(
            new ModuleJobApprovalRepository(_dbContext, pp, _fixture.CreateMockBus(),
                Options.Create(new ModuleJobApprovalRepositorySettings())),
            pp,
            new ModuleJobSecuredRepository(
                new ModuleJobRepository(_dbContext, pp, _fixture.CreateMockBus(),
                    Options.Create(new ModuleJobRepositorySettings())),
                pp));
    }

    private ModuleJobApprovalRepository Nonsecured(Guid principalId)
    {
        var pp = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        return new ModuleJobApprovalRepository(_dbContext, pp, _fixture.CreateMockBus(),
            Options.Create(new ModuleJobApprovalRepositorySettings()));
    }

    private async Task<(Guid JobId, Guid ApprovalId)> Seed(bool? waitingForApproval, Guid approverId)
    {
        var jobId = Guid.NewGuid();
        var approvalId = Guid.NewGuid();
        _jobIds.Add(jobId);

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            ModuleId = _moduleId,
            OrganizationId = _organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            JobType = "Apply",
            WaitingForApproval = waitingForApproval,
            IsCurrent = false
        });
        db.ModuleJobApprovals.Add(new ModuleJobApproval
        {
            Id = approvalId,
            ModuleJobId = jobId,
            OrganizationId = _organizationId,
            PrincipalId = approverId,
            PrincipalDiscriminator = PrincipalDiscriminator.User,
            DecisionDateTime = DateTime.UtcNow,
            Declined = false
        });
        await db.SaveChangesAsync();

        return (jobId, approvalId);
    }
}
