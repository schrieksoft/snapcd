using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// One row per secret processed by a Secret Migrator run. Grouped by <see cref="RunId"/>.
/// </summary>
public class SecretMigrationAudit : AuditBase
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public DateTime RunStartedUtc { get; set; }

    public Guid OrganizationId { get; set; }
    public Guid ExecutedByUserId { get; set; }

    [Required, MaxLength(64)] public required string Direction { get; set; }
    [Required, MaxLength(512)] public required string Name { get; set; }
    [Required, MaxLength(32)] public required string Action { get; set; }
    [Required, MaxLength(16)] public required string Kind { get; set; }

    [MaxLength(2048)] public string? ErrorMessage { get; set; }
}
