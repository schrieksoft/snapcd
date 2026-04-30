using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Views;

public class JobMetadata
{
    public Guid ExecuteNamespaceSagaId { get; set; }
    public Guid NamespaceJobId { get; set; }
    public Guid ModuleJobId { get; set; }
    public Guid NamespaceId { get; set; }
    public Guid ModuleId { get; set; }
    [MaxLength(255)] public string ModuleName { get; set; } = null!;
    [MaxLength(255)] public string NamespaceName { get; set; } = null!;
    public ExecutionStatus Status { get; set; }
}