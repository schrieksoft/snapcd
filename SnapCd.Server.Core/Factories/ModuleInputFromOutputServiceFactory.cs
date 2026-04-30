using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Interfaces;

namespace SnapCd.Server.Core.Factories;

public class ModuleInputFromOutputServiceFactory
{
    private readonly ModuleInputFromOutputService<ModuleParamFromOutput> _paramService;
    private readonly ModuleInputFromOutputService<ModuleEnvVarFromOutput> _envVarService;

    public ModuleInputFromOutputServiceFactory(
        ModuleInputFromOutputService<ModuleParamFromOutput> paramService,
        ModuleInputFromOutputService<ModuleEnvVarFromOutput> envVarService)
    {
        _paramService = paramService;
        _envVarService = envVarService;
    }

    public IModuleInputFromOutputService GetService(InputKind inputKind)
    {
        return inputKind switch
        {
            InputKind.Param => _paramService,
            InputKind.EnvVar => _envVarService,
            _ => throw new ArgumentException($"Unsupported InputKind: {inputKind}")
        };
    }
}