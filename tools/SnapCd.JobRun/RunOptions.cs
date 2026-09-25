// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.JobRun;

/// <summary>
/// What the run needs to know, and the arguments that supply it. The defaults are the ids the
/// debug seeder creates, so a run needs no arguments beyond where the database lives.
/// </summary>
public class RunOptions
{
    /// <summary>The preseeded organization, from ProductionDataSeederSettings.DefaultId.</summary>
    public static readonly Guid SeededOrganizationId = new("10000000-0000-0000-0000-000000000000");

    /// <summary>The preseeded agent, from PreseededSettings.DefaultAgentId.</summary>
    public static readonly Guid SeededAgentId = new("20000000-0000-0000-0000-000000000000");

    /// <summary>The debug seeder's own user, runner pool and mock Module.</summary>
    public static readonly Guid SeededUserId = new("99999999-9999-9999-9999-999999999990");

    public static readonly Guid SeededModuleId = new("99999999-9999-9999-9999-999999999910");

    public required string MasterConnectionString { get; init; }
    public required string DatabaseName { get; init; }
    public Guid OrganizationId { get; init; } = SeededOrganizationId;
    public Guid ModuleId { get; init; } = SeededModuleId;
    public Guid PrincipalId { get; init; } = SeededUserId;
    public Guid RunnerId { get; init; }

    /// <summary>Runner connections are only dispatchable from the server instance they name.</summary>
    public Guid ServerInstanceId { get; } = Guid.NewGuid();

    public string ConnectionId { get; init; } = $"jobrun-{Guid.NewGuid():N}";
    public string RunnerInstanceName { get; init; } = "jobrun";
    public bool Verbose { get; init; }
    public bool Keep { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The server's own settings, layered under the run's overrides. Settings such as the state
    /// store's encryption key have to match the server's or existing rows will not decrypt.
    /// </summary>
    public required string ServerAppSettingsPath { get; init; }

    public string ConnectionString =>
        $"{MasterConnectionString.TrimEnd(';')};Database={DatabaseName}";

    /// <summary>The bus shares the run's database, so the run needs no broker of its own.</summary>
    public Dictionary<string, string?> Configuration => new()
    {
        ["ConnectionString"] = ConnectionString,
        ["Server:InstanceId"] = ServerInstanceId.ToString(),
        ["ServiceBus:BusType"] = "SqlServer",
        ["ServiceBus:TransportOptions:SqlServer:ConnectionString"] = ConnectionString,

        // Selects DebugDataSeeder, which creates the organization, user, runner and Module the
        // run uses. Without it the seeder makes a bare production tenant with no Module.
        ["UseDebugDataSeeder"] = "true",

        // DebugDataSeeder builds on the preseed rather than repeating it: its Module references
        // the preseeded runner and its missions the preseeded agent, so without this it fails on
        // a foreign key.
        ["ProductionDataSeeder:Preseeded:Enabled"] = "true",

    };

    /// <summary>Walks up to the repo root, so the run works from any working directory.</summary>
    private static string DefaultAppSettingsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "SnapCd.Server.Host", "appsettings.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        return Path.Combine("SnapCd.Server.Host", "appsettings.json");
    }

    public static RunOptions? Parse(string[] args)
    {
        var map = new Dictionary<string, string>();
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i].StartsWith("--"))
                map[args[i][2..]] = args[i + 1];

        string? Get(string name) => map.TryGetValue(name, out var v) ? v : null;

        var server = Get("server");

        if (server is null)
        {
            Console.Error.WriteLine(
                """
                Runs an Apply against a Module, answering the runner in-process, and watches the
                job until it reaches a terminal state.

                The run creates its own database, migrates it, seeds it and drops it again, so it
                touches nothing a server or another run is using.

                  --server        <string>  the SQL Server to create the database on, without a
                                            Database= entry, e.g.
                                            "Server=localhost,1435;User Id=sa;Password=...;TrustServerCertificate=True"
                  --database      <name>    default: a fresh name per run
                  --module        <guid>    default: the seeder's mock Module
                  --principal     <guid>    default: the seeder's debug user
                  --appsettings   <path>    the server's appsettings.json
                  --timeout       <seconds> default 300
                  --keep                    leave the database behind to inspect
                  --verbose                 show dispatches and server logs
                """);
            return null;
        }

        return new RunOptions
        {
            MasterConnectionString = server,
            DatabaseName = Get("database") ?? $"SnapCdJobRun_{DateTime.Now:yyyyMMdd_HHmmss}",
            ServerAppSettingsPath = Get("appsettings") ?? DefaultAppSettingsPath(),
            ModuleId = Guid.TryParse(Get("module"), out var m) ? m : SeededModuleId,
            PrincipalId = Guid.TryParse(Get("principal"), out var p) ? p : SeededUserId,
            RunnerId = Guid.TryParse(Get("runner"), out var r) ? r : Guid.Empty,
            Keep = args.Contains("--keep"),
            Verbose = args.Contains("--verbose"),
            Timeout = int.TryParse(Get("timeout"), out var t)
                ? TimeSpan.FromSeconds(t)
                : TimeSpan.FromMinutes(5),
        };
    }
}
