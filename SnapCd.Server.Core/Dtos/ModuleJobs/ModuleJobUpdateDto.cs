using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.ModuleJobs;

public class ModuleJobUpdateDto : ModuleJobCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
