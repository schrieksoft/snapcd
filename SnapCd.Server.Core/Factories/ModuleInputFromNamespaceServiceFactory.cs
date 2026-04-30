using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Interfaces;

namespace SnapCd.Server.Core.Factories;

public class ModuleInputFromNamespaceServiceFactory
{
    private readonly ModuleInputFromNamespaceService<ModuleParamFromNamespace> _paramService;
    private readonly ModuleInputFromNamespaceService<ModuleEnvVarFromNamespace> _envVarService;

    public ModuleInputFromNamespaceServiceFactory(
        ModuleInputFromNamespaceService<ModuleParamFromNamespace> paramService,
        ModuleInputFromNamespaceService<ModuleEnvVarFromNamespace> envVarService)
    {
        _paramService = paramService;
        _envVarService = envVarService;
    }

    public IModuleInputFromNamespaceService GetService(InputKind inputKind)
    {
        return inputKind switch
        {
            InputKind.Param => _paramService,
            InputKind.EnvVar => _envVarService,
            _ => throw new ArgumentException($"Unsupported InputKind: {inputKind}")
        };
    }
}