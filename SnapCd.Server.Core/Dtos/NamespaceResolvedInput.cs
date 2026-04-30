using SnapCd.Contracts;

namespace SnapCd.Server.Core.Dtos;

public class NamespaceResolvedInput
{
    public required Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string OriginalValue { get; set; } = string.Empty;

    public string ResolvedValue { get; set; } = string.Empty;

    public NamespaceInputUsageMode UsageMode { get; set; }
    public NamespaceInputSource Source { get; set; }
}