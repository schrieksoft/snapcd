using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Interfaces;

namespace SnapCd.Server.Core.Factories;

public class NamespaceInputFromLiteralServiceFactory
{
    private readonly NamespaceInputFromLiteralService<NamespaceParamFromLiteral> _paramService;
    private readonly NamespaceInputFromLiteralService<NamespaceEnvVarFromLiteral> _envVarService;

    public NamespaceInputFromLiteralServiceFactory(
        NamespaceInputFromLiteralService<NamespaceParamFromLiteral> paramService,
        NamespaceInputFromLiteralService<NamespaceEnvVarFromLiteral> envVarService)
    {
        _paramService = paramService;
        _envVarService = envVarService;
    }

    public INamespaceInputFromLiteralService GetService(InputKind inputKind)
    {
        return inputKind switch
        {
            InputKind.Param => _paramService,
            InputKind.EnvVar => _envVarService,
            _ => throw new ArgumentException($"Unsupported InputKind: {inputKind}")
        };
    }
}