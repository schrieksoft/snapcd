// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Misc.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddExternalConfiguration(
        this IConfigurationBuilder builder)
    {
        return File.Exists("externalsettings.json")
            ? builder.Add(new ExternalConfigurationSource("externalsettings.json"))
            : builder;
    }

    /// <summary>
    /// Adds predefined configuration values that are generated at startup.
    /// Currently sets Server:InstanceId to a unique GUID for this server instance.
    /// </summary>
    public static IConfigurationBuilder AddPredefined(
        this IConfigurationBuilder builder)
    {
        var predefinedValues = new Dictionary<string, string?>
        {
            ["Server:InstanceId"] = Guid.NewGuid().ToString()
        };

        return builder.AddInMemoryCollection(predefinedValues);
    }
}