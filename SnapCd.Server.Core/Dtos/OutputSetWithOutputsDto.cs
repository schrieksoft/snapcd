using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos;

public class OutputSetWithOutputsDto: IDto
{
    public Guid Id { get; set;  }
    public Guid ModuleId { get; set; }
    public List<string> CreatedOrUpdatedOutputs { get; set; } = new();
}