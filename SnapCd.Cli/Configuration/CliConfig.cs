// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapCd.Cli.Configuration;

/// <summary>A Server and the organization the CLI works in there.</summary>
public class CliContext
{
    public required string Name { get; set; }
    public required string Url { get; set; }
    public bool InsecureSkipVerify { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string? UserEmail { get; set; }
}

/// <summary>The contexts and the current one. Holds nothing secret; tokens are in <see cref="Auth.TokenStore"/>.</summary>
public class CliConfig
{
    public string? CurrentContext { get; set; }
    public List<CliContext> Contexts { get; set; } = [];

    public CliContext? Find(string name) =>
        Contexts.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal));

    /// <summary>The named context, or the current one when no name is given.</summary>
    public CliContext Resolve(string? name)
    {
        var target = name ?? CurrentContext
            ?? throw new CliException("No context is selected. Add one with: snapcd context add <name> --url <server>");
        return Find(target) ?? throw new CliException($"No context named '{target}'. See: snapcd context list");
    }
}

public class CliConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CliConfigStore(string directory)
    {
        Directory = directory;
    }

    public string Directory { get; }

    private string ConfigPath => Path.Combine(Directory, "config.json");

    /// <summary>
    /// <c>SNAPCD_CLI_HOME</c> overrides the directory. The default is a subdirectory, because
    /// <c>~/.snapcd/config.json</c> belongs to other tooling.
    /// </summary>
    public static CliConfigStore Default()
    {
        var directory = Environment.GetEnvironmentVariable("SNAPCD_CLI_HOME");
        if (string.IsNullOrEmpty(directory))
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".snapcd", "cli");
        return new CliConfigStore(directory);
    }

    public CliConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new CliConfig();
        return JsonSerializer.Deserialize<CliConfig>(File.ReadAllText(ConfigPath), JsonOptions) ?? new CliConfig();
    }

    public void Save(CliConfig config)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var temp = ConfigPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(temp, ConfigPath, overwrite: true);
    }
}

/// <summary>An error the user can act on. Printed without a stack trace.</summary>
public class CliException(string message) : Exception(message);
