using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.GroupMembers;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;
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
        services.AddScoped<RunnerStackAssignmentSecuredRepository>();
        services.AddScoped<RunnerNamespaceAssignmentSecuredRepository>();
        services.AddScoped<RunnerModuleAssignmentSecuredRepository>();
        services.AddScoped<StackSecretSecuredRepository>();
        services.AddScoped<NamespaceSecretSecuredRepository>();
        services.AddScoped<ModuleSecretSecuredRepository>();
        services.AddScoped<SecretSecuredRepository>();
        services.AddScoped<SecretOutputSecuredRepository>();
        services.AddScoped<DependsOnModuleSecuredRepository>();
        services.AddScoped<ModuleExtraFileSecuredRepository>();
        services.AddScoped<NamespaceExtraFileSecuredRepository>();
        services.AddScoped<ModuleBackendConfigSecuredRepository>();
        services.AddScoped<NamespaceBackendConfigSecuredRepository>();
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

        return services;
    }
}