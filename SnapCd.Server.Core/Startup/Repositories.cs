// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.GroupMembers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.AgentSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Variables;
using SnapCd.Server.Core.Repositories.System.Nonsecured;

namespace SnapCd.Server.Core.Startup;

public static class Repositories
{
    public static IServiceCollection AddSnapCdRepositories(this IServiceCollection services)
    {
        // Core Entity Repositories
        services.AddScoped<StackRepository>();
        services.AddScoped<ModuleRepository>();
        services.AddScoped<ModuleSagaRepository>();
        services.AddScoped<NamespaceRepository>();
        services.AddScoped<RunnerRepository>();
        services.AddScoped<ServicePrincipalRepository>();
        services.AddScoped<UserSystemRepository>();
        services.AddScoped<OrganizationSystemRepository>();
        services.AddScoped<OrganizationUserRepository>();
        services.AddScoped<GroupRepository>();
        services.AddScoped<GroupMemberRepository>();
        services.AddScoped<GroupGroupMemberRepository>();
        services.AddScoped<UserGroupMemberRepository>();
        services.AddScoped<ServicePrincipalGroupMemberRepository>();
        services.AddScoped<SourceRefresherPreselectionRepository>();
        services.AddScoped<RunnerConnectionJobRepository>();
        services.AddScoped<NamespaceInputRepository>();
        services.AddScoped<ModuleInputRepository>();
        services.AddScoped<ModuleInputFromDefinitionRepository<ModuleEnvVarFromDefinition>>();
        services.AddScoped<ModuleInputFromNamespaceRepository<ModuleEnvVarFromNamespace>>();
        services.AddScoped<ModuleInputFromLiteralRepository<ModuleEnvVarFromLiteral>>();
        services.AddScoped<ModuleInputFromOutputRepository<ModuleEnvVarFromOutput>>();
        services.AddScoped<ModuleInputFromDefinitionRepository<ModuleParamFromDefinition>>();
        services.AddScoped<ModuleInputFromNamespaceRepository<ModuleParamFromNamespace>>();
        services.AddScoped<ModuleInputFromLiteralRepository<ModuleParamFromLiteral>>();
        services.AddScoped<ModuleInputFromSecretRepository<ModuleParamFromSecret>>();
        services.AddScoped<ModuleInputFromSecretRepository<ModuleEnvVarFromSecret>>();
        services.AddScoped<ModuleInputFromOutputRepository<ModuleParamFromOutput>>();
        services.AddScoped<ModuleInputFromOutputSetRepository<ModuleParamFromOutputSet>>();
        services.AddScoped<NamespaceInputFromLiteralRepository<NamespaceParamFromLiteral>>();
        services.AddScoped<NamespaceInputFromLiteralRepository<NamespaceEnvVarFromLiteral>>();
        services.AddScoped<NamespaceInputFromDefinitionRepository<NamespaceParamFromDefinition>>();
        services.AddScoped<NamespaceInputFromDefinitionRepository<NamespaceEnvVarFromDefinition>>();
        services.AddScoped<NamespaceInputFromSecretRepository<NamespaceParamFromSecret>>();
        services.AddScoped<NamespaceInputFromSecretRepository<NamespaceEnvVarFromSecret>>();
        services.AddScoped<RunnerStackSupplyRepository>();
        services.AddScoped<RunnerNamespaceSupplyRepository>();
        services.AddScoped<RunnerModuleSupplyRepository>();
        services.AddScoped<AgentStackSupplyRepository>();
        services.AddScoped<AgentNamespaceSupplyRepository>();
        services.AddScoped<AgentModuleSupplyRepository>();
        services.AddScoped<StackSecretRepository>();
        services.AddScoped<NamespaceSecretRepository>();
        services.AddScoped<ModuleSecretRepository>();
        services.AddScoped<SecretRepository>();
        services.AddScoped<SecretOutputRepository>();
        services.AddScoped<DependsOnModuleRepository>();
        services.AddScoped<ModuleExtraFileRepository>();
        services.AddScoped<NamespaceExtraFileRepository>();
        services.AddScoped<ModuleAdditionalTriggerPathRepository>();
        services.AddScoped<NamespaceAdditionalTriggerPathRepository>();
        services.AddScoped<ModuleTerraformInlinePolicyRepository>();
        services.AddScoped<ModuleTerraformRemotePolicyRepository>();
        services.AddScoped<ModuleTerraformLocalPolicyRepository>();
        services.AddScoped<ModulePulumiInlinePolicyRepository>();
        services.AddScoped<ModulePulumiRemotePolicyRepository>();
        services.AddScoped<ModulePulumiLocalPolicyRepository>();
        services.AddScoped<NamespaceTerraformInlinePolicyRepository>();
        services.AddScoped<NamespaceTerraformRemotePolicyRepository>();
        services.AddScoped<NamespaceTerraformLocalPolicyRepository>();
        services.AddScoped<NamespacePulumiInlinePolicyRepository>();
        services.AddScoped<NamespacePulumiRemotePolicyRepository>();
        services.AddScoped<NamespacePulumiLocalPolicyRepository>();
        services.AddScoped<ModulePulumiFlagRepository>();
        services.AddScoped<ModulePulumiArrayFlagRepository>();
        services.AddScoped<NamespacePulumiFlagRepository>();
        services.AddScoped<NamespacePulumiArrayFlagRepository>();
        services.AddScoped<ModuleTerraformFlagRepository>();
        services.AddScoped<ModuleTerraformArrayFlagRepository>();
        services.AddScoped<NamespaceTerraformFlagRepository>();
        services.AddScoped<NamespaceTerraformArrayFlagRepository>();
        services.AddScoped<ModuleHookRepository>();
        services.AddScoped<NamespaceHookRepository>();
        services.AddScoped<PreviewFeatureAcceptanceRepository>();
        services.AddScoped<ModuleJobRepository>();
        services.AddScoped<ManualModuleJobRepository>();
        services.AddScoped<ManualModuleJobRepositoryFactory>();
        services.AddScoped<ModuleJobApprovalRepository>();
        services.AddScoped<ApplyJobSagaRepository>();
        services.AddScoped<DestroyJobSagaRepository>();
        services.AddScoped<OutputSetRepository>();
        services.AddScoped<OutputRepository>();
        services.AddScoped<LiteralOutputRepository>();
        services.AddScoped<VariableSetRepository>();
        services.AddScoped<VariableRepository>();
        
        services.AddScoped<OrganizationRoleAssignmentRepository>();
        services.AddScoped<StackRoleAssignmentRepository>();
        services.AddScoped<NamespaceRoleAssignmentRepository>();
        services.AddScoped<ModuleRoleAssignmentRepository>();
        services.AddScoped<RunnerRoleAssignmentRepository>();
        
        services.AddScoped<UserOrganizationRoleAssignmentRepository>();
        services.AddScoped<UserStackRoleAssignmentRepository>();
        services.AddScoped<UserNamespaceRoleAssignmentRepository>();
        services.AddScoped<UserModuleRoleAssignmentRepository>();
        services.AddScoped<UserRunnerRoleAssignmentRepository>();
        
        services.AddScoped<ServicePrincipalOrganizationRoleAssignmentRepository>();
        services.AddScoped<ServicePrincipalStackRoleAssignmentRepository>();
        services.AddScoped<ServicePrincipalNamespaceRoleAssignmentRepository>();
        services.AddScoped<ServicePrincipalModuleRoleAssignmentRepository>();
        services.AddScoped<ServicePrincipalRunnerRoleAssignmentRepository>();
        
        services.AddScoped<GroupOrganizationRoleAssignmentRepository>();
        services.AddScoped<GroupStackRoleAssignmentRepository>();
        services.AddScoped<GroupNamespaceRoleAssignmentRepository>();
        services.AddScoped<GroupModuleRoleAssignmentRepository>();
        services.AddScoped<GroupRunnerRoleAssignmentRepository>();

        // Agent + AgentConnection
        services.AddScoped<AgentRepository>();
        services.AddScoped<AgentConnectionRepository>();

        // Mission family (4 scopes, raw)
        services.AddScoped<OrganizationMissionRepository>();
        services.AddScoped<StackMissionRepository>();
        services.AddScoped<NamespaceMissionRepository>();
        services.AddScoped<ModuleMissionRepository>();

        // IntegrationEvent family (4 scopes, raw)
        services.AddScoped<OrganizationIntegrationEventRepository>();
        services.AddScoped<StackIntegrationEventRepository>();
        services.AddScoped<NamespaceIntegrationEventRepository>();
        services.AddScoped<ModuleIntegrationEventRepository>();

        // IntegrationSupply family (3 scopes, raw)
        services.AddScoped<IntegrationStackSupplyRepository>();
        services.AddScoped<IntegrationNamespaceSupplyRepository>();
        services.AddScoped<IntegrationModuleSupplyRepository>();

        // AgentRoleAssignment family (TPH base + 3 subclasses)
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.Base.AgentRoleAssignmentRepository>();
        services.AddScoped<UserAgentRoleAssignmentRepository>();
        services.AddScoped<ServicePrincipalAgentRoleAssignmentRepository>();
        services.AddScoped<GroupAgentRoleAssignmentRepository>();

        // StateStore + StateFile
        services.AddScoped<StateStoreRepository>();
        services.AddScoped<StateFileRepository>();

        // StateStoreRoleAssignment family (TPH base + 3 subclasses)
        services.AddScoped<StateStoreRoleAssignmentRepository>();
        services.AddScoped<UserStateStoreRoleAssignmentRepository>();
        services.AddScoped<ServicePrincipalStateStoreRoleAssignmentRepository>();
        services.AddScoped<GroupStateStoreRoleAssignmentRepository>();

        return services;
    }
}