// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Newtonsoft.Json;

namespace SnapCd.Runner.Configuration;

public class ExternalConfigurationSource : IConfigurationSource
{
    public List<ExternalProvider> Providers { get; init; }


    public ExternalConfigurationSource(string settingsFilePath)
    {
        // Read the JSON file
        var jsonString = File.ReadAllText(settingsFilePath);
        var settings = JsonConvert.DeserializeObject<SettingsFile>(jsonString);

        if (settings == null)
            throw new InvalidOperationException($"Failed to deserialize configuration file '{settingsFilePath}'. The file may be empty or contain invalid JSON.");

        Providers = settings.Providers;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ExternalConfigurationProvider(this);
    }
}

public class SettingsFile
{
    public required List<ExternalProvider> Providers { get; set; }
}
