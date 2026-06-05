// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.RoleAssignment;
using SnapCd.Server.Core.Services.Crud.Secrets;
using SnapCd.Server.Core.Services.Crud.Secrets.Scoped;

namespace SnapCd.Server.Core.Startup;

public static class CrudService
{
    public static IServiceCollection AddSnapCdCrudServices(this IServiceCollection services)
    {
        // Core Domain Services
        services.AddScoped<StackService>();
        services.AddScoped<ModuleService>();
        services.AddScoped<NamespaceService>();
        services.AddScoped<RunnerService>();
        services.AddScoped<AgentService>();
        services.AddScoped<OrganizationMissionService>();
        services.AddScoped<StackMissionService>();
        services.AddScoped<NamespaceMissionService>();
        services.AddScoped<ModuleMissionService>();
        services.AddScoped<SourceRefresherPreselectionService>();
        services.AddScoped<ModuleInputFromDefinitionBaseService>();
        services.AddScoped<ModuleInputFromLiteralBaseService>();
        services.AddScoped<ModuleInputFromNamespaceBaseService>();
        services.AddScoped<ModuleInputFromOutputBaseService>();
        services.AddScoped<ModuleInputFromOutputSetBaseService>();
        services.AddScoped<ModuleInputFromSecretBaseService>();
        services.AddScoped<NamespaceInputFromDefinitionBaseService>();
        services.AddScoped<NamespaceInputFromLiteralBaseService>();
        services.AddScoped<NamespaceInputFromSecretBaseService>();
        services.AddScoped<ModuleInputFromDefinitionService<ModuleEnvVarFromDefinition>>();
        services.AddScoped<ModuleInputFromNamespaceService<ModuleEnvVarFromNamespace>>();
        services.AddScoped<ModuleInputFromLiteralService<ModuleEnvVarFromLiteral>>();
        services.AddScoped<ModuleInputFromOutputService<ModuleEnvVarFromOutput>>();
        services.AddScoped<ModuleInputFromDefinitionService<ModuleParamFromDefinition>>();
        services.AddScoped<ModuleInputFromNamespaceService<ModuleParamFromNamespace>>();
        services.AddScoped<ModuleInputFromLiteralService<ModuleParamFromLiteral>>();
        services.AddScoped<ModuleInputFromSecretService<ModuleParamFromSecret>>();
        services.AddScoped<ModuleInputFromSecretService<ModuleEnvVarFromSecret>>();
        services.AddScoped<ModuleInputFromOutputService<ModuleParamFromOutput>>();
        services.AddScoped<ModuleInputFromOutputSetService<ModuleParamFromOutputSet>>();
        services.AddScoped<NamespaceInputFromLiteralService<NamespaceParamFromLiteral>>();
        services.AddScoped<NamespaceInputFromLiteralService<NamespaceEnvVarFromLiteral>>();
        services.AddScoped<NamespaceInputFromDefinitionService<NamespaceParamFromDefinition>>();
        services.AddScoped<NamespaceInputFromDefinitionService<NamespaceEnvVarFromDefinition>>();
        services.AddScoped<NamespaceInputFromSecretService<NamespaceParamFromSecret>>();
        services.AddScoped<NamespaceInputFromSecretService<NamespaceEnvVarFromSecret>>();
        services.AddScoped<ServicePrincipalService>();
        services.AddScoped<ServicePrincipalServiceFactory>();
        services.AddScoped<GroupService>();
        services.AddScoped<GroupMemberService>();
        services.AddScoped<GroupMemberServiceFactory>();
        services.AddScoped<OrganizationUserService>();
        services.AddScoped<OrganizationRoleAssignmentService>();
        services.AddScoped<StackRoleAssignmentService>();
        services.AddScoped<NamespaceRoleAssignmentService>();
        services.AddScoped<ModuleRoleAssignmentService>();
        services.AddScoped<RunnerRoleAssignmentService>();
        services.AddScoped<AgentRoleAssignmentService>();
        services.AddScoped<RunnerStackAssignmentService>();
        services.AddScoped<RunnerNamespaceAssignmentService>();
        services.AddScoped<RunnerModuleAssignmentService>();
        services.AddScoped<AgentStackAssignmentService>();
        services.AddScoped<AgentNamespaceAssignmentService>();
        services.AddScoped<AgentModuleAssignmentService>();
        services.AddScoped<SecretService>();
        services.AddScoped<StackSecretService>();
        services.AddScoped<NamespaceSecretService>();
        services.AddScoped<ModuleSecretService>();
        services.AddScoped<DependsOnModuleService>();
        services.AddScoped<ModuleExtraFileService>();
        services.AddScoped<NamespaceExtraFileService>();
        services.AddScoped<ModulePulumiFlagService>();
        services.AddScoped<ModulePulumiArrayFlagService>();
        services.AddScoped<NamespacePulumiFlagService>();
        services.AddScoped<NamespacePulumiArrayFlagService>();
        services.AddScoped<ModuleTerraformFlagService>();
        services.AddScoped<ModuleTerraformArrayFlagService>();
        services.AddScoped<NamespaceTerraformFlagService>();
        services.AddScoped<NamespaceTerraformArrayFlagService>();
        services.AddScoped<ModuleHookService>();
        services.AddScoped<NamespaceHookService>();
        services.AddScoped<PreviewFeatureAcceptanceService>();
        services.AddScoped<OutputSetService>();
        services.AddScoped<OutputService>();
        services.AddScoped<VariableSetService>();
        services.AddScoped<VariableService>();

        return services;
    }
}