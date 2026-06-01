// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Azure.Identity;

namespace SnapCd.Server.Core.Settings;

public enum SecretStoreProvider
{
    AzureKeyVault,
    SqlServer
}

public class SecretStoreSettings
{
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

public class SecretMigratorSettings
{
    /// <summary>
    /// Max concurrent Azure Key Vault calls during planning (existence probes) and execution (copies).
    /// AKV's default throttle is ~2000 ops / 10 s per vault — 8–16 is comfortable, 32 is an aggressive upper bound.
    /// </summary>
    public int MaxParallelism { get; set; } = 8;
}

public class AzureKeyVaultSecretStoreSettings
{
    public DefaultAzureCredentialOptions CredentialOptions { get; set; } = new();
    public bool UseExplicitCredentials { get; set; }
    public AzureExplicitCredentials ExplicitCredentials { get; set; } = new();

    public string DefaultInputKeyVaultUrl { get; set; } = string.Empty;
    public string DefaultOutputKeyVaultUrl { get; set; } = string.Empty;
}

public class SqlServerSecretStoreSettings
{
    /// <summary>Base64-encoded 32-byte (AES-256) symmetric key used to encrypt secret values at rest.</summary>
    public string? SymmetricKey { get; set; }
}

public class AzureExplicitCredentials
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class AwsStoreSettings
{
    public bool UseExplicitCredentials { get; set; }
    public AwsExplicitCredentials ExplicitCredentials { get; set; } = new();
}

public class AwsExplicitCredentials
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}