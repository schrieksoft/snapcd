using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Interfaces;

namespace SnapCd.Server.Core.Factories;

public class NamespaceInputFromSecretServiceFactory
{
    private readonly NamespaceInputFromSecretService<NamespaceParamFromSecret> _paramService;
    private readonly NamespaceInputFromSecretService<NamespaceEnvVarFromSecret> _envVarService;

    public NamespaceInputFromSecretServiceFactory(
        NamespaceInputFromSecretService<NamespaceParamFromSecret> paramService,
        NamespaceInputFromSecretService<NamespaceEnvVarFromSecret> envVarService)
    {
        _paramService = paramService;
        _envVarService = envVarService;
    }

    public INamespaceInputFromSecretService GetService(InputKind inputKind)
    {
        return inputKind switch
        {
            InputKind.Param => _paramService,
            InputKind.EnvVar => _envVarService,
            _ => throw new ArgumentException($"Unsupported InputKind: {inputKind}")
        };
    }
}