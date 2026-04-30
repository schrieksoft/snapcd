using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Interfaces;

namespace SnapCd.Server.Core.Factories;

public class ModuleInputFromDefinitionServiceFactory
{
    private readonly ModuleInputFromDefinitionService<ModuleParamFromDefinition> _paramService;
    private readonly ModuleInputFromDefinitionService<ModuleEnvVarFromDefinition> _envVarService;

    public ModuleInputFromDefinitionServiceFactory(
        ModuleInputFromDefinitionService<ModuleParamFromDefinition> paramService,
        ModuleInputFromDefinitionService<ModuleEnvVarFromDefinition> envVarService)
    {
        _paramService = paramService;
        _envVarService = envVarService;
    }

    public IModuleInputFromDefinitionService GetService(InputKind inputKind)
    {
        return inputKind switch
        {
            InputKind.Param => _paramService,
            InputKind.EnvVar => _envVarService,
            _ => throw new ArgumentException($"Unsupported InputKind: {inputKind}")
        };
    }
}