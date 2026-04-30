using SnapCd.Contracts;

namespace SnapCd.Server.Core.Dtos;

public class ModuleResolvedInput
{
    public string Name { get; set; } = string.Empty;

    public string OriginalValue { get; set; } = string.Empty;

    public string ResolvedValue { get; set; } = string.Empty;

    public bool IsFromNamespaceDefault { get; set; }
    public ModuleInputSource Source { get; set; }
}