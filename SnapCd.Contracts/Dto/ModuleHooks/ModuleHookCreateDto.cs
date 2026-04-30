using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.ModuleHooks;

public class ModuleHookCreateDto
{
    public HookTask Task { get; set; }

    public HookPhase Phase { get; set; }

    [MaxLength(8000)] public string Script { get; set; } = null!;

    public Guid ModuleId { get; set; }
}
