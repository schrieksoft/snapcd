// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.SecretMigrator;

public enum MigrationDirection
{
    SqlToAzureKeyVault,
    AzureKeyVaultToSql
}

public enum MigrationKind
{
    Input,
    Output
}

public enum ItemAction
{
    Copy,       // source exists, dest does not
    Overwrite,  // both exist, policy Overwrite
    Skip        // both exist, policy Skip
}

public record MigrationItem(
    string Name,
    MigrationKind Kind,
    bool SourceExists,
    bool DestExists,
    ItemAction Action);

public record MigrationPlan(
    Guid RunId,
    MigrationDirection Direction,
    IReadOnlyList<MigrationItem> Items,
    string InputVaultUrl,
    string OutputVaultUrl);

public record MigrationProgress(
    int Total,
    int Processed,
    int Copied,
    int Overwritten,
    int Skipped,
    int Failed,
    string? CurrentName,
    string? LastError);

public enum PlanPhase
{
    ListingSource,
    ProbingDestination,
    Done
}

public record PlanProgress(
    PlanPhase Phase,
    int Processed,
    int? KnownTotal,
    string? CurrentName);
