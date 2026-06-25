// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Startup;

public static class RepositorySettings
{
    public static IServiceCollection AddSnapCdRepositorySettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DependsOnModuleRepositorySettings>(configuration.GetSection("Repositories:DependsOnModule"));
        services.Configure<ModuleJobMissionRunRepositorySettings>(configuration.GetSection("Repositories:ModuleJobMissionRun"));
        services.Configure<ModuleJobMissionRunMilestoneRepositorySettings>(configuration.GetSection("Repositories:ModuleJobMissionRunMilestone"));
        services.Configure<IntegrationRepositorySettings>(configuration.GetSection("Repositories:Integration"));
        services.Configure<IntegrationModuleSupplyRepositorySettings>(configuration.GetSection("Repositories:IntegrationModuleSupply"));
        services.Configure<IntegrationNamespaceSupplyRepositorySettings>(configuration.GetSection("Repositories:IntegrationNamespaceSupply"));
        services.Configure<IntegrationStackSupplyRepositorySettings>(configuration.GetSection("Repositories:IntegrationStackSupply"));
        services.Configure<IntegrationRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:IntegrationRoleAssignment"));
        services.Configure<IntegrationEventRepositorySettings>(configuration.GetSection("Repositories:IntegrationEvent"));
        services.Configure<OrganizationIntegrationEventRepositorySettings>(configuration.GetSection("Repositories:OrganizationIntegrationEvent"));
        services.Configure<StackIntegrationEventRepositorySettings>(configuration.GetSection("Repositories:StackIntegrationEvent"));
        services.Configure<NamespaceIntegrationEventRepositorySettings>(configuration.GetSection("Repositories:NamespaceIntegrationEvent"));
        services.Configure<ModuleIntegrationEventRepositorySettings>(configuration.GetSection("Repositories:ModuleIntegrationEvent"));
        services.Configure<GroupGroupMemberRepositorySettings>(configuration.GetSection("Repositories:GroupGroupMember"));
        services.Configure<GroupMemberRepositorySettings>(configuration.GetSection("Repositories:GroupMember"));
        services.Configure<GroupModuleRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:GroupModuleRoleAssignment"));
        services.Configure<GroupNamespaceRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:GroupNamespaceRoleAssignment"));
        services.Configure<GroupOrganizationRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:GroupOrganizationRoleAssignment"));
        services.Configure<GroupRepositorySettings>(configuration.GetSection("Repositories:Group"));
        services.Configure<GroupRunnerRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:GroupRunnerRoleAssignment"));
        services.Configure<GroupStackRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:GroupStackRoleAssignment"));
        services.Configure<ModulePulumiFlagRepositorySettings>(configuration.GetSection("Repositories:ModulePulumiFlag"));
        services.Configure<ModulePulumiArrayFlagRepositorySettings>(configuration.GetSection("Repositories:ModulePulumiArrayFlag"));
        services.Configure<ModuleTerraformFlagRepositorySettings>(configuration.GetSection("Repositories:ModuleTerraformFlag"));
        services.Configure<ModuleTerraformArrayFlagRepositorySettings>(configuration.GetSection("Repositories:ModuleTerraformArrayFlag"));
        services.Configure<ModuleHookRepositorySettings>(configuration.GetSection("Repositories:ModuleHook"));
        services.Configure<ModuleExtraFileRepositorySettings>(configuration.GetSection("Repositories:ModuleExtraFile"));
        services.Configure<ModuleInputFromDefinitionRepositorySettings>(configuration.GetSection("Repositories:ModuleInputFromDefinition"));
        services.Configure<ModuleInputFromLiteralRepositorySettings>(configuration.GetSection("Repositories:ModuleInputFromLiteral"));
        services.Configure<ModuleInputFromNamespaceRepositorySettings>(configuration.GetSection("Repositories:ModuleInputFromNamespace"));
        services.Configure<ModuleInputFromOutputRepositorySettings>(configuration.GetSection("Repositories:ModuleInputFromOutput"));
        services.Configure<ModuleInputFromOutputSetRepositorySettings>(configuration.GetSection("Repositories:ModuleInputFromOutputSet"));
        services.Configure<ModuleInputFromSecretRepositorySettings>(configuration.GetSection("Repositories:ModuleInputFromSecret"));
        services.Configure<ModuleInputRepositorySettings>(configuration.GetSection("Repositories:ModuleInput"));
        services.Configure<ModuleJobApprovalRepositorySettings>(configuration.GetSection("Repositories:ModuleJobApproval"));
        services.Configure<ModuleJobRepositorySettings>(configuration.GetSection("Repositories:ModuleJob"));
        services.Configure<ModuleRepositorySettings>(configuration.GetSection("Repositories:Module"));
        services.Configure<ModuleRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:ModuleRoleAssignment"));
        services.Configure<ModuleSecretRepositorySettings>(configuration.GetSection("Repositories:ModuleSecret"));
        services.Configure<NamespacePulumiFlagRepositorySettings>(configuration.GetSection("Repositories:NamespacePulumiFlag"));
        services.Configure<NamespacePulumiArrayFlagRepositorySettings>(configuration.GetSection("Repositories:NamespacePulumiArrayFlag"));
        services.Configure<NamespaceTerraformFlagRepositorySettings>(configuration.GetSection("Repositories:NamespaceTerraformFlag"));
        services.Configure<NamespaceTerraformArrayFlagRepositorySettings>(configuration.GetSection("Repositories:NamespaceTerraformArrayFlag"));
        services.Configure<NamespaceHookRepositorySettings>(configuration.GetSection("Repositories:NamespaceHook"));
        services.Configure<NamespaceExtraFileRepositorySettings>(configuration.GetSection("Repositories:NamespaceExtraFile"));
        services.Configure<NamespaceInputFromDefinitionRepositorySettings>(configuration.GetSection("Repositories:NamespaceInputFromDefinition"));
        services.Configure<NamespaceInputFromLiteralRepositorySettings>(configuration.GetSection("Repositories:NamespaceInputFromLiteral"));
        services.Configure<NamespaceInputFromSecretRepositorySettings>(configuration.GetSection("Repositories:NamespaceInputFromSecret"));
        services.Configure<NamespaceInputRepositorySettings>(configuration.GetSection("Repositories:NamespaceInput"));
        services.Configure<NamespaceRepositorySettings>(configuration.GetSection("Repositories:Namespace"));
        services.Configure<NamespaceRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:NamespaceRoleAssignment"));
        services.Configure<NamespaceSecretRepositorySettings>(configuration.GetSection("Repositories:NamespaceSecret"));
        services.Configure<OrganizationRepositorySettings>(configuration.GetSection("Repositories:Organization"));
        services.Configure<OrganizationRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:OrganizationRoleAssignment"));
        services.Configure<OrganizationUserRepositorySettings>(configuration.GetSection("Repositories:OrganizationUser"));
        services.Configure<PreviewFeatureAcceptanceRepositorySettings>(configuration.GetSection("Repositories:PreviewFeatureAcceptance"));
        services.Configure<OutputRepositorySettings>(configuration.GetSection("Repositories:Output"));
        services.Configure<OutputSetRepositorySettings>(configuration.GetSection("Repositories:OutputSet"));
        services.Configure<RoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:RoleAssignment"));
        services.Configure<RunnerConnectionJobRepositorySettings>(configuration.GetSection("Repositories:RunnerConnectionJob"));
        services.Configure<RunnerConnectionRepositorySettings>(configuration.GetSection("Repositories:RunnerConnection"));
        services.Configure<RunnerModuleSupplyRepositorySettings>(configuration.GetSection("Repositories:RunnerModuleSupply"));
        services.Configure<RunnerNamespaceSupplyRepositorySettings>(configuration.GetSection("Repositories:RunnerNamespaceSupply"));
        services.Configure<RunnerRepositorySettings>(configuration.GetSection("Repositories:Runner"));
        services.Configure<RunnerRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:RunnerRoleAssignment"));
        services.Configure<RunnerStackSupplyRepositorySettings>(configuration.GetSection("Repositories:RunnerStackSupply"));
        services.Configure<AgentModuleSupplyRepositorySettings>(configuration.GetSection("Repositories:AgentModuleSupply"));
        services.Configure<AgentNamespaceSupplyRepositorySettings>(configuration.GetSection("Repositories:AgentNamespaceSupply"));
        services.Configure<AgentStackSupplyRepositorySettings>(configuration.GetSection("Repositories:AgentStackSupply"));
        services.Configure<SecretRepositorySettings>(configuration.GetSection("Repositories:Secret"));
        services.Configure<ServicePrincipalGroupMemberRepositorySettings>(configuration.GetSection("Repositories:ServicePrincipalGroupMember"));
        services.Configure<ServicePrincipalModuleRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:ServicePrincipalModuleRoleAssignment"));
        services.Configure<ServicePrincipalNamespaceRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:ServicePrincipalNamespaceRoleAssignment"));
        services.Configure<ServicePrincipalOrganizationRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:ServicePrincipalOrganizationRoleAssignment"));
        services.Configure<ServicePrincipalRepositorySettings>(configuration.GetSection("Repositories:ServicePrincipal"));
        services.Configure<ServicePrincipalRunnerRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:ServicePrincipalRunnerRoleAssignment"));
        services.Configure<ServicePrincipalStackRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:ServicePrincipalStackRoleAssignment"));
        services.Configure<SourceRefresherPreselectionRepositorySettings>(configuration.GetSection("Repositories:SourceRefresherPreselection"));
        services.Configure<StackRepositorySettings>(configuration.GetSection("Repositories:Stack"));
        services.Configure<StackRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:StackRoleAssignment"));
        services.Configure<StackSecretRepositorySettings>(configuration.GetSection("Repositories:StackSecret"));
        services.Configure<UserGroupMemberRepositorySettings>(configuration.GetSection("Repositories:UserGroupMember"));
        services.Configure<UserModuleRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:UserModuleRoleAssignment"));
        services.Configure<UserNamespaceRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:UserNamespaceRoleAssignment"));
        services.Configure<UserOrganizationRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:UserOrganizationRoleAssignment"));
        services.Configure<UserRepositorySettings>(configuration.GetSection("Repositories:User"));
        services.Configure<UserRunnerRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:UserRunnerRoleAssignment"));
        services.Configure<UserStackRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:UserStackRoleAssignment"));
        services.Configure<VariableRepositorySettings>(configuration.GetSection("Repositories:Variable"));
        services.Configure<VariableSetRepositorySettings>(configuration.GetSection("Repositories:VariableSet"));

        // Agent + AgentConnection
        services.Configure<AgentRepositorySettings>(configuration.GetSection("Repositories:Agent"));
        services.Configure<AgentConnectionRepositorySettings>(configuration.GetSection("Repositories:AgentConnection"));

        // Mission family (4 scopes)
        services.Configure<OrganizationMissionRepositorySettings>(configuration.GetSection("Repositories:OrganizationMission"));
        services.Configure<StackMissionRepositorySettings>(configuration.GetSection("Repositories:StackMission"));
        services.Configure<NamespaceMissionRepositorySettings>(configuration.GetSection("Repositories:NamespaceMission"));
        services.Configure<ModuleMissionRepositorySettings>(configuration.GetSection("Repositories:ModuleMission"));

        // AgentRoleAssignment family (TPH base + 3 subclasses)
        services.Configure<AgentRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:AgentRoleAssignment"));
        services.Configure<UserAgentRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:UserAgentRoleAssignment"));
        services.Configure<ServicePrincipalAgentRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:ServicePrincipalAgentRoleAssignment"));
        services.Configure<GroupAgentRoleAssignmentRepositorySettings>(configuration.GetSection("Repositories:GroupAgentRoleAssignment"));

        return services;
    }
}