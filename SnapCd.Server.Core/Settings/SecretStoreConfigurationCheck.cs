// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Answers whether the secret store is usable, so a caller can report the problem instead of
/// failing when it first tries to read a secret. Messages name settings in appsettings.json form
/// because that is where the reader has to go to fix them.
/// </summary>
public static class SecretStoreConfigurationCheck
{
    /// <summary>The problem with the configured secret store, or null when it is usable.</summary>
    public static string? Validate(SecretStoreSettings settings)
    {
        return settings.Provider switch
        {
            SecretStoreProvider.SqlServer => ValidateSqlServer(settings),
            _ => null
        };
    }

    /// <summary>
    /// Decodes the SQL store's symmetric key. Callable regardless of the configured provider,
    /// since the secret migrator reads and writes the SQL store either way.
    /// </summary>
    public static bool TryGetSqlServerKey(SecretStoreSettings settings, out byte[] key, out string? problem)
    {
        key = [];

        var base64 = settings.SqlServer?.SymmetricKey;
        if (string.IsNullOrWhiteSpace(base64))
        {
            problem = "SecretStore:SqlServer:SymmetricKey is not set.";
            return false;
        }

        try
        {
            key = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            problem = "SecretStore:SqlServer:SymmetricKey must be a Base64-encoded 32-byte AES-256 key.";
            return false;
        }

        if (key.Length != 32)
        {
            problem = $"SecretStore:SqlServer:SymmetricKey must decode to 32 bytes (got {key.Length}).";
            return false;
        }

        problem = null;
        return true;
    }

    private static string? ValidateSqlServer(SecretStoreSettings settings)
        => TryGetSqlServerKey(settings, out _, out var problem) ? null : problem;
}
