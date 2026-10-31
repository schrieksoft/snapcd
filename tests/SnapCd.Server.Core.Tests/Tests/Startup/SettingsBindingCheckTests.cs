// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Startup;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Startup;

/// <summary>
/// A key that binds to nothing is dropped in silence, so the only sign of a typo or a rename left
/// behind is the setting quietly not applying. These assert it is reported instead.
/// </summary>
public class SettingsBindingCheckTests
{
    [Fact]
    public void A_Key_With_No_Property_Is_Reported()
    {
        var warnings = Report(new()
        {
            ["SetRetention:OutputSets:MaxKeptPerModule"] = "50",
            ["SetRetention:OutputSets:MaxKeptPerMdoule"] = "50"
        });

        Assert.Contains(warnings, w => w.Contains("SetRetention:OutputSets:MaxKeptPerMdoule"));
        Assert.DoesNotContain(warnings, w => w.Contains("MaxKeptPerModule\""));
    }

    [Fact]
    public void A_Key_That_Binds_Is_Not_Reported()
    {
        var warnings = Report(new()
        {
            ["SetRetention:OutputSets:CullAboveLimit"] = "true",
            ["SetRetention:OutputSets:MaxKeptPerModule"] = "20",
            ["SetRetention:ProbeWaves:CullAboveLimit"] = "false"
        });

        Assert.Empty(warnings);
    }

    /// <summary>
    /// A section at the top of a bound type, misspelled, is the case that costs most: every key
    /// under it is ignored at once.
    /// </summary>
    [Fact]
    public void A_Misspelled_Subsection_Is_Reported_Once()
    {
        var warnings = Report(new()
        {
            ["SetRetention:OutputSet:MaxKeptPerModule"] = "50"
        });

        Assert.Single(warnings);
        Assert.Contains("SetRetention:OutputSet", warnings[0]);
    }

    /// <summary>
    /// A section nothing binds any more cannot be found by walking a type, because there is no
    /// type. It is named instead, with what replaced it.
    /// </summary>
    [Fact]
    public void A_Retired_Key_Is_Reported_With_Its_Replacement()
    {
        var warnings = Report(new()
        {
            ["SourceRefresh:RefreshIntervalCronExpression"] = "*/9 * * * *"
        });

        Assert.Single(warnings);
        Assert.Contains("SourceRefresh:RefreshIntervalCronExpression", warnings[0]);
        Assert.Contains("BackgroundJobs:SourceRefresh:Cron", warnings[0]);
    }

    [Fact]
    public void Nothing_Configured_Reports_Nothing()
    {
        Assert.Empty(Report(new()));
    }


    /// <summary>
    /// The shape every deployment still ships: the source refresh schedule under the key the
    /// server stopped reading when the job settings moved. An operator who set it keeps a config
    /// that looks right and a job that no longer honours it.
    /// </summary>
    [Fact]
    public void The_Schedule_Every_Deployment_Still_Sets_Is_Reported()
    {
        var warnings = Report(new()
        {
            ["SourceRefresh:RefreshIntervalCronExpression"] = "*/9 * * * *",
            ["SourceRefresh:TimeoutSeconds"] = "30",
            ["SecretStore:EnableMigrator"] = "true"
        });

        Assert.Equal(3, warnings.Count);
        Assert.Contains(warnings, w => w.Contains("BackgroundJobs:SourceRefresh:Cron"));
    }

    /// <summary>
    /// A retired key reaches no property either, so both halves of the check see it. Only the
    /// message naming its replacement is worth printing.
    /// </summary>
    [Fact]
    public void A_Retired_Key_Is_Reported_Once_Not_Twice()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SourceRefresh:RefreshIntervalCronExpression"] = "*/9 * * * *"
            })
            .Build();

        var logger = new CollectingLogger();

        SettingsBindingCheck.Report(
            configuration, logger, ("SourceRefresh", typeof(SourceRefreshSettings)));

        Assert.Single(logger.Warnings);
        Assert.Contains("BackgroundJobs:SourceRefresh:Cron", logger.Warnings[0]);
    }

    private static List<string> Report(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var logger = new CollectingLogger();

        SettingsBindingCheck.Report(
            configuration, logger, ("SetRetention", typeof(SetRetentionSettings)));

        return logger.Warnings;
    }

    private sealed class CollectingLogger : ILogger
    {
        public readonly List<string> Warnings = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) Warnings.Add(formatter(state, exception));
        }
    }
}
