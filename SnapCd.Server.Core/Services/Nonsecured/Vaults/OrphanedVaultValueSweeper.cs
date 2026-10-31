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
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Services.Utils.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Nonsecured.Vaults;

/// <summary>
/// Removes values whose owner has been deleted.
///
/// A delete is a SQL cascade and makes no vault call, so the rows go and the values stay. The
/// value's name carries the organization and the owner, so everything one deleted owner held is
/// found by prefix and removed here, after the rows are already gone.
/// </summary>
public class OrphanedVaultValueSweeper(
    SnapCdDbContext dbContext,
    IVaultFactory vaultFactory,
    IOptions<SecretStoreSettings> secretStoreSettings,
    ILogger<OrphanedVaultValueSweeper> logger)
{
    private readonly SecretStoreSettings _settings = secretStoreSettings.Value;

    /// <summary>
    /// Deletes every value stored under this prefix. Called once the owner's rows are gone, so a
    /// match is by definition unreferenced.
    /// </summary>
    public async Task SweepPrefix(string prefix, Guid organizationId, VaultKind kind, CancellationToken ct = default)
    {
        using var vault = vaultFactory.Create(await VaultUrl(organizationId, kind, ct));

        var names = await vault.ListNamesAsync(prefix, ct);
        if (names.Count == 0) return;

        var removed = 0;
        foreach (var name in names)
        {
            try
            {
                await vault.DeleteSecretAsync(name);
                removed++;
            }
            catch (Exception ex)
            {
                // One value failing says nothing about the next; the scheduled sweep catches what is left.
                logger.LogWarning(ex, "Could not remove orphaned value {SecretName}", name);
            }
        }

        logger.LogInformation(
            "Removed {Removed} of {Found} orphaned value(s) under {Prefix}", removed, names.Count, prefix);
    }

    /// <summary>
    /// Removes the values of a Module's sensitive outputs that no OutputSet declares any more.
    ///
    /// An output's vault name carries the Module and the output's name, so sets of the same Module
    /// share it. What survives is read from the rows that remain, and anything under the Module's
    /// prefix that is not among them belonged only to sets that are gone.
    /// </summary>
    public async Task SweepUnreferencedModuleOutputs(
        Guid moduleId, Guid organizationId, CancellationToken ct = default)
    {
        var stillReferenced = await dbContext.Set<SecretOutput>()
            .Where(o => o.OrganizationId == organizationId && o.OutputSet.ModuleId == moduleId)
            .Select(o => o.RemoteSecretName)
            .Distinct()
            .ToListAsync(ct);

        await SweepPrefixExcept(
            RemoteSecretName.OutputPrefix(organizationId, moduleId),
            stillReferenced.ToHashSet(StringComparer.Ordinal),
            organizationId,
            VaultKind.Output,
            ct);
    }

    /// <summary>
    /// Removes the values of a Module's sensitive signals that no SignalOutputSet records any
    /// more. Sets of one Module share names where they answer the same wave, so what survives
    /// decides what stays.
    /// </summary>
    public async Task SweepUnreferencedSignals(
        Guid moduleId, Guid organizationId, CancellationToken ct = default)
    {
        var stillReferenced = await dbContext.Set<SecretSignalOutput>()
            .Where(o => o.OrganizationId == organizationId &&
                        o.SignalOutputSet.ModuleId == moduleId)
            .Select(o => o.RemoteSecretName)
            .Distinct()
            .ToListAsync(ct);

        await SweepPrefixExcept(
            RemoteSecretName.SignalPrefix(organizationId, moduleId),
            stillReferenced.ToHashSet(StringComparer.Ordinal),
            organizationId,
            VaultKind.Input,
            ct);
    }

    /// <summary>
    /// Deletes everything under the prefix that is not in the keep set. The keep set is read from
    /// the rows that survived the delete, so an empty one means nothing references the prefix.
    /// </summary>
    private async Task SweepPrefixExcept(
        string prefix,
        HashSet<string> keep,
        Guid organizationId,
        VaultKind kind,
        CancellationToken ct)
    {
        using var vault = vaultFactory.Create(await VaultUrl(organizationId, kind, ct));

        var names = await vault.ListNamesAsync(prefix, ct);
        var orphaned = names.Where(n => !keep.Contains(n)).ToList();
        if (orphaned.Count == 0) return;

        var removed = 0;
        foreach (var name in orphaned)
        {
            try
            {
                await vault.DeleteSecretAsync(name);
                removed++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not remove orphaned value {SecretName}", name);
            }
        }

        logger.LogInformation(
            "Removed {Removed} of {Found} unreferenced value(s) under {Prefix}", removed, orphaned.Count, prefix);
    }

    private async Task<string> VaultUrl(Guid organizationId, VaultKind kind, CancellationToken ct)
    {
        var organization = await dbContext.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => new { o.InputKeyVaultUrl, o.OutputKeyVaultUrl })
            .FirstOrDefaultAsync(ct);

        var akv = _settings.AzureKeyVault;

        return kind == VaultKind.Output
            ? organization?.OutputKeyVaultUrl ?? akv.DefaultOutputKeyVaultUrl
            : organization?.InputKeyVaultUrl ?? akv.DefaultInputKeyVaultUrl;
    }
}

/// <summary>
/// Which of an organization's two vaults holds a value. Module outputs live in the output vault;
/// Secrets, integration connections and probe signals in the input one.
/// </summary>
public enum VaultKind
{
    Input,
    Output
}
