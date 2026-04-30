using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Interfaces;

namespace SnapCd.Server.Core.Factories;

public class ModuleInputFromOutputSetServiceFactory
{
    private readonly ModuleInputFromOutputSetService<ModuleParamFromOutputSet> _paramService;

    public ModuleInputFromOutputSetServiceFactory(
        ModuleInputFromOutputSetService<ModuleParamFromOutputSet> paramService)
    {
        _paramService = paramService;
    }

    public IModuleInputFromOutputSetService GetService(InputKind inputKind)
    {
        return inputKind switch
        {
            InputKind.Param => _paramService,
            _ => throw new ArgumentException($"Unsupported InputKind for OutputSet: {inputKind}. Only Param is supported.")
        };
    }
}