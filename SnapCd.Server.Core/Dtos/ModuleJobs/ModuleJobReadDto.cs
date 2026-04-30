using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.ModuleJobs;

public class ModuleJobReadDto : ModuleJobCreateDto, IDto
{
    public Guid Id { get; set; }
}