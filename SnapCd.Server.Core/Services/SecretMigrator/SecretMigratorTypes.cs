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
