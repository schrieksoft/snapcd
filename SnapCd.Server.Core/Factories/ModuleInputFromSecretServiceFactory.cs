using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Interfaces;

namespace SnapCd.Server.Core.Factories;

public class ModuleInputFromSecretServiceFactory
{
    private readonly ModuleInputFromSecretService<ModuleParamFromSecret> _paramService;
    private readonly ModuleInputFromSecretService<ModuleEnvVarFromSecret> _envVarService;

    public ModuleInputFromSecretServiceFactory(
        ModuleInputFromSecretService<ModuleParamFromSecret> paramService,
        ModuleInputFromSecretService<ModuleEnvVarFromSecret> envVarService)
    {
        _paramService = paramService;
        _envVarService = envVarService;
    }

    public IModuleInputFromSecretService GetService(InputKind inputKind)
    {
        return inputKind switch
        {
            InputKind.Param => _paramService,
            InputKind.EnvVar => _envVarService,
            _ => throw new ArgumentException($"Unsupported InputKind: {inputKind}")
        };
    }
}