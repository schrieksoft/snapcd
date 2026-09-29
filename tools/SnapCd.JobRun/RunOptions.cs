// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.JobRun;

/// <summary>The job kinds the run knows how to start.</summary>
public enum JobKind
{
    Apply,
    Destroy,
    List,
    Move,
    Import,
    Remove,
    Split,
    Transfer
}

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

    /// <summary>The seeder's second Module, which a transfer needs as the counterparty.</summary>
    public static readonly Guid SeededCounterpartyModuleId =
        new("99999999-9999-9999-9999-999999999911");

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
    public JobKind Job { get; init; } = JobKind.Apply;

    /// <summary>The addresses a job names. Every kind here refuses an empty set.</summary>
    public IReadOnlyCollection<string> Addresses { get; init; } = ["null_resource.example"];

    /// <summary>
    /// What each address becomes. A move needs one and an import needs the resource's id; a
    /// remove takes none.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>A split's root within the checkout, and whether it proceeds over its own doubts.</summary>
    public string? RootDirectory { get; init; }

    public bool Force { get; init; }

    public Guid CounterpartyModuleId { get; init; } = SeededCounterpartyModuleId;

    /// <summary>The refs each side runs against, which carry the code move.</summary>
    public string? ModuleRef { get; init; }

    public string? CounterpartyRef { get; init; }

    /// <summary>
    /// The outputs the consuming side's map says it needs, and which the producing side
    /// publishes. The sample's app root reads the networking root's random_pet_dns_zone.
    /// </summary>
    public IReadOnlyCollection<string> NeedsOutputs { get; init; } = ["random_pet_dns_zone"];

    /// <summary>
    /// Which transport to run on. The two route differently: a runner-facing consumer's endpoint
    /// carries no topic subscription, so a published step reaches it on SQL Server and is dropped
    /// on Azure Service Bus. A green run on one says nothing about the other.
    /// </summary>
    public string BusType { get; init; } = "SqlServer";

    /// <summary>The Azure Service Bus namespace, when running on that transport.</summary>
    public string? ServiceBusConnectionString { get; init; }

    public bool Keep { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Seconds to wait after every endpoint reports ready, before publishing.</summary>
    public int SettleSeconds { get; init; }

    /// <summary>How many times to ask again when no job row appears.</summary>
    public int RetryRequests { get; init; }

    /// <summary>Sends a throwaway message before the real one.</summary>
    public bool WarmUpSend { get; init; }

    /// <summary>
    /// MassTransit's own logging at Debug, which shows queue and topic creation, the
    /// subscriptions binding them, and which saga consumed a message. On by default: it is what
    /// distinguishes a message nothing consumed from one that was never routed anywhere.
    /// </summary>
    public bool TransportTrace { get; init; }

    /// <summary>
    /// The server's own settings, layered under the run's overrides. Settings such as the state
    /// store's encryption key have to match the server's or existing rows will not decrypt.
    /// </summary>
    public required string ServerAppSettingsPath { get; init; }

    /// <summary>A second settings file layered over the first, for the Azure namespace.</summary>
    public string? ServerAppSettingsOverlayPath { get; init; }

    public string ConnectionString =>
        $"{MasterConnectionString.TrimEnd(';')};Database={DatabaseName}";

    /// <summary>The bus shares the run's database, so the run needs no broker of its own.</summary>
    public Dictionary<string, string?> Configuration
    {
        get
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionString"] = ConnectionString,
                ["Server:InstanceId"] = ServerInstanceId.ToString(),
                ["ServiceBus:BusType"] = BusType,
                ["ServiceBus:TransportOptions:SqlServer:ConnectionString"] = ConnectionString,

                // Selects DebugDataSeeder, which creates the organization, user, runner and Module
                // the run uses. Without it the seeder makes a bare production tenant with no Module.
                ["UseDebugDataSeeder"] = "true",

                // DebugDataSeeder builds on the preseed rather than repeating it: its Module
                // references the preseeded runner and its missions the preseeded agent, so without
                // this it fails on a foreign key.
                ["ProductionDataSeeder:Preseeded:Enabled"] = "true"
            };

            // Left unset, the namespace comes from the server's own appsettings, so a run on Azure
            // needs no connection string of its own.
            if (ServiceBusConnectionString is not null)
                settings["ServiceBus:TransportOptions:AzureServiceBus:ConnectionString"] =
                    ServiceBusConnectionString;

            return settings;
        }
    }

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
                  --job           <kind>    apply (default), list, move, import, remove,
                                            split or transfer
                  --counterparty  <guid>    the transfer's other Module
                  --needs         <a,b>     the outputs the consuming side waits for
                  --addresses     <a,b,c>   the addresses the job names
                  --target        <string>  what each address becomes, for a move or an import
                  --root          <path>    a split's root within the checkout
                  --force                   let a split proceed over its own doubts
                  --module        <guid>    default: the seeder's mock Module
                  --principal     <guid>    default: the seeder's debug user
                  --appsettings   <path>    the server's appsettings.json
                  --bus           <kind>    SqlServer (default) or AzureServiceBus
                  --servicebus    <string>  the Azure Service Bus namespace, for --bus
                                            AzureServiceBus
                  --timeout       <seconds> default 300
                  --keep                    leave the database behind to inspect
                  --quiet-transport         drop MassTransit to Information (trace is on by default)
                """);
            return null;
        }

        return new RunOptions
        {
            MasterConnectionString = server,
            DatabaseName = Get("database") ?? $"SnapCdJobRun_{DateTime.Now:yyyyMMdd_HHmmss}",
            ServerAppSettingsPath = Get("appsettings") ?? DefaultAppSettingsPath(),
            ServerAppSettingsOverlayPath = Get("appsettings-overlay"),
            Job = Enum.TryParse<JobKind>(Get("job"), ignoreCase: true, out var kind) ? kind : JobKind.Apply,
            Target = Get("target"),
            RootDirectory = Get("root"),
            BusType = Get("bus") ?? "SqlServer",
            ServiceBusConnectionString = Get("servicebus"),
            CounterpartyModuleId = Guid.TryParse(Get("counterparty"), out var c)
                ? c
                : SeededCounterpartyModuleId,
            ModuleRef = Get("ref"),
            CounterpartyRef = Get("counterparty-ref"),
            NeedsOutputs = Get("needs")?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(o => o.Trim()).ToList()
                           ?? ["random_pet_dns_zone"],
            Force = args.Contains("--force"),
            Addresses = Get("addresses")?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(a => a.Trim()).ToList()
                        ?? ["null_resource.example"],
            ModuleId = Guid.TryParse(Get("module"), out var m) ? m : SeededModuleId,
            PrincipalId = Guid.TryParse(Get("principal"), out var p) ? p : SeededUserId,
            RunnerId = Guid.TryParse(Get("runner"), out var r) ? r : Guid.Empty,
            Keep = args.Contains("--keep"),
            TransportTrace = !args.Contains("--quiet-transport"),
            Timeout = ParseTimeout(Get("timeout")),
            SettleSeconds = int.TryParse(Get("settle"), out var st) ? st : 0,
            RetryRequests = int.TryParse(Get("retry-requests"), out var rr) ? rr : 0,
            WarmUpSend = args.Contains("--warm-up"),
        };
    }

    /// <summary>
    /// Seconds, or a duration such as 13:00. A value the flag cannot read is refused rather than
    /// defaulted: a run that silently stops early reads as the job stalling.
    /// </summary>
    private static TimeSpan ParseTimeout(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return TimeSpan.FromMinutes(5);

        if (int.TryParse(value, out var seconds)) return TimeSpan.FromSeconds(seconds);

        if (TimeSpan.TryParse(value, out var span)) return span;

        throw new ArgumentException(
            $"--timeout could not read '{value}'. Give seconds (900) or a duration (15:00).");
    }
}
