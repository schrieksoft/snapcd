// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using SnapCd.Server.Core.Settings;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Startup;

/// <summary>
/// Every recurring job says whether it runs and how often, in its own section under
/// BackgroundJobs. The defaults are the schedules the server kept before the settings existed, so
/// an untouched configuration must change nothing.
/// </summary>
public class BackgroundJobSettingsTests
{
    [Fact]
    public void A_Job_Runs_By_Default()
    {
        Assert.True(new SourceRefreshJobSettings().Enabled);
    }

    [Fact]
    public void A_Job_Can_Be_Disabled()
    {
        var settings = Bind<TelemetryReportJobSettings>("TelemetryReport", new()
        {
            ["BackgroundJobs:TelemetryReport:Enabled"] = "false"
        });

        Assert.False(settings.Enabled);
    }

    [Fact]
    public void A_Schedule_Can_Be_Overridden()
    {
        var settings = Bind<SourceRefreshJobSettings>("SourceRefresh", new()
        {
            ["BackgroundJobs:SourceRefresh:Cron"] = "*/1 * * * *"
        });

        Assert.Equal("*/1 * * * *", settings.Cron);
        Assert.True(settings.Enabled);
    }

    [Fact]
    public void An_Untouched_Section_Keeps_Every_Default()
    {
        var settings = Bind<SourceRefreshJobSettings>("SourceRefresh", new());
        var defaults = new SourceRefreshJobSettings();

        Assert.Equal(defaults.Cron, settings.Cron);
        Assert.Equal(defaults.Enabled, settings.Enabled);
    }

    /// <summary>
    /// The defaults are the schedules the server ran before these settings existed, so an
    /// untouched configuration has to leave every job exactly where it was.
    /// </summary>
    [Theory]
    [InlineData("*/5 * * * *", typeof(SourceRefreshJobSettings))]
    [InlineData("*/1 * * * *", typeof(ServerConnectionCleanupJobSettings))]
    [InlineData("*/15 * * * *", typeof(RunnerConnectionJobCleanupJobSettings))]
    [InlineData("0 0 * * *", typeof(InvitationCleanupJobSettings))]
    [InlineData("*/10 * * * *", typeof(OrphanedJobCleanupJobSettings))]
    [InlineData("*/5 * * * *", typeof(StuckJobDetectionJobSettings))]
    [InlineData("0 3 * * *", typeof(LicenseRefreshJobSettings))]
    [InlineData("0 4 * * *", typeof(LicensePublicKeyRefreshJobSettings))]
    [InlineData("0 5 * * *", typeof(TelemetryReportJobSettings))]
    [InlineData("0 3 * * *", typeof(SecretNameSweepJobSettings))]
    public void A_Jobs_Default_Schedule_Is_What_It_Always_Was(string expected, Type settingsType)
    {
        var settings = (BackgroundJobSettingsBase)Activator.CreateInstance(settingsType)!;

        Assert.Equal(expected, settings.Cron);
        Assert.True(settings.Enabled);
    }

    /// <summary>
    /// Every job's settings derive from the same base, so neither knob can be forgotten when one
    /// is added. A job whose settings do not is a job that cannot be turned off.
    /// </summary>
    [Fact]
    public void Every_Job_Settings_Class_Carries_Both_Knobs()
    {
        var jobSettings = typeof(BackgroundJobSettingsBase).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("JobSettings", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(jobSettings);
        Assert.All(jobSettings, t =>
            Assert.True(typeof(BackgroundJobSettingsBase).IsAssignableFrom(t),
                $"{t.Name} does not derive from {nameof(BackgroundJobSettingsBase)}"));
    }

    /// <summary>
    /// Registering with Hangfire directly works and silently skips both knobs, so a job added that
    /// way cannot be turned off or rescheduled. Everything goes through RecurringJobs.Register.
    /// </summary>
    [Fact]
    public void No_Job_Is_Registered_Straight_With_Hangfire()
    {
        var offenders = File.ReadAllLines(FindProgramCs())
            .Select((line, index) => (line, number: index + 1))
            .Where(x => x.line.Contains("RecurringJob.AddOrUpdate", StringComparison.Ordinal))
            .Select(x => $"Program.cs:{x.number}")
            .ToList();

        Assert.Empty(offenders);
    }

    /// <summary>
    /// appsettings.json writes every job out so the schedules can be read without reading the
    /// code. That duplicates the defaults, so the two have to be checked against each other: a
    /// job added to one and not the other is the failure this prevents.
    /// </summary>
    [Fact]
    public void Appsettings_Lists_Every_Job()
    {
        var configured = Appsettings().GetSection("BackgroundJobs").GetChildren()
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var jobs = typeof(BackgroundJobSettingsBase).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(BackgroundJobSettingsBase).IsAssignableFrom(t))
            .Select(t => t.Name.Replace("JobSettings", string.Empty, StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(jobs);
        Assert.All(jobs, job =>
            Assert.True(configured.Contains(job), $"appsettings.json has no BackgroundJobs:{job} section"));
    }

    private static IConfigurationRoot Appsettings()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "SnapCd.Server.Host", "appsettings.json");
            if (File.Exists(candidate))
                return new ConfigurationBuilder().AddJsonFile(candidate).Build();
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find SnapCd.Server.Host/appsettings.json.");
    }

    private static string FindProgramCs()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "SnapCd.Server.Host", "Program.cs");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find SnapCd.Server.Host/Program.cs from the test output directory.");
    }

    private static T Bind<T>(string section, Dictionary<string, string?> values) where T : new()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return configuration.GetSection($"BackgroundJobs:{section}").Get<T>() ?? new T();
    }
}
