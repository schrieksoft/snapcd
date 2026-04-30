using SnapCd.Contracts;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.ParamResolver;

namespace SnapCd.Server.Core.Factories;

public class ParamResolverFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly OutputParamResolverFactory _outputParamResolverFactory;
    private readonly OutputSetParamResolverFactory _outputSetParamResolverFactory;
    private readonly SecretParamResolverFactory _secretParamResolverFactory;


    public ParamResolverFactory(
        OutputParamResolverFactory outputParamResolverFactory,
        OutputSetParamResolverFactory outputSetParamResolverFactory,
        SecretParamResolverFactory secretParamResolverFactory,
        ILoggerFactory loggerFactory
    )
    {
        _outputParamResolverFactory = outputParamResolverFactory;
        _outputSetParamResolverFactory = outputSetParamResolverFactory;
        _loggerFactory = loggerFactory;
        _secretParamResolverFactory = secretParamResolverFactory;
    }

    /// <summary>
    /// Creates a ParamResolver for resolving Params (used in Plan).
    /// Includes OutputSetParamResolver for FromOutputSet resolution.
    /// </summary>
    public ParamResolver<ModuleParamFromOutput> CreateForParams(
        ServerTaskContext context,
        List<ModuleInputFromDefinitionReadDto> fromDefinitionParams,
        List<ModuleInputFromLiteralReadDto> fromLiteralParams,
        List<ModuleInputFromNamespaceReadDto> fromNamespaceParams,
        List<NamespaceInputFromLiteralReadDto> fromLiteralNamespaceParams,
        List<NamespaceInputFromDefinitionReadDto> fromDefinitionNamespaceParams,
        List<SelectedModuleSecret> selectedModuleSecrets,
        List<SelectedNamespaceSecret> selectedNamespaceSecrets,
        // Properties from Declared
        Guid stackId,
        string stackName,
        Guid namespaceId,
        string namespaceName,
        Guid moduleId,
        string moduleName,
        string sourceRevision,
        string sourceUrl,
        string sourceSubdirectory,
        // Organization ID for service calls
        Guid organizationId,
        string engine
    )
    {
        var logger = _loggerFactory.CreateLogger<ParamResolver<ModuleParamFromOutput>>();
        var nsLogger = _loggerFactory.CreateLogger<NamespaceParamResolver>();

        var outputParamResolver = _outputParamResolverFactory.CreateForParams();
        var outputSetParamResolver = _outputSetParamResolverFactory.Create();
        var secretParamResolver = _secretParamResolverFactory.Create();

        return new ParamResolver<ModuleParamFromOutput>(
            context,
            fromDefinitionParams,
            fromLiteralParams,
            fromNamespaceParams,
            fromLiteralNamespaceParams,
            fromDefinitionNamespaceParams,
            outputParamResolver,
            outputSetParamResolver,
            secretParamResolver,
            logger,
            nsLogger,
            selectedModuleSecrets,
            selectedNamespaceSecrets,
            stackId,
            stackName,
            namespaceId,
            namespaceName,
            moduleId,
            moduleName,
            sourceRevision,
            sourceUrl,
            sourceSubdirectory,
            organizationId,
            engine
        );
    }

    /// <summary>
    /// Creates a ParamResolver for resolving EnvVars (used in Init).
    /// Does NOT include OutputSetParamResolver (FromOutputSet is only for Params).
    /// </summary>
    public ParamResolver<ModuleEnvVarFromOutput> CreateForEnvVars(
        ServerTaskContext context,
        List<ModuleInputFromDefinitionReadDto> fromDefinitionParams,
        List<ModuleInputFromLiteralReadDto> fromLiteralParams,
        List<ModuleInputFromNamespaceReadDto> fromNamespaceParams,
        List<NamespaceInputFromLiteralReadDto> fromLiteralNamespaceParams,
        List<NamespaceInputFromDefinitionReadDto> fromDefinitionNamespaceParams,
        List<SelectedModuleSecret> selectedModuleSecrets,
        List<SelectedNamespaceSecret> selectedNamespaceSecrets,
        // Properties from Declared
        Guid stackId,
        string stackName,
        Guid namespaceId,
        string namespaceName,
        Guid moduleId,
        string moduleName,
        string sourceRevision,
        string sourceUrl,
        string sourceSubdirectory,
        // Organization ID for service calls
        Guid organizationId,
        string engine
    )
    {
        var logger = _loggerFactory.CreateLogger<ParamResolver<ModuleEnvVarFromOutput>>();
        var nsLogger = _loggerFactory.CreateLogger<NamespaceParamResolver>();

        var outputParamResolver = _outputParamResolverFactory.CreateForEnvVars();
        var secretParamResolver = _secretParamResolverFactory.Create();

        return new ParamResolver<ModuleEnvVarFromOutput>(
            context,
            fromDefinitionParams,
            fromLiteralParams,
            fromNamespaceParams,
            fromLiteralNamespaceParams,
            fromDefinitionNamespaceParams,
            outputParamResolver,
            null, // No OutputSetParamResolver for EnvVars
            secretParamResolver,
            logger,
            nsLogger,
            selectedModuleSecrets,
            selectedNamespaceSecrets,
            stackId,
            stackName,
            namespaceId,
            namespaceName,
            moduleId,
            moduleName,
            sourceRevision,
            sourceUrl,
            sourceSubdirectory,
            organizationId,
            engine
        );
    }
}
