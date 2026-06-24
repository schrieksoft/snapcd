// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.GroupMembers;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.AgentSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.IntegrationSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Variables;
using SnapCd.Server.Core.Repositories.System.Secured;

namespace SnapCd.Server.Core.Startup;

public static class SecuredRepositories
{
    public static IServiceCollection AddSnapCdSecuredRepositories(this IServiceCollection services)
    {
        services.AddScoped<StackSecuredRepository>();
        services.AddScoped<ModuleSecuredRepository>();
        services.AddScoped<ModuleSagaSecuredRepository>();
        services.AddScoped<NamespaceSecuredRepository>();
        services.AddScoped<RunnerSecuredRepository>();
        services.AddScoped<ServicePrincipalSecuredRepository>();
        services.AddScoped<UserSystemSecuredRepository>();
        services.AddScoped<OrganizationSystemSecuredRepository>();
        services.AddScoped<OrganizationUserSecuredRepository>();
        services.AddScoped<GroupSecuredRepository>();
        services.AddScoped<GroupGroupMemberSecuredRepository>();
        services.AddScoped<UserGroupMemberSecuredRepository>();
        services.AddScoped<ServicePrincipalGroupMemberSecuredRepository>();
        services.AddScoped<GroupMemberSecuredRepository>();
        services.AddScoped<SourceRefresherPreselectionSecuredRepository>();
        services.AddScoped<ModuleInputSecuredRepository>();
        services.AddScoped<ModuleInputFromDefinitionSecuredRepository<ModuleEnvVarFromDefinition>>();
        services.AddScoped<ModuleInputFromNamespaceSecuredRepository<ModuleEnvVarFromNamespace>>();
        services.AddScoped<ModuleInputFromLiteralSecuredRepository<ModuleEnvVarFromLiteral>>();
        services.AddScoped<ModuleInputFromOutputSecuredRepository<ModuleEnvVarFromOutput>>();
        services.AddScoped<ModuleInputFromDefinitionSecuredRepository<ModuleParamFromDefinition>>();
        services.AddScoped<ModuleInputFromNamespaceSecuredRepository<ModuleParamFromNamespace>>();
        services.AddScoped<ModuleInputFromLiteralSecuredRepository<ModuleParamFromLiteral>>();
        services.AddScoped<ModuleInputFromSecretSecuredRepository<ModuleParamFromSecret>>();
        services.AddScoped<ModuleInputFromSecretSecuredRepository<ModuleEnvVarFromSecret>>();
        services.AddScoped<ModuleInputFromOutputSecuredRepository<ModuleParamFromOutput>>();
        services.AddScoped<ModuleInputFromOutputSetSecuredRepository<ModuleParamFromOutputSet>>();
        services.AddScoped<NamespaceInputSecuredRepository>();
        services.AddScoped<NamespaceInputFromLiteralSecuredRepository<NamespaceParamFromLiteral>>();
        services.AddScoped<NamespaceInputFromLiteralSecuredRepository<NamespaceEnvVarFromLiteral>>();
        services.AddScoped<NamespaceInputFromDefinitionSecuredRepository<NamespaceParamFromDefinition>>();
        services.AddScoped<NamespaceInputFromDefinitionSecuredRepository<NamespaceEnvVarFromDefinition>>();
        services.AddScoped<NamespaceInputFromSecretSecuredRepository<NamespaceParamFromSecret>>();
        services.AddScoped<NamespaceInputFromSecretSecuredRepository<NamespaceEnvVarFromSecret>>();
        services.AddScoped<RunnerStackSupplySecuredRepository>();
        services.AddScoped<RunnerNamespaceSupplySecuredRepository>();
        services.AddScoped<RunnerModuleSupplySecuredRepository>();
        services.AddScoped<AgentStackSupplySecuredRepository>();
        services.AddScoped<AgentNamespaceSupplySecuredRepository>();
        services.AddScoped<AgentModuleSupplySecuredRepository>();
        services.AddScoped<StackSecretSecuredRepository>();
        services.AddScoped<NamespaceSecretSecuredRepository>();
        services.AddScoped<ModuleSecretSecuredRepository>();
        services.AddScoped<SecretSecuredRepository>();
        services.AddScoped<SecretOutputSecuredRepository>();
        services.AddScoped<DependsOnModuleSecuredRepository>();
        services.AddScoped<ModuleExtraFileSecuredRepository>();
        services.AddScoped<NamespaceExtraFileSecuredRepository>();
        services.AddScoped<ModulePulumiFlagSecuredRepository>();
        services.AddScoped<ModulePulumiArrayFlagSecuredRepository>();
        services.AddScoped<NamespacePulumiFlagSecuredRepository>();
        services.AddScoped<NamespacePulumiArrayFlagSecuredRepository>();
        services.AddScoped<ModuleTerraformFlagSecuredRepository>();
        services.AddScoped<ModuleTerraformArrayFlagSecuredRepository>();
        services.AddScoped<NamespaceTerraformFlagSecuredRepository>();
        services.AddScoped<NamespaceTerraformArrayFlagSecuredRepository>();
        services.AddScoped<ModuleHookSecuredRepository>();
        services.AddScoped<NamespaceHookSecuredRepository>();
        services.AddScoped<PreviewFeatureAcceptanceSecuredRepository>();
        services.AddScoped<ModuleJobSecuredRepository>();
        services.AddScoped<ModuleJobApprovalSecuredRepository>();
        services.AddScoped<OutputSetSecuredRepository>();
        services.AddScoped<OutputSecuredRepository>();
        services.AddScoped<LiteralOutputSecuredRepository>();
        services.AddScoped<VariableSetSecuredRepository>();
        services.AddScoped<VariableSecuredRepository>();
        
        services.AddScoped<OrganizationRoleAssignmentSecuredRepository>();
        services.AddScoped<StackRoleAssignmentSecuredRepository>();
        services.AddScoped<NamespaceRoleAssignmentSecuredRepository>();
        services.AddScoped<ModuleRoleAssignmentSecuredRepository>();
        services.AddScoped<RunnerRoleAssignmentSecuredRepository>();
        
        services.AddScoped<UserOrganizationRoleAssignmentSecuredRepository>();
        services.AddScoped<UserStackRoleAssignmentSecuredRepository>();
        services.AddScoped<UserNamespaceRoleAssignmentSecuredRepository>();
        services.AddScoped<UserModuleRoleAssignmentSecuredRepository>();
        services.AddScoped<UserRunnerRoleAssignmentSecuredRepository>();
        
        services.AddScoped<ServicePrincipalOrganizationRoleAssignmentSecuredRepository>();
        services.AddScoped<ServicePrincipalStackRoleAssignmentSecuredRepository>();
        services.AddScoped<ServicePrincipalNamespaceRoleAssignmentSecuredRepository>();
        services.AddScoped<ServicePrincipalModuleRoleAssignmentSecuredRepository>();
        services.AddScoped<ServicePrincipalRunnerRoleAssignmentSecuredRepository>();
        
        services.AddScoped<GroupOrganizationRoleAssignmentSecuredRepository>();
        services.AddScoped<GroupStackRoleAssignmentSecuredRepository>();
        services.AddScoped<GroupNamespaceRoleAssignmentSecuredRepository>();
        services.AddScoped<GroupModuleRoleAssignmentSecuredRepository>();
        services.AddScoped<GroupRunnerRoleAssignmentSecuredRepository>();

        // Agent + AgentConnection: secured Agent + (no secured AgentConnection — internal only)
        services.AddScoped<AgentSecuredRepository>();

        // Mission family secured (4 scopes)
        services.AddScoped<OrganizationMissionSecuredRepository>();
        services.AddScoped<StackMissionSecuredRepository>();
        services.AddScoped<NamespaceMissionSecuredRepository>();
        services.AddScoped<ModuleMissionSecuredRepository>();

        // IntegrationEvent family secured (4 scopes)
        services.AddScoped<OrganizationIntegrationEventSecuredRepository>();
        services.AddScoped<StackIntegrationEventSecuredRepository>();
        services.AddScoped<NamespaceIntegrationEventSecuredRepository>();
        services.AddScoped<ModuleIntegrationEventSecuredRepository>();

        // IntegrationSupply family secured (3 scopes)
        services.AddScoped<IntegrationStackSupplySecuredRepository>();
        services.AddScoped<IntegrationNamespaceSupplySecuredRepository>();
        services.AddScoped<IntegrationModuleSupplySecuredRepository>();

        // AgentRoleAssignment family secured (TPH base + 3 subclasses)
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base.AgentRoleAssignmentSecuredRepository>();
        services.AddScoped<UserAgentRoleAssignmentSecuredRepository>();
        services.AddScoped<ServicePrincipalAgentRoleAssignmentSecuredRepository>();
        services.AddScoped<GroupAgentRoleAssignmentSecuredRepository>();

        return services;
    }
}