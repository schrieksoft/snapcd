// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Runner.Hub;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Logging;

/// <summary>
/// The streams a runner ships logs through, one per table. Each is bound to the endpoint that
/// stores into its table, so the table is named once here rather than at every emit site.
/// </summary>
public sealed class JobLogStreams : IJobLogStreams, IAsyncDisposable
{
    private readonly HubJobLogStream<ModuleJobLogEntryDto> _moduleJobs;
    private readonly HubJobLogStream<StateMigrationLogEntryDto> _stateMigrations;

    public JobLogStreams(
        RunnerHubConnection hub,
        IOptions<JobLogStreamSettings> settings,
        ILoggerFactory loggerFactory)
    {
        _moduleJobs = new HubJobLogStream<ModuleJobLogEntryDto>(
            hub, ServerEndpoints.AddModuleJobLogs, settings.Value,
            loggerFactory.CreateLogger<HubJobLogStream<ModuleJobLogEntryDto>>());

        _stateMigrations = new HubJobLogStream<StateMigrationLogEntryDto>(
            hub, ServerEndpoints.AddStateMigrationLogs, settings.Value,
            loggerFactory.CreateLogger<HubJobLogStream<StateMigrationLogEntryDto>>());
    }

    public IJobLogStream ModuleJobs => _moduleJobs;

    public IJobLogStream StateMigrations => _stateMigrations;

    public async ValueTask DisposeAsync()
    {
        await _moduleJobs.DisposeAsync();
        await _stateMigrations.DisposeAsync();
    }
}
