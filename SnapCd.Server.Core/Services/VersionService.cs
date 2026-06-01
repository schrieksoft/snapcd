// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;

namespace SnapCd.Server.Core.Services;

public interface IVersionService
{
    string Version { get; }
    string ShortVersion { get; }
}

public class VersionService : IVersionService
{
    private readonly string _version;

    public VersionService()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        _version = versionAttribute?.InformationalVersion ?? "0.0.0";
    }

    public string Version => _version;

    // Returns version without build metadata (e.g., "1.2.3" instead of "1.2.3+sha.abc123")
    public string ShortVersion
    {
        get
        {
            var plusIndex = _version.IndexOf('+');
            return plusIndex > 0 ? _version.Substring(0, plusIndex) : _version;
        }
    }
}