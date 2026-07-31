// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Nodes;
using SnapCd.Utils.Settings;

namespace SnapCd.Generators;

// Emits schemas/{runner,agent,server}.schema.json in one process. Types from the three component
// assemblies are fully qualified throughout — Runner, Agent and Server each declare their own
// ServerSettings, so unqualified names would be ambiguous here.
internal static class SettingsSchemasCommand
{
    public static int Run(string snapcdRoot)
    {
        var rc = RunRunner(snapcdRoot);
        rc = Math.Max(rc, RunAgent(snapcdRoot));
        rc = Math.Max(rc, RunServer(snapcdRoot));
        return rc;
    }

    // Section-name → POCO type map. Keys must match the section names passed to Configure<T> in
    // SnapCd.Runner/Program.cs — those are the strings operators write into appsettings.json.
    private static int RunRunner(string snapcdRoot)
    {
        var sectionTypes = new Dictionary<string, Type>
        {
            ["Server"] = typeof(SnapCd.Runner.Settings.ServerSettings),
            ["WorkingDirectory"] = typeof(SnapCd.Runner.Settings.WorkingDirectorySettings),
            ["Runner"] = typeof(SnapCd.Runner.Settings.RunnerSettings),
            ["HooksPreapproval"] = typeof(SnapCd.Runner.Settings.HooksPreapprovalSettings),
            ["Engine"] = typeof(SnapCd.Runner.Settings.EngineSettings),
            ["JobLogStream"] = typeof(SnapCd.Runner.Settings.JobLogStreamSettings),
            ["SourceCache"] = typeof(SnapCd.Runner.Settings.SourceCacheSettings),
        };

        var sectionFragments = new Dictionary<string, JsonNode>
        {
            ["Logging"] = StandardSchemaFragments.Logging,
        };

        return SettingsSchemaCliRunner.Run(
            component: "runner",
            sectionTypes: sectionTypes,
            outputPath: Path.Combine(snapcdRoot, "schemas", "runner.schema.json"),
            args: [],
            sectionFragments: sectionFragments);
    }

    // Keys must match the section names bound in SnapCd.Agent/Program.cs.
    private static int RunAgent(string snapcdRoot)
    {
        var sectionTypes = new Dictionary<string, Type>
        {
            [SnapCd.Agent.Configuration.ServerSettings.SectionName] = typeof(SnapCd.Agent.Configuration.ServerSettings),
            [SnapCd.Agent.Configuration.AgentOptions.SectionName] = typeof(SnapCd.Agent.Configuration.AgentOptions),
        };

        var sectionFragments = new Dictionary<string, JsonNode>
        {
            ["Logging"] = StandardSchemaFragments.Logging,
        };

        return SettingsSchemaCliRunner.Run(
            component: "agent",
            sectionTypes: sectionTypes,
            outputPath: Path.Combine(snapcdRoot, "schemas", "agent.schema.json"),
            args: [],
            sectionFragments: sectionFragments);
    }

    // Keys must match the section names passed to Configure<T> in SnapCd.Server.Host/Program.cs and
    // the various AddSnapCd*() extension methods under SnapCd.Server.Core/Startup/.
    private static int RunServer(string snapcdRoot)
    {
        var sectionTypes = new Dictionary<string, Type>
        {
            ["Server"] = typeof(SnapCd.Server.Core.Settings.ServerSettings),
            ["ServiceBus"] = typeof(SnapCd.Server.Core.Settings.ServiceBusSettings),
            ["ProductionDataSeeder"] = typeof(SnapCd.Server.Core.Settings.DataSeeder.ProductionDataSeederSettings),
            ["DebugDataSeeder"] = typeof(SnapCd.Server.Core.Settings.DataSeeder.DebugDataSeederSettings),
            ["SecretStore"] = typeof(SnapCd.Server.Core.Settings.SecretStoreSettings),
            ["SourceRefresh"] = typeof(SnapCd.Server.Core.Settings.SourceRefreshSettings),
            ["InvitationSettings"] = typeof(SnapCd.Server.Core.Settings.InvitationSettings),
            ["OrphanedJobCleanup"] = typeof(SnapCd.Server.Core.Settings.OrphanedJobCleanupSettings),
            ["License"] = typeof(SnapCd.Server.Core.Settings.LicenseSettings),
            ["Debugging"] = typeof(SnapCd.Server.Core.Settings.DebuggingOptions),
            ["OpenIdConnect"] = typeof(SnapCd.Server.Core.Settings.OpenIdConnectSettings),
            ["Turnstile"] = typeof(SnapCd.Server.Core.Settings.TurnstileSettings),
            ["StateStore"] = typeof(SnapCd.Server.Core.Settings.StateStoreSettings),
            ["Caching"] = typeof(SnapCd.Server.Core.Settings.CachingSettings),
            ["EmailSender"] = typeof(SnapCd.Server.Core.Settings.EmailSenderSettings),
        };

        // Load the XmlDocLookup once so we can pass it to per-type Emit() calls when assembling the
        // dynamic Repositories fragment below (the static SettingsSchemaCliRunner.Run path loads its
        // own; this load is the same file, so two parsers running over the same XML is acceptable
        // for a generator that runs in seconds).
        var xmlDoc = SettingsSchemaEmitter.LoadXmlDoc(typeof(SnapCd.Server.Core.Settings.ServerSettings).Assembly);

        // Section-name → pre-baked schema fragment. Logging is the shared standard .NET shape;
        // ConnectionString and AllowHttp are Server-specific top-level scalars that don't fit the
        // POCO-driven path (they're read directly as `configuration["ConnectionString"]` /
        // `configuration.GetSection("AllowHttp").Get<bool>()` in Program.cs); Repositories is
        // built dynamically by reflecting on every *RepositorySettings type and emitting one schema
        // entry per entity (~85 entries).
        var sectionFragments = new Dictionary<string, JsonNode>
        {
            ["Logging"] = StandardSchemaFragments.Logging,
            ["ConnectionString"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] =
                    "SQL Server connection string. Used both for the application database and as the "
                    + "default ServiceBus transport. Sensitive — production deployments should source this "
                    + "via the External Settings provider rather than committing it to appsettings.json.",
            },
            ["AllowHttp"] = new JsonObject
            {
                ["type"] = "boolean",
                ["default"] = false,
                ["description"] =
                    "When false (default), the Server refuses to issue tokens over plain HTTP. Set to true "
                    + "only when terminating TLS upstream of the Server process (e.g. behind a reverse "
                    + "proxy that handles TLS itself).",
            },
            ["Repositories"] = BuildRepositoriesFragment(xmlDoc),
        };

        return SettingsSchemaCliRunner.Run(
            component: "server",
            sectionTypes: sectionTypes,
            outputPath: Path.Combine(snapcdRoot, "schemas", "server.schema.json"),
            args: [],
            sectionFragments: sectionFragments,
            postProcess: InjectForeignTypeFragments);
    }

    // ---------------------------------------------------------------------------
    // Foreign-type schema fragments — nested injection.
    //
    // Several settings properties have runtime types from foreign SDKs (MassTransit, Azure.Identity)
    // that are self-referentially deep enough to break the BCL JSON Schema exporter. The properties
    // keep their foreign type at the C# level so runtime binding and forwards-compatibility with the
    // SDK upgrades match what SDK consumers expect; the [JsonIgnore] attribute on each only prevents
    // the exporter from recursing into them. This post-process step pastes hand-authored schemas —
    // captured once by reflecting on the declared properties of each type — back into the empty
    // positions the exporter would otherwise leave.
    //
    // If any of the foreign types adds a declared property, the corresponding fragment here will
    // fall behind. The schemas are scoped to the *operator-relevant* surface (the fields a
    // self-hosted operator might actually set), so partial divergence is acceptable — operators who
    // need exotic tunables can still set them via env vars, just without IntelliSense.
    // ---------------------------------------------------------------------------

    private static void InjectForeignTypeFragments(JsonObject document)
    {
        InjectMassTransitFragments(document);
        InjectAzureKeyVaultCredentialOptionsFragment(document);
    }

    private static void InjectMassTransitFragments(JsonObject document)
    {
        // Navigate to the TransportOptions sub-schema. Both children carry [JsonIgnore] in source so
        // the emitter typically omits the `properties` key entirely on this node — create it if so.
        var transportOptions = document
            ["properties"]?["ServiceBus"]?["properties"]?["TransportOptions"] as JsonObject;

        if (transportOptions is null)
        {
            throw new InvalidOperationException(
                "Server schema document is missing the expected ServiceBus.TransportOptions node. "
                + "Check that ServiceBus is still in sectionTypes.");
        }

        if (transportOptions["properties"] is not JsonObject properties)
        {
            properties = new JsonObject();
            transportOptions["properties"] = properties;
        }

        properties["AzureServiceBus"] = new JsonObject
        {
            ["type"] = "object",
            ["description"] =
                "Azure Service Bus transport configuration. Required when ServiceBus.BusType is "
                + "AzureServiceBus. Property type at runtime is MassTransit.AzureServiceBusTransportOptions; "
                + "the schema below covers the operator-settable surface (a single ConnectionString as "
                + "of MassTransit 8.x).",
            ["properties"] = new JsonObject
            {
                ["ConnectionString"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] =
                        "Azure Service Bus namespace connection string. Accepts either the URI form "
                        + "(sb://{namespace}.servicebus.windows.net — uses managed identity via "
                        + "DefaultAzureCredential) or the standard SAS form "
                        + "(Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=...). Sensitive — "
                        + "production deployments should source this via the External Settings provider.",
                },
            },
        };

        properties["SqlServer"] = new JsonObject
        {
            ["type"] = "object",
            ["description"] =
                "SQL Server transport configuration. Required when ServiceBus.BusType is SqlServer. "
                + "Property type at runtime is MassTransit.SqlTransport.SqlTransportOptions; the schema "
                + "below covers its full declared surface.",
            ["properties"] = new JsonObject
            {
                ["ConnectionString"] = new JsonObject
                {
                    ["type"] = new JsonArray { "string", "null" },
                    ["description"] =
                        "Connection string for the SQL Server hosting the transport tables. When null "
                        + "(the default), the app's top-level ConnectionString is reused so transport "
                        + "tables live in the same database as application data.",
                },
                ["Host"] = new JsonObject
                {
                    ["type"] = new JsonArray { "string", "null" },
                    ["description"] = "SQL Server host. Ignored when ConnectionString is set.",
                },
                ["Port"] = new JsonObject
                {
                    ["type"] = new JsonArray { "integer", "null" },
                    ["description"] = "SQL Server port. Ignored when ConnectionString is set.",
                },
                ["Database"] = new JsonObject
                {
                    ["type"] = new JsonArray { "string", "null" },
                    ["description"] = "Database name. Ignored when ConnectionString is set.",
                },
                ["Username"] = new JsonObject
                {
                    ["type"] = new JsonArray { "string", "null" },
                    ["description"] =
                        "SQL login the transport runs as. Ignored when ConnectionString is set.",
                },
                ["Password"] = new JsonObject
                {
                    ["type"] = new JsonArray { "string", "null" },
                    ["description"] =
                        "Password for the SQL login. Sensitive — source via the External Settings provider "
                        + "in production. Ignored when ConnectionString is set.",
                },
                ["AdminUsername"] = new JsonObject
                {
                    ["type"] = new JsonArray { "string", "null" },
                    ["description"] =
                        "SQL login MassTransit uses during transport maintenance (schema migrations, "
                        + "queue creation). Falls back to Username when null.",
                },
                ["AdminPassword"] = new JsonObject
                {
                    ["type"] = new JsonArray { "string", "null" },
                    ["description"] =
                        "Password for AdminUsername. Sensitive — source via the External Settings provider.",
                },
                ["Schema"] = new JsonObject
                {
                    ["type"] = new JsonArray { "string", "null" },
                    ["description"] = "SQL schema for transport tables. Defaults to \"transport\".",
                },
                ["Role"] = new JsonObject
                {
                    ["type"] = new JsonArray { "string", "null" },
                    ["description"] =
                        "Optional SQL role MassTransit assumes when running maintenance operations.",
                },
                ["ConnectionLimit"] = new JsonObject
                {
                    ["type"] = new JsonArray { "integer", "null" },
                    ["description"] =
                        "Maximum number of concurrent SQL connections the transport opens. Null lets "
                        + "MassTransit pick a default based on the configured concurrency.",
                },
                ["DisableMaintenance"] = new JsonObject
                {
                    ["type"] = "boolean",
                    ["description"] =
                        "When true, the Server skips running MassTransit's transport-maintenance hosted "
                        + "service. Use when maintenance is performed out-of-band (e.g. by a dedicated "
                        + "ops process) and the Server should only consume / produce.",
                    ["default"] = false,
                },
            },
        };
    }

    // Azure Key Vault credential options — nested injection.
    //
    // SecretStoreSettings.AzureKeyVault.CredentialOptions has property type
    // Azure.Identity.DefaultAzureCredentialOptions — self-referentially deep through its
    // Azure.Core.ClientOptions base. [JsonIgnore]'d at source to prevent the BCL exporter from
    // recursing; the fragment pasted in comes from SnapCd.Utils.Settings.StandardSchemaFragments
    // (loaded once from an embedded JSON resource so future generators that surface AKV-backed
    // settings can reuse the same shape).
    private static void InjectAzureKeyVaultCredentialOptionsFragment(JsonObject document)
    {
        var azureKeyVault = document
            ["properties"]?["SecretStore"]?["properties"]?["AzureKeyVault"] as JsonObject;

        if (azureKeyVault is null)
        {
            throw new InvalidOperationException(
                "Server schema document is missing the expected SecretStore.AzureKeyVault node. "
                + "Check that SecretStore is still in sectionTypes.");
        }

        if (azureKeyVault["properties"] is not JsonObject properties)
        {
            properties = new JsonObject();
            azureKeyVault["properties"] = properties;
        }

        properties["CredentialOptions"] = StandardSchemaFragments.AzureKeyVaultCredentialOptions.DeepClone();
    }

    // Repositories fragment builder.
    //
    // SnapCd.Server.Core/Startup/RepositorySettings.cs binds ~85 per-entity *RepositorySettings
    // types via `Configure<T>(configuration.GetSection("Repositories:<Entity>"))`. Hand-listing
    // every entry in the section-types map would be brittle and would silently drift when entities
    // are added. Reflection over the SnapCd.Server.Core.Settings.Repositories namespace discovers
    // them deterministically: every type ending in "RepositorySettings" maps to a "Repositories:<Entity>"
    // binding where "<Entity>" is the type name minus the suffix.
    private static JsonObject BuildRepositoriesFragment(XmlDocLookup? xmlDoc)
    {
        const string suffix = "RepositorySettings";
        var assembly = typeof(SnapCd.Server.Core.Settings.ServerSettings).Assembly;

        var properties = new JsonObject();
        foreach (var type in assembly.GetTypes().OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            if (type.Namespace != "SnapCd.Server.Core.Settings.Repositories") continue;
            if (!type.Name.EndsWith(suffix, StringComparison.Ordinal)) continue;
            if (!type.IsClass || type.IsAbstract) continue;

            var entityName = type.Name[..^suffix.Length];
            properties[entityName] = SettingsSchemaEmitter.Emit(type, xmlDoc);
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["description"] =
                "Per-entity repository overrides. Each sub-key binds a discrete *RepositorySettings "
                + "POCO from SnapCd.Server.Core.Settings.Repositories. Almost all are empty placeholders "
                + "in the default appsettings.json — operators set them only to override per-entity "
                + "behaviour (typically just for advanced tuning).",
            ["properties"] = properties,
        };
    }
}
