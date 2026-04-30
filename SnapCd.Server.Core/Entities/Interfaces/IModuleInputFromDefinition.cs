using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IModuleInputFromDefinition
{
    public DefinitionInputType DefinitionName { get; set; }
}