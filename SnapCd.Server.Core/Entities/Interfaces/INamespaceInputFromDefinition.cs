using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface INamespaceInputFromDefinition
{
    public DefinitionInputType DefinitionName { get; set; }
}