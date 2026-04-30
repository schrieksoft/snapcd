using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Interfaces;

namespace SnapCd.Server.Core.Factories;

public class NamespaceInputFromDefinitionServiceFactory
{
    private readonly NamespaceInputFromDefinitionService<NamespaceParamFromDefinition> _paramService;
    private readonly NamespaceInputFromDefinitionService<NamespaceEnvVarFromDefinition> _envVarService;

    public NamespaceInputFromDefinitionServiceFactory(
        NamespaceInputFromDefinitionService<NamespaceParamFromDefinition> paramService,
        NamespaceInputFromDefinitionService<NamespaceEnvVarFromDefinition> envVarService)
    {
        _paramService = paramService;
        _envVarService = envVarService;
    }

    public INamespaceInputFromDefinitionService GetService(InputKind inputKind)
    {
        return inputKind switch
        {
            InputKind.Param => _paramService,
            InputKind.EnvVar => _envVarService,
            _ => throw new ArgumentException($"Unsupported InputKind: {inputKind}")
        };
    }
}