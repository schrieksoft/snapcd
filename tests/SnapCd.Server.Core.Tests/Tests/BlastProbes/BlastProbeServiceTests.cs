// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.BlastProbes;

/// <summary>
/// The request path: a job row and a published request, with nothing listening for it yet.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class BlastProbeServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _moduleId;
    private Guid _upstreamModuleId;
    private Guid _organizationId;
    private readonly List<Guid> _created = [];

    public BlastProbeServiceTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _moduleId = _fixture.Modules["0000"].Id;
        _upstreamModuleId = _fixture.Modules["0001"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_created.Count > 0)
        {
            await _dbContext.BlastProbeJobs
                .Where(j => _created.Contains(j.Id))
                .ExecuteDeleteAsync();
        }

        await _dbContext.DisposeAsync();
    }

    /// <summary>
    /// Read permission on the consumer Module is the whole of the gate: the plan reads that Module
    /// and changes nothing, so there is no pause check and no refusal for a Module that is busy.
    /// </summary>
    [Fact]
    public async Task Start_refuses_a_principal_who_cannot_read_the_module()
    {
        var service = CreateService(asOwner: false);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.Start(_moduleId, _organizationId, _upstreamModuleId, "{}"));

        Assert.False(await _dbContext.BlastProbeJobs.AsNoTracking()
            .AnyAsync(j => j.ModuleId == _moduleId && j.OrganizationId == _organizationId));
    }

    /// <summary>
    /// Built without the dependencies a launch needs, the service refuses before it writes
    /// anything. Asserted because the alternative is a row left Running with no request behind it.
    /// </summary>
    [Fact]
    public async Task Start_without_launch_dependencies_writes_no_row()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Start(_moduleId, _organizationId, _upstreamModuleId, "{}"));

        Assert.False(await _dbContext.BlastProbeJobs.AsNoTracking()
            .AnyAsync(j => j.ModuleId == _moduleId && j.OrganizationId == _organizationId));
    }

    /// <summary>
    /// Two plans on one Module at once. This is the case StateMigrationJobs forbids with a unique
    /// index filtered on Running, and the case this family exists to allow.
    /// </summary>
    [Fact]
    public async Task A_second_plan_can_run_while_the_first_is_running()
    {
        var first = await Insert(ExecutionStatus.Running);
        var second = await Insert(ExecutionStatus.Running);

        Assert.NotEqual(first, second);

        var running = await _dbContext.BlastProbeJobs.AsNoTracking()
            .CountAsync(j => j.ModuleId == _moduleId
                             && j.OrganizationId == _organizationId
                             && j.Status == ExecutionStatus.Running);

        Assert.Equal(2, running);
    }

    [Fact]
    public async Task Repository_lists_the_plans_prompted_by_one_upstream_job()
    {
        var upstreamJobId = Guid.NewGuid();

        var mine = await Insert(ExecutionStatus.Running, upstreamJobId);
        await Insert(ExecutionStatus.Running, Guid.NewGuid());

        using var repository = new BlastProbeJobRepository(_fixture.CreateDbContext());
        var listed = await repository.ListByUpstreamJob(upstreamJobId, _organizationId);

        Assert.Single(listed);
        Assert.Equal(mine, listed[0].Id);
    }

    [Fact]
    public async Task Repository_finalizes_with_the_signals_it_was_given()
    {
        var id = await Insert(ExecutionStatus.Running);

        var signals = """{"format_version":"1","outputs":{"pet_id":{"action":"unknown","type":"InN0cmluZyI=","before":"sGNoYW1waW9uLWhlcnJpbmc=","after":"1AAA"}}}""";

        using var repository = new BlastProbeJobRepository(_fixture.CreateDbContext());
        await repository.Finalize(id, _organizationId, ExecutionStatus.Completed, DateTimeOffset.UtcNow, signals);

        var row = await _dbContext.BlastProbeJobs.AsNoTracking()
            .SingleAsync(j => j.Id == id && j.OrganizationId == _organizationId);

        Assert.Equal(ExecutionStatus.Completed, row.Status);
        Assert.NotNull(row.TimestampEnd);
        Assert.Equal(signals, row.SignalsJson);
    }

    [Fact]
    public async Task Repository_records_a_server_side_failure()
    {
        var id = await Insert(ExecutionStatus.Running);

        using var repository = new BlastProbeJobRepository(_fixture.CreateDbContext());
        await repository.FinalizeWithServerError(
            id, _organizationId, DateTimeOffset.UtcNow, null, "header", "body");

        var row = await _dbContext.BlastProbeJobs.AsNoTracking()
            .SingleAsync(j => j.Id == id && j.OrganizationId == _organizationId);

        Assert.Equal(ExecutionStatus.Failed, row.Status);
        Assert.Equal("header", row.ServerSideErrorHeader);
        Assert.Equal("body", row.ServerSideError);
    }

    private async Task<Guid> Insert(ExecutionStatus status, Guid? upstreamModuleJobId = null)
    {
        var id = Guid.NewGuid();

        _dbContext.BlastProbeJobs.Add(new BlastProbeJob
        {
            Id = id,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            UpstreamModuleId = _upstreamModuleId,
            UpstreamModuleJobId = upstreamModuleJobId,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = status,
            InputsJson = "{}"
        });

        await _dbContext.SaveChangesAsync();
        _created.Add(id);

        return id;
    }

    private BlastProbeService CreateService(bool asOwner = true)
    {
        var principalId = asOwner
            ? _fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id
            : Guid.NewGuid();

        var principalProvider = _fixture.CreatePrincipalProvider(
            principalId, PrincipalDiscriminator.User, _organizationId);

        var securedRepository = new ModuleSecuredRepository(
            new ModuleRepository(
                _fixture.CreateDbContext(),
                principalProvider,
                _fixture.CreateMockBus(),
                _fixture.CreateModuleSettings()),
            principalProvider);

        return new BlastProbeService(DbContextFactory(), securedRepository);
    }

    private IDbContextFactory<SnapCdDbContext> DbContextFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
    }
}
