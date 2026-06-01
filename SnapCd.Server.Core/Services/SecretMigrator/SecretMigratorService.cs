// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Services.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.SecretMigrator;

public class SecretMigratorService(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    AzureVaultFactory azureVaultFactory,
    SqlVaultFactory sqlVaultFactory,
    IOptions<SecretStoreSettings> settings,
    ILogger<SecretMigratorService> logger)
{
    private const int MaxPlanItems = 10_000;

    private int Parallelism => Math.Max(1, settings.Value.Migrator.MaxParallelism);

    public async Task<MigrationPlan> PlanAsync(
        MigrationDirection direction,
        Guid organizationId,
        Guid callerUserId,
        string? inputVaultUrlOverride,
        string? outputVaultUrlOverride,
        IProgress<PlanProgress>? progress,
        CancellationToken ct)
    {
        await EnsureAuthorizedAsync(callerUserId, organizationId, ct);
        EnsureEnabled();

        var akv = settings.Value.AzureKeyVault;
        var inputUrl = inputVaultUrlOverride ?? akv.DefaultInputKeyVaultUrl;
        var outputUrl = outputVaultUrlOverride ?? akv.DefaultOutputKeyVaultUrl;

        if (string.IsNullOrWhiteSpace(inputUrl) || string.IsNullOrWhiteSpace(outputUrl))
            throw new InvalidOperationException("Both the Input and Output Azure Key Vault URLs must be configured (or supplied per-run).");

        var orgMarker = $"--{organizationId}--";
        List<string> sourceNames;

        switch (direction)
        {
            case MigrationDirection.SqlToAzureKeyVault:
                progress?.Report(new PlanProgress(PlanPhase.ListingSource, 0, null, null));
                await using (var db = await dbContextFactory.CreateDbContextAsync(ct))
                {
                    sourceNames = await db.Set<VaultSecret>()
                        .Where(v => v.Name.Contains(orgMarker))
                        .OrderBy(v => v.Name)
                        .Select(v => v.Name)
                        .Take(MaxPlanItems + 1)
                        .ToListAsync(ct);
                }
                break;

            case MigrationDirection.AzureKeyVaultToSql:
                sourceNames = new List<string>();
                progress?.Report(new PlanProgress(PlanPhase.ListingSource, 0, null, inputUrl));
                await foreach (var name in azureVaultFactory.ListSecretNamesAsync(inputUrl, ct))
                {
                    ct.ThrowIfCancellationRequested();
                    if (!name.Contains(orgMarker, StringComparison.Ordinal)) continue;
                    if (IsOutputName(name)) continue;
                    sourceNames.Add(name);
                    progress?.Report(new PlanProgress(PlanPhase.ListingSource, sourceNames.Count, null, name));
                    if (sourceNames.Count > MaxPlanItems) break;
                }
                progress?.Report(new PlanProgress(PlanPhase.ListingSource, sourceNames.Count, null, outputUrl));
                await foreach (var name in azureVaultFactory.ListSecretNamesAsync(outputUrl, ct))
                {
                    ct.ThrowIfCancellationRequested();
                    if (!name.Contains(orgMarker, StringComparison.Ordinal)) continue;
                    if (!IsOutputName(name)) continue;
                    sourceNames.Add(name);
                    progress?.Report(new PlanProgress(PlanPhase.ListingSource, sourceNames.Count, null, name));
                    if (sourceNames.Count > MaxPlanItems) break;
                }
                sourceNames.Sort(StringComparer.Ordinal);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction));
        }

        if (sourceNames.Count > MaxPlanItems)
            throw new InvalidOperationException(
                $"More than {MaxPlanItems} secrets matched — too many to migrate via the UI. Run in batches.");

        var items = new List<MigrationItem>(sourceNames.Count);
        var total = sourceNames.Count;

        if (direction == MigrationDirection.AzureKeyVaultToSql)
        {
            progress?.Report(new PlanProgress(PlanPhase.ProbingDestination, 0, total, null));

            HashSet<string> destNames;
            await using (var db = await dbContextFactory.CreateDbContextAsync(ct))
            {
                destNames = (await db.Set<VaultSecret>()
                        .Where(v => sourceNames.Contains(v.Name))
                        .Select(v => v.Name)
                        .ToListAsync(ct))
                    .ToHashSet(StringComparer.Ordinal);
            }

            foreach (var name in sourceNames)
            {
                var dest = destNames.Contains(name);
                items.Add(BuildItem(name, sourceExists: true, destExists: dest));
            }

            progress?.Report(new PlanProgress(PlanPhase.ProbingDestination, total, total, null));
        }
        else
        {
            // SQL→AKV; probe AKV per-item, in parallel (AKV throttles — small MDOP).
            // Two long-lived SecretClients (one per vault URL) reused across the parallel loop —
            // avoids re-pooling connections and re-validating credentials per item.
            using var inputAkv = azureVaultFactory.Create(inputUrl);
            using var outputAkv = azureVaultFactory.Create(outputUrl);

            var probed = 0;
            var resultsByIndex = new MigrationItem[sourceNames.Count];
            var indexed = sourceNames.Select((name, idx) => (name, idx));

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Parallelism,
                CancellationToken = ct
            };

            progress?.Report(new PlanProgress(PlanPhase.ProbingDestination, 0, total, null));

            await Parallel.ForEachAsync(indexed, parallelOptions, async (entry, token) =>
            {
                var vault = IsOutputName(entry.name) ? outputAkv : inputAkv;
                var exists = await TryGetAsync(vault, entry.name, token);
                resultsByIndex[entry.idx] = BuildItem(entry.name, sourceExists: true, destExists: exists);

                var done = Interlocked.Increment(ref probed);
                progress?.Report(new PlanProgress(PlanPhase.ProbingDestination, done, total, entry.name));
            });

            items.AddRange(resultsByIndex);
        }

        progress?.Report(new PlanProgress(PlanPhase.Done, items.Count, total, null));

        return new MigrationPlan(
            RunId: Guid.NewGuid(),
            Direction: direction,
            Items: items,
            InputVaultUrl: inputUrl,
            OutputVaultUrl: outputUrl);
    }

    public async Task<MigrationProgress> ExecuteAsync(
        MigrationPlan plan,
        Guid organizationId,
        Guid callerUserId,
        IProgress<MigrationProgress> progress,
        CancellationToken ct)
    {
        await EnsureAuthorizedAsync(callerUserId, organizationId, ct);
        EnsureEnabled();

        var runStartedUtc = DateTime.UtcNow;
        var total = plan.Items.Count;
        var processed = 0;
        var copied = 0;
        var overwritten = 0;
        var skipped = 0;
        var failed = 0;
        string? lastError = null;

        void Report(string? currentName)
        {
            progress.Report(new MigrationProgress(
                total, processed, copied, overwritten, skipped, failed, currentName, lastError));
        }

        Report(null);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Parallelism,
            CancellationToken = ct
        };

        // Long-lived vaults reused across the parallel loop. Azure SecretClient is thread-safe
        // and designed for reuse; creating one per item re-pools connections unnecessarily.
        using var inputAkv = azureVaultFactory.Create(plan.InputVaultUrl);
        using var outputAkv = azureVaultFactory.Create(plan.OutputVaultUrl);
        using var sql = sqlVaultFactory.Create("");

        IVault SourceVaultFor(MigrationItem item) => plan.Direction switch
        {
            MigrationDirection.SqlToAzureKeyVault => sql,
            MigrationDirection.AzureKeyVaultToSql => item.Kind == MigrationKind.Output ? outputAkv : inputAkv,
            _ => throw new ArgumentOutOfRangeException(nameof(plan.Direction))
        };

        IVault DestVaultFor(MigrationItem item) => plan.Direction switch
        {
            MigrationDirection.SqlToAzureKeyVault => item.Kind == MigrationKind.Output ? outputAkv : inputAkv,
            MigrationDirection.AzureKeyVaultToSql => sql,
            _ => throw new ArgumentOutOfRangeException(nameof(plan.Direction))
        };

        await Parallel.ForEachAsync(plan.Items, parallelOptions, async (item, token) =>
        {
            Interlocked.Increment(ref processed);
            Report(item.Name);

            string? error = null;
            var actualAction = item.Action;

            try
            {
                if (item.Action == ItemAction.Skip)
                {
                    Interlocked.Increment(ref skipped);
                }
                else
                {
                    var value = await SourceVaultFor(item).GetSecretAsync(item.Name);
                    await DestVaultFor(item).SetIfChanged(item.Name, value);
                    if (item.Action == ItemAction.Copy) Interlocked.Increment(ref copied);
                    else Interlocked.Increment(ref overwritten);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failed);
                error = ex.Message;
                lastError = ex.Message;
                logger.LogWarning(ex, "Migrator failed on {Name}", item.Name);
                actualAction = item.Action; // keep planned; record error separately
            }

            await WriteAuditAsync(plan, item, actualAction, organizationId, callerUserId,
                plan.RunId, runStartedUtc, error, token);

            Report(item.Name);
        });

        Report(null);
        return new MigrationProgress(total, processed, copied, overwritten, skipped, failed, null, lastError);
    }

    private async Task WriteAuditAsync(
        MigrationPlan plan, MigrationItem item, ItemAction action,
        Guid organizationId, Guid callerUserId, Guid runId, DateTime runStartedUtc,
        string? error, CancellationToken ct)
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            db.Set<SecretMigrationAudit>().Add(new SecretMigrationAudit
            {
                Id = Guid.NewGuid(),
                RunId = runId,
                RunStartedUtc = runStartedUtc,
                OrganizationId = organizationId,
                ExecutedByUserId = callerUserId,
                Direction = plan.Direction.ToString(),
                Name = item.Name,
                Action = error is null ? action.ToString() : "Failed",
                Kind = item.Kind.ToString(),
                ErrorMessage = error,
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write migrator audit row for {Name}", item.Name);
        }
    }

    private static MigrationItem BuildItem(string name, bool sourceExists, bool destExists)
    {
        var kind = IsOutputName(name) ? MigrationKind.Output : MigrationKind.Input;
        // Conflicts default to Skip; the UI flips selected ones to Overwrite before Execute.
        var action = destExists ? ItemAction.Skip : ItemAction.Copy;
        return new MigrationItem(name, kind, sourceExists, destExists, action);
    }

    private static bool IsOutputName(string name) =>
        name.StartsWith("output--", StringComparison.Ordinal);

    private static async Task<bool> TryGetAsync(IVault vault, string name, CancellationToken ct)
    {
        try
        {
            _ = await vault.GetSecretAsync(name);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void EnsureEnabled()
    {
        if (!settings.Value.EnableMigrator)
            throw new InvalidOperationException("Secret Migrator is not enabled (SecretStore:EnableMigrator).");
    }

    public async Task<bool> IsAuthorizedAsync(Guid callerUserId, Guid organizationId, CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var isSystemAdmin = await db.Set<UserSystemRoleAssignment>()
            .AnyAsync(r => r.UserId == callerUserId && r.RoleName == SystemRole.Administrator, ct);
        if (isSystemAdmin) return true;

        var isOrgOwner = await db.Set<UserOrganizationRoleAssignment>()
            .AnyAsync(r => r.UserId == callerUserId
                        && r.OrganizationId == organizationId
                        && r.RoleName == OrganizationRole.Owner, ct);
        return isOrgOwner;
    }

    private async Task EnsureAuthorizedAsync(Guid callerUserId, Guid organizationId, CancellationToken ct)
    {
        if (!await IsAuthorizedAsync(callerUserId, organizationId, ct))
            throw new UnauthorizedAccessException("Secret Migrator requires organization Owner or system Administrator.");
    }
}
