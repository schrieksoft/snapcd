// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using System.Text.Json.Nodes;

namespace SnapCd.Utils.Settings;

/// <summary>
/// Pre-baked JSON Schema fragments for settings sections that don't fit the POCO-driven path the
/// <see cref="SettingsSchemaEmitter"/> takes for snapcd-defined settings types. These are settings
/// the .NET runtime or other libraries define — Snap CD doesn't own the shape, only the choice to
/// include them in the component's appsettings.json.
///
/// Each fragment is shipped as an <c>EmbeddedResource</c> JSON file under
/// <c>SnapCd.Utils/Settings/Carveouts/</c> and loaded once at first access. Generators reference
/// them by name through this class rather than re-loading the resources directly, so a typo in
/// the section name fails at compile time instead of producing a silent empty fragment.
/// </summary>
public static class StandardSchemaFragments
{
    private static readonly Assembly Assembly = typeof(StandardSchemaFragments).Assembly;

    /// <summary>
    /// The standard .NET <c>Logging</c> section — <c>LogLevel</c> map plus open
    /// <c>additionalProperties</c> for provider-specific sub-blocks (Console, Debug, etc.).
    /// </summary>
    public static JsonNode Logging => LoggingValue.Value;

    /// <summary>
    /// Operator-relevant subset of <c>Azure.Identity.DefaultAzureCredentialOptions</c> — the
    /// 11 <c>Exclude*</c> credential-type flags plus multi-tenant / sovereign-cloud /
    /// managed-identity overrides. Intended for injection at any settings path whose runtime
    /// type binds <c>DefaultAzureCredentialOptions</c>; currently consumed by
    /// <c>SnapCd.Settings.Generator.Server</c> at <c>SecretStore.AzureKeyVault.CredentialOptions</c>,
    /// and reusable from any future generator that surfaces AKV-backed settings.
    /// </summary>
    public static JsonNode AzureKeyVaultCredentialOptions => AzureKeyVaultCredentialOptionsValue.Value;

    private static readonly Lazy<JsonNode> LoggingValue = new(() => Load("logging.schema.json"));

    private static readonly Lazy<JsonNode> AzureKeyVaultCredentialOptionsValue = new(
        () => Load("azurekeyvault-credentialoptions.schema.json"));

    private static JsonNode Load(string resourceFileName)
    {
        // Embedded resources are keyed by RootNamespace + dotted path. The csproj has
        // <RootNamespace>SnapCd.Utils</RootNamespace>, so files under Settings/Carveouts/ land at
        // SnapCd.Utils.Settings.Carveouts.<filename>.
        var resourceName = $"SnapCd.Utils.Settings.Carveouts.{resourceFileName}";
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Carve-out resource '{resourceName}' not found. Check the <EmbeddedResource> "
                + "entry in SnapCd.Utils.csproj and the file's location under Settings/Carveouts/.");

        return JsonNode.Parse(stream)
            ?? throw new InvalidOperationException(
                $"Carve-out resource '{resourceName}' parsed to a null JSON node. The file is "
                + "likely empty or not valid JSON.");
    }
}
