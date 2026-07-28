// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
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
using SnapCd.Server.Core.Repositories.System.Nonsecured;
using SnapCd.Server.Core.Repositories.System.Secured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.Crud.RoleAssignment;
using SnapCd.Server.Core.Services.Crud.Secrets;
using SnapCd.Server.Core.Services.DependencyGraph;
using SnapCd.Server.Core.Services.IdentityAccess;
using SnapCd.Server.Core.Services.ResolvedConfiguration;

namespace SnapCd.Server.Core.Startup;

public static class Factories
{
    public static IServiceCollection AddSnapCdFactories(this IServiceCollection services)
    {
        services.AddScoped<StackRepositoryFactory>();
        services.AddScoped<StackSecuredRepositoryFactory>();
        services.AddScoped<ModuleRepositoryFactory>();
        services.AddScoped<ModuleSecuredRepositoryFactory>();
        services.AddScoped<ModuleSagaSecuredRepositoryFactory>();
        services.AddScoped<SourceRefresherPreselectionSecuredRepositoryFactory>();
        services.AddScoped<NamespaceRepositoryFactory>();
        services.AddScoped<NamespaceSecuredRepositoryFactory>();
        services.AddScoped<RunnerRepositoryFactory>();
        services.AddScoped<RunnerSecuredRepositoryFactory>();
        services.AddScoped<ModuleSagaRepositoryFactory>();
        services.AddScoped<ModuleSagaSecuredRepositoryFactory>();
        services.AddScoped<ApplyJobSagaRepositoryFactory>();
        services.AddScoped<DestroyJobSagaRepositoryFactory>();
        services.AddScoped<ServicePrincipalRepositoryFactory>();
        services.AddScoped<JobSagaRepositoryFactory>();
        services.AddScoped<OrganizationSecuredRepositoryFactory>();
        services.AddScoped<UserRepositoryFactory>();
        services.AddScoped<UserSecuredRepositoryFactory>();
        services.AddScoped<UserRepositoryFactory>();
        services.AddScoped<UserSecuredRepositoryFactory>();
        services.AddScoped<OrganizationRepositoryFactory>();
        services.AddScoped<OrganizationSecuredRepositoryFactory>();
        services.AddScoped<OrganizationUserRepositoryFactory>();
        services.AddScoped<OrganizationUserSecuredRepositoryFactory>();
        services.AddScoped<ServicePrincipalSecuredRepositoryFactory>();
        services.AddScoped<GroupSecuredRepositoryFactory>();
        services.AddScoped<GroupMemberSecuredRepositoryFactory>();
        services.AddScoped<ServicePrincipalGroupMemberSecuredRepositoryFactory>();
        services.AddScoped<UserGroupMemberSecuredRepositoryFactory>();
        services.AddScoped<GroupGroupMemberSecuredRepositoryFactory>();

        services.AddScoped<OrganizationRoleAssignmentRepositoryFactory>();
        services.AddScoped<StackRoleAssignmentRepositoryFactory>();
        services.AddScoped<NamespaceRoleAssignmentRepositoryFactory>();
        services.AddScoped<ModuleRoleAssignmentRepositoryFactory>();
        services.AddScoped<RunnerRoleAssignmentRepositoryFactory>();
        
        services.AddScoped<UserOrganizationRoleAssignmentRepositoryFactory>();
        services.AddScoped<UserStackRoleAssignmentRepositoryFactory>();
        services.AddScoped<UserNamespaceRoleAssignmentRepositoryFactory>();
        services.AddScoped<UserModuleRoleAssignmentRepositoryFactory>();
        services.AddScoped<UserRunnerRoleAssignmentRepositoryFactory>();
        
        services.AddScoped<ServicePrincipalOrganizationRoleAssignmentRepositoryFactory>();
        services.AddScoped<ServicePrincipalStackRoleAssignmentRepositoryFactory>();
        services.AddScoped<ServicePrincipalNamespaceRoleAssignmentRepositoryFactory>();
        services.AddScoped<ServicePrincipalModuleRoleAssignmentRepositoryFactory>();
        services.AddScoped<ServicePrincipalRunnerRoleAssignmentRepositoryFactory>();
            ;
        services.AddScoped<GroupOrganizationRoleAssignmentRepositoryFactory>();
        services.AddScoped<GroupStackRoleAssignmentRepositoryFactory>();
        services.AddScoped<GroupNamespaceRoleAssignmentRepositoryFactory>();
        services.AddScoped<GroupModuleRoleAssignmentRepositoryFactory>();
        services.AddScoped<GroupRunnerRoleAssignmentRepositoryFactory>();
        
        services.AddScoped<OrganizationRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<StackRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<NamespaceRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<ModuleRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<RunnerRoleAssignmentSecuredRepositoryFactory>();
        
        services.AddScoped<UserOrganizationRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<UserStackRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<UserNamespaceRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<UserModuleRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<UserRunnerRoleAssignmentSecuredRepositoryFactory>();
        
        services.AddScoped<ServicePrincipalOrganizationRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<ServicePrincipalStackRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<ServicePrincipalNamespaceRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<ServicePrincipalModuleRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<ServicePrincipalRunnerRoleAssignmentSecuredRepositoryFactory>();
        
        services.AddScoped<GroupOrganizationRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<GroupStackRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<GroupNamespaceRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<GroupModuleRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<GroupRunnerRoleAssignmentSecuredRepositoryFactory>();
        
        services.AddScoped<NamespaceInputFromDefinitionServiceFactory>();
        services.AddScoped<NamespaceInputFromLiteralServiceFactory>();
        services.AddScoped<NamespaceInputFromSecretServiceFactory>();
        services.AddScoped<ModuleInputFromDefinitionServiceFactory>();
        services.AddScoped<ModuleInputFromLiteralServiceFactory>();
        services.AddScoped<ModuleInputFromNamespaceServiceFactory>();
        services.AddScoped<ModuleInputFromOutputServiceFactory>();
        services.AddScoped<ModuleInputFromOutputSetServiceFactory>();
        services.AddScoped<ModuleInputFromSecretServiceFactory>();
        services.AddScoped<ModuleInputFromDefinitionSecuredRepositoryFactory<ModuleEnvVarFromDefinition>>();
        services.AddScoped<ModuleInputFromNamespaceSecuredRepositoryFactory<ModuleEnvVarFromNamespace>>();
        services.AddScoped<ModuleInputFromLiteralSecuredRepositoryFactory<ModuleEnvVarFromLiteral>>();
        services.AddScoped<ModuleInputFromOutputSecuredRepositoryFactory<ModuleEnvVarFromOutput>>();
        services.AddScoped<ModuleInputFromSecretSecuredRepositoryFactory<ModuleEnvVarFromSecret>>();
        services.AddScoped<ModuleInputFromDefinitionSecuredRepositoryFactory<ModuleParamFromDefinition>>();
        services.AddScoped<ModuleInputFromNamespaceSecuredRepositoryFactory<ModuleParamFromNamespace>>();
        services.AddScoped<ModuleInputFromLiteralSecuredRepositoryFactory<ModuleParamFromLiteral>>();
        services.AddScoped<ModuleInputFromOutputSecuredRepositoryFactory<ModuleParamFromOutput>>();
        services.AddScoped<ModuleInputFromOutputSetSecuredRepositoryFactory<ModuleParamFromOutputSet>>();
        services.AddScoped<ModuleInputFromSecretSecuredRepositoryFactory<ModuleParamFromSecret>>();
        services.AddScoped<NamespaceInputFromLiteralSecuredRepositoryFactory<NamespaceParamFromLiteral>>();
        services.AddScoped<NamespaceInputFromLiteralSecuredRepositoryFactory<NamespaceEnvVarFromLiteral>>();
        services.AddScoped<NamespaceInputFromDefinitionSecuredRepositoryFactory<NamespaceParamFromDefinition>>();
        services.AddScoped<NamespaceInputFromDefinitionSecuredRepositoryFactory<NamespaceEnvVarFromDefinition>>();
        services.AddScoped<NamespaceInputFromSecretSecuredRepositoryFactory<NamespaceParamFromSecret>>();
        services.AddScoped<NamespaceInputFromSecretSecuredRepositoryFactory<NamespaceEnvVarFromSecret>>();
        services.AddScoped<RunnerStackSupplySecuredRepositoryFactory>();
        services.AddScoped<RunnerNamespaceSupplySecuredRepositoryFactory>();
        services.AddScoped<RunnerModuleSupplySecuredRepositoryFactory>();
        services.AddScoped<AgentStackSupplySecuredRepositoryFactory>();
        services.AddScoped<AgentNamespaceSupplySecuredRepositoryFactory>();
        services.AddScoped<AgentModuleSupplySecuredRepositoryFactory>();
        services.AddScoped<StackSecretRepositoryFactory>();
        services.AddScoped<NamespaceSecretRepositoryFactory>();
        services.AddScoped<ModuleSecretRepositoryFactory>();
        services.AddScoped<StackSecretSecuredRepositoryFactory>();
        services.AddScoped<NamespaceSecretSecuredRepositoryFactory>();
        services.AddScoped<ModuleSecretSecuredRepositoryFactory>();
        services.AddScoped<SecretRepositoryFactory>();
        services.AddScoped<SecretSecuredRepositoryFactory>();
        // Concrete factories — kept ungated. SecretMigratorService injects these directly so
        // it can move secrets across SQL <-> AKV boundaries even when the licence has rendered
        // the configured premium backend "off-limits" for normal traffic.
        services.AddScoped<AzureVaultFactory>();
        services.AddScoped<SqlVaultFactory>();
        // Keyed "inner" registration: chooses the configured SecretStoreProvider. Consumed only
        // by LicenseGatedVaultFactory (the public IVaultFactory) — not directly by callers.
        services.AddKeyedScoped<IVaultFactory>(LicenseGatedVaultFactory.InnerKey, (sp, _) =>
        {
            var settings = sp.GetRequiredService<IOptions<SecretStoreSettings>>().Value;
            return settings.Provider switch
            {
                SecretStoreProvider.SqlServer => sp.GetRequiredService<SqlVaultFactory>(),
                _ => sp.GetRequiredService<AzureVaultFactory>(),
            };
        });
        // Public IVaultFactory routes through the licence gate. Hosts register the
        // IPremiumSecretStorePolicy impl that drives the gate decision.
        services.AddScoped<IVaultFactory, LicenseGatedVaultFactory>();
        services.AddScoped<DependsOnModuleSecuredRepositoryFactory>();
        services.AddScoped<ModuleExtraFileSecuredRepositoryFactory>();
        services.AddScoped<NamespaceExtraFileSecuredRepositoryFactory>();
        services.AddScoped<PreviewFeatureAcceptanceSecuredRepositoryFactory>();
        services.AddScoped<UserFavoriteRepositoryFactory>();
        services.AddScoped<UserFavoriteSecuredRepositoryFactory>();
        services.AddScoped<UserColorRepositoryFactory>();
        services.AddScoped<UserColorSecuredRepositoryFactory>();
        services.AddScoped<ModuleTerraformFlagSecuredRepositoryFactory>();
        services.AddScoped<ModuleTerraformArrayFlagSecuredRepositoryFactory>();
        services.AddScoped<NamespaceTerraformFlagSecuredRepositoryFactory>();
        services.AddScoped<NamespaceTerraformArrayFlagSecuredRepositoryFactory>();
        services.AddScoped<ModuleHookSecuredRepositoryFactory>();
        services.AddScoped<NamespaceHookSecuredRepositoryFactory>();
        services.AddScoped<ModulePulumiFlagSecuredRepositoryFactory>();
        services.AddScoped<ModulePulumiArrayFlagSecuredRepositoryFactory>();
        services.AddScoped<NamespacePulumiFlagSecuredRepositoryFactory>();
        services.AddScoped<NamespacePulumiArrayFlagSecuredRepositoryFactory>();
        services.AddScoped<ModuleJobSecuredRepositoryFactory>();
        services.AddScoped<ModuleJobRepositoryFactory>();
        services.AddScoped<ModuleJobApprovalSecuredRepositoryFactory>();
        services.AddScoped<OutputSetSecuredRepositoryFactory>();
        services.AddScoped<OutputSecuredRepositoryFactory>();
        services.AddScoped<VariableSetSecuredRepositoryFactory>();
        services.AddScoped<VariableSecuredRepositoryFactory>();
        services.AddScoped<LiteralOutputRepositoryFactory>();
        services.AddScoped<LiteralOutputSecuredRepositoryFactory>();
        services.AddScoped<SecretOutputRepositoryFactory>();
        services.AddScoped<SecretOutputSecuredRepositoryFactory>();
        services.AddScoped<ApplyJobSagaRepositoryFactory>();
        services.AddScoped<DestroyJobSagaRepositoryFactory>();
        services.AddScoped<OrganizationServiceFactory>();
        services.AddScoped<DependencyGraphServiceFactory>();
        services.AddScoped<DestroyModuleGraphServiceFactory>();
        services.AddScoped<ApplyModuleGraphServiceFactory>();
        services.AddScoped<SecretServiceFactory>();
        services.AddScoped<OutputServiceFactory>();
        services.AddScoped<ResolvedConfigurationServiceFactory>();
        services.AddScoped<JobServiceFactory>();
        services.AddScoped<SecuredJobServiceFactory>();
        services.AddScoped<SourceChangedServiceFactory>();
        services.AddScoped<AccessTokenServiceFactory>();
        services.AddScoped<UserManagerFactory>();
        services.AddScoped<UserManagerFactory<User, IdentityRole<Guid>, SnapCdDbContext>>();
        services.AddScoped<OrganizationRoleAssignmentServiceFactory>();
        services.AddScoped<StackRoleAssignmentServiceFactory>();
        services.AddScoped<NamespaceRoleAssignmentServiceFactory>();
        services.AddScoped<ModuleRoleAssignmentServiceFactory>();
        services.AddScoped<SignInManagerFactory>();
        services.AddScoped<UserLoginServiceFactory>();
        services.AddScoped<IdentityUserAccessorFactory>();
        services.AddScoped<UserManagerFactory<User, IdentityRole<Guid>, SnapCdDbContext>>();
        services.AddScoped<IdentityUserAccessorFactory>();
        services.AddScoped<OutputParamResolverFactory>();
        services.AddScoped<OutputSetParamResolverFactory>();
        services.AddScoped<OutputRepositoryFactory>();
        services.AddScoped<SecretParamResolverFactory>();
        services.AddScoped<RunnerConnectionRepositoryFactory>();
        services.AddScoped<RunnerConnectionJobRepositoryFactory>();

        // Agent + AgentConnection
        services.AddScoped<AgentRepositoryFactory>();
        services.AddScoped<AgentSecuredRepositoryFactory>();
        services.AddScoped<AgentConnectionRepositoryFactory>();
        services.AddScoped<ModuleJobMissionRunRepositoryFactory>();
        services.AddScoped<ModuleJobMissionRunMilestoneRepositoryFactory>();

        // Integrations (Phase 1 — Slack). Codecs are stateless singletons; the registry fans them out by type.
        services.AddScoped<IntegrationRepositoryFactory>();
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Secured.IntegrationSecuredRepositoryFactory>();
        services.AddScoped<Services.Integrations.IntegrationSecretStore>();
        services.AddScoped<Services.Integrations.IntegrationService>();
        services.AddSingleton<Services.Integrations.Codecs.IIntegrationCodec, Services.Integrations.Codecs.SlackCodec>();
        services.AddSingleton<Services.Integrations.Codecs.IIntegrationCodecRegistry, Services.Integrations.Codecs.IntegrationCodecRegistry>();
        // Phase 2 — per-instance display cache (IMemoryCache only, never distributed), invalidated by fanout consumers.
        services.AddMemoryCache();
        services.AddSingleton<Services.Integrations.IntegrationConnectionCache>();
        // Phase 3 — supply (assignments + resolver), mirroring agents.
        services.AddScoped<IntegrationStackSupplyRepositoryFactory>();
        services.AddScoped<IntegrationStackSupplySecuredRepositoryFactory>();
        services.AddScoped<IntegrationNamespaceSupplyRepositoryFactory>();
        services.AddScoped<IntegrationNamespaceSupplySecuredRepositoryFactory>();
        services.AddScoped<IntegrationModuleSupplyRepositoryFactory>();
        services.AddScoped<IntegrationModuleSupplySecuredRepositoryFactory>();
        services.AddScoped<Services.Integrations.IntegrationSupplyService>();
        services.AddScoped<Services.Integrations.IntegrationSupplyResolver>();
        // Phase 3 RBAC — integration role-assignment repos (non-secured + secured) + service.
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.IntegrationRoleAssignmentRepository>();
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.UserIntegrationRoleAssignmentRepository>();
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.ServicePrincipalIntegrationRoleAssignmentRepository>();
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.GroupIntegrationRoleAssignmentRepository>();
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.IntegrationRoleAssignmentSecuredRepository>();
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.UserIntegrationRoleAssignmentSecuredRepository>();
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.ServicePrincipalIntegrationRoleAssignmentSecuredRepository>();
        services.AddScoped<SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.GroupIntegrationRoleAssignmentSecuredRepository>();
        services.AddScoped<SnapCd.Server.Core.Services.Crud.RoleAssignment.IntegrationRoleAssignmentService>();
        // Phase 4 — IntegrationEvent (demand): per-scope repos + service + matcher.
        services.AddScoped<OrganizationIntegrationEventRepositoryFactory>();
        services.AddScoped<OrganizationIntegrationEventSecuredRepositoryFactory>();
        services.AddScoped<StackIntegrationEventRepositoryFactory>();
        services.AddScoped<StackIntegrationEventSecuredRepositoryFactory>();
        services.AddScoped<NamespaceIntegrationEventRepositoryFactory>();
        services.AddScoped<NamespaceIntegrationEventSecuredRepositoryFactory>();
        services.AddScoped<ModuleIntegrationEventRepositoryFactory>();
        services.AddScoped<ModuleIntegrationEventSecuredRepositoryFactory>();
        services.AddScoped<Services.Integrations.IntegrationEventMatcher>();

        // Mission family (4 scopes, raw + secured)
        services.AddScoped<OrganizationMissionRepositoryFactory>();
        services.AddScoped<OrganizationMissionSecuredRepositoryFactory>();
        services.AddScoped<StackMissionRepositoryFactory>();
        services.AddScoped<StackMissionSecuredRepositoryFactory>();
        services.AddScoped<NamespaceMissionRepositoryFactory>();
        services.AddScoped<NamespaceMissionSecuredRepositoryFactory>();
        services.AddScoped<ModuleMissionRepositoryFactory>();
        services.AddScoped<ModuleMissionSecuredRepositoryFactory>();

        // AgentRoleAssignment family (TPH base + 3 subclasses, raw + secured)
        services.AddScoped<AgentRoleAssignmentRepositoryFactory>();
        services.AddScoped<AgentRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<UserAgentRoleAssignmentRepositoryFactory>();
        services.AddScoped<UserAgentRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<ServicePrincipalAgentRoleAssignmentRepositoryFactory>();
        services.AddScoped<ServicePrincipalAgentRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<GroupAgentRoleAssignmentRepositoryFactory>();
        services.AddScoped<GroupAgentRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<AgentRoleAssignmentServiceFactory>();

        // StateStore + StateFile
        services.AddScoped<StateStoreRepositoryFactory>();
        services.AddScoped<StateStoreSecuredRepositoryFactory>();
        services.AddScoped<StateFileRepositoryFactory>();
        services.AddScoped<StateFileSecuredRepositoryFactory>();

        // StateStoreRoleAssignment family (TPH base + 3 subclasses, raw + secured)
        services.AddScoped<StateStoreRoleAssignmentRepositoryFactory>();
        services.AddScoped<StateStoreRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<UserStateStoreRoleAssignmentRepositoryFactory>();
        services.AddScoped<UserStateStoreRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<ServicePrincipalStateStoreRoleAssignmentRepositoryFactory>();
        services.AddScoped<ServicePrincipalStateStoreRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<GroupStateStoreRoleAssignmentRepositoryFactory>();
        services.AddScoped<GroupStateStoreRoleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<StateStoreRoleAssignmentServiceFactory>();

        return services;
    }
}