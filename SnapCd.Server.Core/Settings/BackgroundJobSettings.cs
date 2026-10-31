// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Whether a recurring background job runs, and how often. Every such job has these two, under its
/// own section of <c>BackgroundJobs</c>, so one cannot be added without both.
///
/// A job with more to configure than its schedule derives from this and adds its own.
/// </summary>
public abstract class BackgroundJobSettingsBase
{
    /// <summary>Whether the job is scheduled at all. Defaults to true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Cron expression for the job. Each job sets its own default.</summary>
    public abstract string Cron { get; set; }
}

/// <summary>
/// Every recurring job's schedule, one section each. Present so the whole group appears in the
/// settings schema and can be read at a glance; each job binds its own section standalone.
/// </summary>
public class BackgroundJobsSettings
{
    public SourceRefreshJobSettings SourceRefresh { get; set; } = new();
    public ServerConnectionCleanupJobSettings ServerConnectionCleanup { get; set; } = new();
    public SecretNameSweepJobSettings SecretNameSweep { get; set; } = new();
    public RunnerConnectionJobCleanupJobSettings RunnerConnectionJobCleanup { get; set; } = new();
    public InvitationCleanupJobSettings InvitationCleanup { get; set; } = new();
    public OrphanedJobCleanupJobSettings OrphanedJobCleanup { get; set; } = new();
    public StuckJobDetectionJobSettings StuckJobDetection { get; set; } = new();
    public LicenseRefreshJobSettings LicenseRefresh { get; set; } = new();
    public LicensePublicKeyRefreshJobSettings LicensePublicKeyRefresh { get; set; } = new();
    public TelemetryReportJobSettings TelemetryReport { get; set; } = new();
}

/// <summary>Refreshes Module sources so a changed revision is noticed without an explicit run.</summary>
public class SourceRefreshJobSettings : BackgroundJobSettingsBase
{
    public override string Cron { get; set; } = "*/5 * * * *"; // every 5 minutes
}

/// <summary>Clears connections left behind by a server that stopped without disconnecting.</summary>
public class ServerConnectionCleanupJobSettings : BackgroundJobSettingsBase
{
    public override string Cron { get; set; } = "*/1 * * * *"; // every minute
}

/// <summary>Moves Secret values onto the current naming scheme, a few at a time.</summary>
public class SecretNameSweepJobSettings : BackgroundJobSettingsBase
{
    public override string Cron { get; set; } = "0 3 * * *"; // daily at 03:00
}

/// <summary>Clears runner-to-job pinning rows whose job has finished.</summary>
public class RunnerConnectionJobCleanupJobSettings : BackgroundJobSettingsBase
{
    public override string Cron { get; set; } = "*/15 * * * *"; // every 15 minutes
}

/// <summary>Removes invitations that were never accepted.</summary>
public class InvitationCleanupJobSettings : BackgroundJobSettingsBase
{
    public override string Cron { get; set; } = "0 0 * * *"; // daily at midnight
}

/// <summary>Closes jobs whose saga is gone, so a Module is not held by one forever.</summary>
public class OrphanedJobCleanupJobSettings : BackgroundJobSettingsBase
{
    public override string Cron { get; set; } = "*/10 * * * *"; // every 10 minutes
}

/// <summary>Reports jobs that have waited too long at a step to be making progress.</summary>
public class StuckJobDetectionJobSettings : BackgroundJobSettingsBase
{
    public override string Cron { get; set; } = "*/5 * * * *"; // every 5 minutes
}

/// <summary>Re-reads the licence so a renewed one is picked up without a restart.</summary>
public class LicenseRefreshJobSettings : BackgroundJobSettingsBase
{
    public override string Cron { get; set; } = "0 3 * * *"; // daily at 03:00
}

/// <summary>Re-reads the key licences are verified against.</summary>
public class LicensePublicKeyRefreshJobSettings : BackgroundJobSettingsBase
{
    public override string Cron { get; set; } = "0 4 * * *"; // daily at 04:00
}

/// <summary>The daily usage beacon.</summary>
public class TelemetryReportJobSettings : BackgroundJobSettingsBase
{
    public override string Cron { get; set; } = "0 5 * * *"; // daily at 05:00
}
