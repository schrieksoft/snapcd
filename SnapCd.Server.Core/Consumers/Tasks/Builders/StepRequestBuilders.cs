// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
namespace SnapCd.Server.Core.Consumers.Tasks.Builders;

/// <summary>
/// Builds the runner payloads for the four steps both job families share. The consumers are
/// separate per family, because a published reply is routed by message type and each family has
/// its own authorization path; the field mapping is here so it has one home.
/// </summary>
public static class StepRequestBuilders
{
    public static JobMetadata MetadataFor(ResolvedModule declared) => new()
    {
        ModuleName = declared.ModuleName,
        NamespaceName = declared.NamespaceName,
        StackName = declared.StackName,
        ModuleId = declared.ModuleId,
        SourceSubdirectory = declared.SourceSubdirectory
    };

    public static GetModuleRequestBase GetModule(Guid jobId, Guid organizationId, ResolvedModule declared) => new()
    {
        JobId = jobId,
        OrganizationId = organizationId,
        Metadata = MetadataFor(declared),
        SourceType = declared.SourceType,
        SourceRevisionType = declared.SourceRevisionType,
        SourceUrl = declared.SourceUrl,
        SourceRevision = declared.SourceRevision,
        Engine = declared.Engine,
        CleanInitEnabled = declared.CleanInitEnabled,
        ExtraFiles = declared.ExtraFiles
    };

    public static InitRequestBase Init(
        Guid jobId,
        Guid organizationId,
        ResolvedModule declared,
        Dictionary<string, string> resolvedEnvVars) => new()
    {
        JobId = jobId,
        OrganizationId = organizationId,
        Metadata = MetadataFor(declared),
        Engine = declared.Engine,
        InitBeforeHook = declared.InitBeforeHook,
        InitAfterHook = declared.InitAfterHook,
        CleanInitEnabled = declared.CleanInitEnabled,
        ResolvedEnvVars = resolvedEnvVars,
        BackendConfiguration = new EngineBackendConfiguration
        {
            PulumiFlags = declared.PulumiFlags.Where(f => f.Task == PulumiCommandTask.Init).ToList(),
            PulumiArrayFlags = declared.PulumiArrayFlags.Where(f => f.Task == PulumiCommandTask.Init).ToList(),
            TerraformFlags = declared.TerraformFlags.Where(f => f.Task == TerraformCommandTask.Init).ToList(),
            TerraformArrayFlags = declared.TerraformArrayFlags.Where(f => f.Task == TerraformCommandTask.Init).ToList()
        }
    };

    public static ValidateRequestBase Validate(Guid jobId, Guid organizationId, ResolvedModule declared) => new()
    {
        JobId = jobId,
        OrganizationId = organizationId,
        Metadata = MetadataFor(declared),
        Engine = declared.Engine,
        ValidateBeforeHook = declared.ValidateBeforeHook,
        ValidateAfterHook = declared.ValidateAfterHook
    };

    /// <summary>
    /// Resolves the module's environment variables on the server, as the init step needs them
    /// pre-resolved. Shared so the two job families cannot drift on which sources are consulted.
    /// </summary>
    public static async Task<Dictionary<string, string>> ResolveInitEnvVars(
        ParamResolverFactory paramResolverFactory,
        Guid jobId,
        Guid organizationId,
        ResolvedModule declared,
        ILogger logger)
    {
        var taskContext = new ServerTaskContext(jobId, "Init", logger, MetadataFor(declared));

        var paramResolver = paramResolverFactory.CreateForEnvVars(
            taskContext,
            declared.ModuleEnvVarFromDefinitions ?? [],
            declared.ModuleEnvVarFromLiterals ?? [],
            declared.ModuleEnvVarFromNamespaces ?? [],
            declared.NamespaceEnvVarFromLiterals ?? [],
            declared.NamespaceEnvVarFromDefinitions ?? [],
            declared.SelectedModuleEnvVarsFromSecrets,
            declared.SelectedNamespaceEnvVarsFromSecrets,
            declared.StackId,
            declared.StackName,
            declared.NamespaceId,
            declared.NamespaceName,
            declared.ModuleId,
            declared.ModuleName,
            declared.SourceRevision,
            declared.SourceUrl,
            declared.SourceSubdirectory,
            organizationId,
            declared.Engine
        );

        return await paramResolver.ResolveEnvVariables();
    }

    /// <summary>
    /// Resolves the module's parameters on the server, as the plan step needs them pre-resolved.
    /// Shared so the two job families cannot drift on which sources are consulted.
    /// </summary>
    public static async Task<Dictionary<string, string>> ResolvePlanParameters(
        ParamResolverFactory paramResolverFactory,
        Guid jobId,
        Guid organizationId,
        ResolvedModule declared,
        ILogger logger)
    {
        var taskContext = new ServerTaskContext(jobId, "Plan", logger, MetadataFor(declared));

        var paramResolver = paramResolverFactory.CreateForParams(
            taskContext,
            declared.ModuleParamFromDefinitions ?? [],
            declared.ModuleParamFromLiterals ?? [],
            declared.ModuleParamFromNamespaces ?? [],
            declared.NamespaceParamFromLiterals ?? [],
            declared.NamespaceParamFromDefinitions ?? [],
            declared.SelectedModuleParamsFromSecrets,
            declared.SelectedNamespaceParamsFromSecrets,
            declared.StackId,
            declared.StackName,
            declared.NamespaceId,
            declared.NamespaceName,
            declared.ModuleId,
            declared.ModuleName,
            declared.SourceRevision,
            declared.SourceUrl,
            declared.SourceSubdirectory,
            organizationId,
            declared.Engine
        );

        return await paramResolver.ResolveParameters();
    }

    public static PlanRequestBase Plan(
        Guid jobId,
        Guid organizationId,
        ResolvedModule declared,
        Dictionary<string, string> resolvedParameters,
        bool isDestroyJob) => new()
    {
        JobId = jobId,
        OrganizationId = organizationId,
        Metadata = MetadataFor(declared),
        Engine = declared.Engine,
        PlanBeforeHook = declared.PlanBeforeHook,
        PlanAfterHook = declared.PlanAfterHook,
        ResolvedParameters = resolvedParameters,
        PulumiFlags = declared.PulumiFlags.Where(f => f.Task == PulumiCommandTask.Plan).ToList(),
        PulumiArrayFlags = declared.PulumiArrayFlags.Where(f => f.Task == PulumiCommandTask.Plan).ToList(),
        TerraformFlags = declared.TerraformFlags.Where(f => f.Task == TerraformCommandTask.Plan).ToList(),
        TerraformArrayFlags = declared.TerraformArrayFlags.Where(f => f.Task == TerraformCommandTask.Plan).ToList(),
        Policies = PolicyApplicability.ForPlanStep(declared, isDestroyJob)
    };
}
