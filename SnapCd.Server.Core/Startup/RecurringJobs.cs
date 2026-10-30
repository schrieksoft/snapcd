// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Linq.Expressions;
using Hangfire;
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
