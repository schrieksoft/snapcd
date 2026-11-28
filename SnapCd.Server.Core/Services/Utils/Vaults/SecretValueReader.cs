// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Services.Interfaces.Vaults;

namespace SnapCd.Server.Core.Services.Utils.Vaults;

/// <summary>
/// Reads a Secret's value from the vault. A Namespace Secret that began as a Stack Secret still has
/// its value under the Stack name, so a miss under its own name looks there and, on finding it,
/// writes it under the name the Secret has.
/// </summary>
public static class SecretValueReader
{
    public static async Task<string> Read(IVault vault, Secret secret, Guid organizationId) =>
        (await ReadWithVersion(vault, secret, organizationId)).Value;

    public static async Task<VersionedSecret> ReadWithVersion(IVault vault, Secret secret, Guid organizationId)
    {
        var name = RemoteSecretName.For(secret, organizationId);

        try
        {
            return await vault.GetSecretWithVersionAsync(name);
        }
        catch (Exception e) when (secret is NamespaceSecret && IsNotFound(e))
        {
            var value = await ReadOrNull(vault, RemoteSecretName.AsStackSecret(secret, organizationId));
            if (value == null) throw;

            await vault.SetSecretAsync(name, value);
            return await vault.GetSecretWithVersionAsync(name);
        }
    }

    /// <summary>Removes the value under both names, so that none is left behind for a deleted Secret.</summary>
    public static async Task Delete(IVault vault, Secret secret, Guid organizationId)
    {
        await vault.DeleteSecretAsync(RemoteSecretName.For(secret, organizationId));

        if (secret is NamespaceSecret)
            await vault.DeleteSecretAsync(RemoteSecretName.AsStackSecret(secret, organizationId));
    }

    private static async Task<string?> ReadOrNull(IVault vault, string name)
    {
        try
        {
            return await vault.GetSecretAsync(name);
        }
        catch (Exception e) when (IsNotFound(e))
        {
            return null;
        }
    }

    // Only absence sends the read to the other name: a vault that failed to answer may hold a newer value.
    private static bool IsNotFound(Exception e) =>
        e is KeyNotFoundException or Azure.RequestFailedException { Status: 404 };
}
