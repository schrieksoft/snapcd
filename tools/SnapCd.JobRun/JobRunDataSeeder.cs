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
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.Nonsecured.DataSeeder;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.DataSeeder;
using SnapCd.Server.Core.StateMachine.Gatekeeping;

namespace SnapCd.JobRun;

/// <summary>
/// The debug seeder plus the graphs only the job harness runs against.
///
/// Fixture data for a harness run does not belong in the debug seeder, which also serves developer
/// workstations: a graph nobody is exercising is Modules counting against the organization's cap
/// for no return. This subclass adds them, and the harness registers it in place of the debug
/// seeder for the runs that need them.
/// </summary>
public class JobRunDataSeeder : DebugDataSeeder
{
    private readonly SnapCdDbContext _dbContext;

    /// <summary>
    /// A fanout: a feeds b, c and d, and every consumer answers. Where the diamond exercises how
    /// far a wave travels, this exercises what the approval panel says about each participant once
    /// it arrives, so the three consumers answer the three ways a probe can report.
    /// </summary>
    private static readonly Guid FanoutAModuleId = RunOptions.FanoutAModuleId;
    private static readonly Guid FanoutBModuleId = RunOptions.FanoutBModuleId;
    private static readonly Guid FanoutCModuleId = RunOptions.FanoutCModuleId;
    private static readonly Guid FanoutDModuleId = RunOptions.FanoutDModuleId;

    public JobRunDataSeeder(
        IServiceProvider serviceProvider,
        SnapCdDbContext dbContext,
        IOptions<ProductionDataSeederSettings> productionOptions,
        IOptions<ServerSettings> serverOptions,
        IOptions<DebugDataSeederSettings> debugOptions)
        : base(serviceProvider, dbContext, productionOptions, serverOptions, debugOptions)
    {
        _dbContext = dbContext;
    }

    public override async Task SeedAsync()
    {
        await base.SeedAsync();
        await SeedProbeFanout(_dbContext);
    }

    /// <summary>
    /// Four Modules wired as a fanout: a's plan changes an output and b, c and d all read it.
    ///
    /// b changes resources without moving an output, c is unaffected and carries its outputs
    /// unchanged, and d answers with nothing at all.
    /// </summary>
    private async Task SeedProbeFanout(SnapCdDbContext dbContext)
    {
        const string fixtureUrl = "https://github.com/schrieksoft/monorepo-testing.git";

        var names = new Dictionary<Guid, string>
        {
            [FanoutAModuleId] = "probe-fanout-a",
            [FanoutBModuleId] = "probe-fanout-b",
            [FanoutCModuleId] = "probe-fanout-c",
            [FanoutDModuleId] = "probe-fanout-d"
        };

        var existing = await dbContext.Modules
            .Where(m => names.Keys.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync();

        foreach (var (moduleId, name) in names)
        {
            if (existing.Contains(moduleId)) continue;

            var module = new Module
            {
                Id = moduleId,
                OrganizationId = PreseededOrganizationId,
                NamespaceId = DebugEntityId,
                RunnerId = DebugEntityId,
                Name = name,
                SourceUrl = fixtureUrl,
                SourceRevision = "main",
                SourceSubdirectory = $"modules/{name}",
                SourceType = SourceType.Git,
                SourceRevisionType = SourceRevisionType.Default,
                // The origin needs a threshold: a wave informs an approval decision, so an
                // auto-approved apply creates none.
                ApplyApprovalThreshold = moduleId == FanoutAModuleId ? 1 : null,
                DestroyApprovalThreshold = moduleId == FanoutAModuleId ? 1 : null,
                WaitForDestroyDependencies = moduleId == FanoutAModuleId
                    ? WaitForDestroyDependencies.Never
                    : WaitForDestroyDependencies.Always,
                CreatedDateTime = DateTime.UtcNow
            };

            module.ModuleSaga = new ModuleSaga
            {
                CorrelationId = moduleId,
                OrganizationId = PreseededOrganizationId,
                RowVersion = [],
                CurrentState = nameof(ModuleStateMachine.Gatekeeping),
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                QueuedDesiredStateHeadline = null
            };

            // The apply that left the output set below. A consumer is held until its upstream has
            // applied, so without this every Module reads as never applied and nothing propagates.
            dbContext.ModuleJobs.Add(new ModuleJob
            {
                Id = Guid.NewGuid(),
                OrganizationId = PreseededOrganizationId,
                ModuleId = moduleId,
                JobType = nameof(ApplyJobSaga),
                Status = ExecutionStatus.Completed,
                ActualStateHeadline = ActualStateHeadline.Applied,
                IsCurrent = null,
                TimestampStart = DateTimeOffset.UtcNow.AddMinutes(-5),
                TimestampEnd = DateTimeOffset.UtcNow.AddMinutes(-5),
                CreatedDateTime = DateTime.UtcNow
            });

            dbContext.Modules.Add(module);
        }

        // All three consumers read the one output of a that moves, so all three are probed.
        await AddSeededEdge(dbContext, FanoutBModuleId, FanoutAModuleId, "a_id", "from_a");
        await AddSeededEdge(dbContext, FanoutCModuleId, FanoutAModuleId, "a_id", "from_a");
        await AddSeededEdge(dbContext, FanoutDModuleId, FanoutAModuleId, "a_id", "from_a");

        await AddSeededOutputs(dbContext, FanoutAModuleId,
            ("a_id", "settled-marmot"));

        await AddSeededOutputs(dbContext, FanoutBModuleId,
            ("b_id", "settled-marmot-plain-asp"));

        // c's three outputs are the ones its probe reports back unchanged.
        await AddSeededOutputs(dbContext, FanoutCModuleId,
            ("c_ip", "100.0.8.255"),
            ("c_fqdn", "fanout-c.example.com"),
            ("c_id", "settled-marmot-sunny-crab"));

        await AddSeededOutputs(dbContext, FanoutDModuleId,
            ("d_id", "settled-marmot-brave-moth"));
    }
}
