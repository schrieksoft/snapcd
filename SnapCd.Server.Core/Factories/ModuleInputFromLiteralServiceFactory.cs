using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Interfaces;

namespace SnapCd.Server.Core.Factories;

public class ModuleInputFromLiteralServiceFactory
{
    private readonly ModuleInputFromLiteralService<ModuleParamFromLiteral> _paramService;
    private readonly ModuleInputFromLiteralService<ModuleEnvVarFromLiteral> _envVarService;

    public ModuleInputFromLiteralServiceFactory(
        ModuleInputFromLiteralService<ModuleParamFromLiteral> paramService,
        ModuleInputFromLiteralService<ModuleEnvVarFromLiteral> envVarService)
    {
        _paramService = paramService;
        _envVarService = envVarService;
    }

    public IModuleInputFromLiteralService GetService(InputKind inputKind)
    {
        return inputKind switch
        {
            InputKind.Param => _paramService,
            InputKind.EnvVar => _envVarService,
            _ => throw new ArgumentException($"Unsupported InputKind: {inputKind}")
        };
    }
}