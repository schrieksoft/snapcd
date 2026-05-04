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
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;
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
        services.AddScoped<StackSecuredRepositoryFactory>();
        services.AddScoped<ModuleRepositoryFactory>();
        services.AddScoped<ModuleSecuredRepositoryFactory>();
        services.AddScoped<ModuleSagaSecuredRepositoryFactory>();
        services.AddScoped<SourceRefresherPreselectionSecuredRepositoryFactory>();
        services.AddScoped<NamespaceRepositoryFactory>();
        services.AddScoped<NamespaceSecuredRepositoryFactory>();
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
        services.AddScoped<RunnerStackAssignmentSecuredRepositoryFactory>();
        services.AddScoped<RunnerNamespaceAssignmentSecuredRepositoryFactory>();
        services.AddScoped<RunnerModuleAssignmentSecuredRepositoryFactory>();
        services.AddScoped<StackSecretRepositoryFactory>();
        services.AddScoped<NamespaceSecretRepositoryFactory>();
        services.AddScoped<ModuleSecretRepositoryFactory>();
        services.AddScoped<StackSecretSecuredRepositoryFactory>();
        services.AddScoped<NamespaceSecretSecuredRepositoryFactory>();
        services.AddScoped<ModuleSecretSecuredRepositoryFactory>();
        services.AddScoped<SecretRepositoryFactory>();
        services.AddScoped<SecretSecuredRepositoryFactory>();
        services.AddScoped<AzureVaultFactory>();
        services.AddScoped<SqlVaultFactory>();
        // IVaultFactory selects the configured SecretStoreProvider at resolution time.
        services.AddScoped<IVaultFactory>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<SecretStoreSettings>>().Value;
            return settings.Provider switch
            {
                SecretStoreProvider.SqlServer => sp.GetRequiredService<SqlVaultFactory>(),
                _ => sp.GetRequiredService<AzureVaultFactory>(),
            };
        });
        services.AddScoped<DependsOnModuleSecuredRepositoryFactory>();
        services.AddScoped<ModuleExtraFileSecuredRepositoryFactory>();
        services.AddScoped<NamespaceExtraFileSecuredRepositoryFactory>();
        services.AddScoped<PreviewFeatureAcceptanceSecuredRepositoryFactory>();
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
        
        return services;
    }
}