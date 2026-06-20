// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Integrations;

/// <summary>
/// Reads/writes/deletes an integration's connection blob in the org's input vault, at
/// <c>integration--{organizationId}--{id}</c> (the same <c>{prefix}--{org}--{id}</c> convention used for
/// other secrets). The whole connection — secret and non-secret fields alike — lives here.
/// </summary>
public sealed class IntegrationSecretStore(
    IVaultFactory vaultFactory,
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IOptions<SecretStoreSettings> secretStoreSettings)
{
    private readonly SecretStoreSettings _secretStoreSettings = secretStoreSettings.Value;

    public static string SecretName(Guid organizationId, Guid integrationId)
        => $"integration--{organizationId}--{integrationId}";

    private async Task<Services.Vaults.IVault> CreateVaultAsync(Guid organizationId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var url = await db.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => o.InputKeyVaultUrl)
            .FirstOrDefaultAsync();
        return vaultFactory.Create(url ?? _secretStoreSettings.AzureKeyVault.DefaultInputKeyVaultUrl);
    }

    public async Task WriteAsync(Guid organizationId, Guid integrationId, string json)
    {
        using var vault = await CreateVaultAsync(organizationId);
        await vault.SetSecretAsync(SecretName(organizationId, integrationId), json);
    }

    public async Task<string?> ReadAsync(Guid organizationId, Guid integrationId)
    {
        using var vault = await CreateVaultAsync(organizationId);
        try
        {
            return await vault.GetSecretAsync(SecretName(organizationId, integrationId));
        }
        catch
        {
            // Missing/unavailable secret — treat as no connection rather than failing the read.
            return null;
        }
    }

    public async Task DeleteAsync(Guid organizationId, Guid integrationId)
    {
        using var vault = await CreateVaultAsync(organizationId);
        try
        {
            await vault.DeleteSecretAsync(SecretName(organizationId, integrationId));
        }
        catch
        {
            // Best-effort cleanup; a missing secret is not an error.
        }
    }
}
