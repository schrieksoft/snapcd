// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Linq.Expressions;
using Hangfire;
using Microsoft.Extensions.Configuration;
using SnapCd.Server.Core.Services.Nonsecured;
using SnapCd.Server.Core.Services.Nonsecured.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Schedules a recurring job, honouring any override configured against its name.
///
/// Every job is registered through here so that each one can be turned off and rescheduled from
/// configuration. Registering with Hangfire directly works and skips both, which is why the jobs
/// that used to do so no longer do.
/// </summary>
public static class RecurringJobs
{
    /// <summary>
    /// The jobs every host runs, which are the ones whose work lives in Core. A host adds its own
    /// afterwards; these are the same wherever the server runs, so neither host declares them.
    /// </summary>
    public static void RegisterCoreJobs(IConfiguration configuration, ILogger? logger = null)
    {
        Register<SourceRefreshJob>(Settings<SourceRefreshJobSettings>(configuration, "SourceRefresh"),
            "source-refresh-job", x => x.ExecuteJob(), logger);

        Register<ServerConnectionCleanupJob>(Settings<ServerConnectionCleanupJobSettings>(configuration, "ServerConnectionCleanup"),
            "server-connection-cleanup-job", x => x.ExecuteJob(), logger);

        Register<RunnerConnectionJobCleanupJob>(Settings<RunnerConnectionJobCleanupJobSettings>(configuration, "RunnerConnectionJobCleanup"),
            "runner-connection-job-cleanup-job", x => x.ExecuteJob(), logger);

        Register<InvitationCleanupJob>(Settings<InvitationCleanupJobSettings>(configuration, "InvitationCleanup"),
            "invitation-cleanup-job", x => x.ExecuteJob(), logger);

        Register<OrphanedJobCleanupJob>(Settings<OrphanedJobCleanupJobSettings>(configuration, "OrphanedJobCleanup"),
            "orphaned-job-cleanup-job", x => x.ExecuteJob(), logger);

        Register<StuckJobDetectionJob>(Settings<StuckJobDetectionJobSettings>(configuration, "StuckJobDetection"),
            "stuck-job-detection-job", x => x.ExecuteJob(), logger);

        Register<SecretNameSweepJob>(Settings<SecretNameSweepJobSettings>(configuration, "SecretNameSweep"),
            "secret-name-sweep-job", x => x.ExecuteJob(), logger);

        // Scheduled messages live in the transport and do not survive a transport change.
        BackgroundJob.Enqueue<TransportReconciliationJob>(x => x.ExecuteJob());
    }

    /// <summary>One job's settings, read from its own section under BackgroundJobs.</summary>
    public static T Settings<T>(IConfiguration configuration, string name)
        where T : BackgroundJobSettingsBase, new() =>
        configuration.GetSection($"BackgroundJobs:{name}").Get<T>() ?? new T();

    public static void Register<TJob>(
        BackgroundJobSettingsBase settings,
        string jobName,
        Expression<Func<TJob, Task>> method,
        ILogger? logger = null)
    {
        if (!settings.Enabled)
        {
            // Removed rather than left behind: an earlier run of the server may have registered it,
            // and Hangfire keeps a recurring job until something takes it away.
            RecurringJob.RemoveIfExists(jobName);
            logger?.LogInformation("Background job {JobName} is disabled by configuration", jobName);
            return;
        }

        RecurringJob.AddOrUpdate(jobName, method, settings.Cron);
    }
}
