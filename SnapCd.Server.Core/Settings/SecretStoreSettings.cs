// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using Azure.Identity;

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Backing-store options for Snap CD Secret resources (Stack / Namespace / Module Secrets and
/// sensitive Output Sets). Selected by <see cref="SecretStoreSettings.Provider"/>.
/// </summary>
public enum SecretStoreProvider
{
    /// <summary>Azure Key Vault. Paid feature — gated by the active license tier.</summary>
    AzureKeyVault,

    /// <summary>The Server's primary SQL Server database. Default; no extra infrastructure required.</summary>
    SqlServer
}

/// <summary>
/// Selects and configures the backing store for Snap CD Secret resources — the encrypted store
/// where Stack, Namespace, and Module Secret values, plus sensitive Output Sets, live at rest.
/// The block matching <see cref="Provider"/> is the only one read; the other is ignored. The
/// Migrator utility moves secrets between stores in-place when an operator changes <see cref="Provider"/>.
/// </summary>
public class SecretStoreSettings
{
    /// <summary>
    /// Backing store for Secret values. Defaults to <c>AzureKeyVault</c>; on Self-Hosted Community
    /// the License Service force-substitutes <c>SqlServer</c> at runtime since AKV is a paid feature.
    /// </summary>
    public SecretStoreProvider Provider { get; set; } = SecretStoreProvider.AzureKeyVault;

    /// <summary>Settings specific to the AzureKeyVault provider.</summary>
    public AzureKeyVaultSecretStoreSettings AzureKeyVault { get; set; } = new();

    /// <summary>Settings specific to the SqlServer provider.</summary>
    public SqlServerSecretStoreSettings? SqlServer { get; set; }

    /// <summary>
    /// When true, exposes the Secret Migrator utility (under System nav) for privileged
    /// users to copy secrets between the SQL and Azure Key Vault stores. Keep false by default.
    /// </summary>
    public bool EnableMigrator { get; set; } = false;

    /// <summary>
    /// Tuning knobs for the Secret Migrator utility. Ignored unless <see cref="EnableMigrator"/> is true.
    /// </summary>
    public SecretMigratorSettings Migrator { get; set; } = new();
}

/// <summary>
/// Tuning for the Secret Migrator background process that moves secret values between the two
/// supported backing stores. Only consulted when <see cref="SecretStoreSettings.EnableMigrator"/>
/// is true; defaults are tuned for a typical AKV-throttled deployment.
/// </summary>
public class SecretMigratorSettings
{
    /// <summary>
    /// Max concurrent Azure Key Vault calls during planning (existence probes) and execution (copies).
    /// AKV's default throttle is ~2000 ops / 10 s per vault — 8–16 is comfortable, 32 is an aggressive upper bound.
    /// </summary>
    public int MaxParallelism { get; set; } = 8;
}

/// <summary>
/// Azure Key Vault-specific Secret Store configuration. Required only when
/// <see cref="SecretStoreSettings.Provider"/> is <c>AzureKeyVault</c>. Authentication is via the
/// Azure.Identity SDK; explicit-credentials mode is available when managed identity / federated
/// auth isn't an option.
/// </summary>
public class AzureKeyVaultSecretStoreSettings
{
    // [JsonIgnore] keeps DefaultAzureCredentialOptions out of the generated JSON Schema —
    // the Azure SDK type is self-referentially deep and breaks the BCL schema exporter. The
    // ConfigurationBinder ignores [JsonIgnore], so runtime binding is unaffected. The schema for
    // this property comes from SnapCd.Utils.StandardSchemaFragments.AzureKeyVaultCredentialOptions
    // injected via the Server generator's post-process callback.
    /// <summary>
    /// Default-credential options passed to <c>DefaultAzureCredential</c>. The 11 <c>Exclude*</c>
    /// flags choose which credential types the chain tries; tenant / authority overrides handle
    /// multi-tenant and sovereign-cloud scenarios. Ignored when
    /// <see cref="UseExplicitCredentials"/> is true.
    /// </summary>
    [JsonIgnore]
    public DefaultAzureCredentialOptions CredentialOptions { get; set; } = new();

    /// <summary>
    /// When true, bypass <c>DefaultAzureCredential</c> and use the client-credentials flow against
    /// <see cref="ExplicitCredentials"/> instead. Use when the host can't run as a managed
    /// identity and the credential chain doesn't resolve.
    /// </summary>
    public bool UseExplicitCredentials { get; set; }

    /// <summary>
    /// Explicit client_id / client_secret / tenant_id credentials. Required when
    /// <see cref="UseExplicitCredentials"/> is true. Sensitive — source via the External Settings
    /// provider in production.
    /// </summary>
    public AzureExplicitCredentials ExplicitCredentials { get; set; } = new();

    /// <summary>
    /// Default Key Vault URL used when fetching a Secret value at runtime. Per-Secret overrides
    /// are possible via metadata; this is the fallback.
    /// </summary>
    public string DefaultInputKeyVaultUrl { get; set; } = string.Empty;

    /// <summary>
    /// Default Key Vault URL used when writing a Secret value (including sensitive Module outputs).
    /// Often the same as <see cref="DefaultInputKeyVaultUrl"/>; split when read- and write-paths
    /// need different vaults for compliance or geo-replication reasons.
    /// </summary>
    public string DefaultOutputKeyVaultUrl { get; set; } = string.Empty;
}

/// <summary>
/// SQL Server-specific Secret Store configuration. Only consulted when
/// <see cref="SecretStoreSettings.Provider"/> is <c>SqlServer</c>. Secret values are encrypted
/// at rest with the supplied symmetric key before being persisted to the secrets table.
/// </summary>
public class SqlServerSecretStoreSettings
{
    /// <summary>Base64-encoded 32-byte (AES-256) symmetric key used to encrypt secret values at rest.</summary>
    public string? SymmetricKey { get; set; }
}

/// <summary>
/// Explicit Azure AD client_credentials grant. Used as the AKV credential when
/// <see cref="AzureKeyVaultSecretStoreSettings.UseExplicitCredentials"/> is true.
/// </summary>
public class AzureExplicitCredentials
{
    /// <summary>The Azure AD application's client ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The Azure AD application's client secret. Sensitive — source via the External Settings provider.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Azure AD tenant ID hosting the application.</summary>
    public string TenantId { get; set; } = string.Empty;
}

