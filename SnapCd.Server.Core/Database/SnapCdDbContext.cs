// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Linq.Expressions;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database.ClassMaps;
using SnapCd.Server.Core.Database.ClassMaps.GroupMembers;
using SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org;
using SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.System;
using SnapCd.Server.Core.Database.ClassMaps.Secrets;
using SnapCd.Server.Core.Database.ClassMaps.Secrets.Scoped;
using SnapCd.Server.Core.Database.SagaClassMaps;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.Missions;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Agent.Base;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Views;
using ExceptionProcessorSqlServer = EntityFramework.Exceptions.SqlServer.ExceptionProcessorExtensions;
using Authorization = SnapCd.Server.Core.Entities.Definition.Authorization;
using Definition_User = SnapCd.Server.Core.Entities.Definition.User;
using Stack = SnapCd.Server.Core.Entities.Definition.Stack;

namespace SnapCd.Server.Core.Database;

public class SnapCdDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public SnapCdDbContext(DbContextOptions<SnapCdDbContext> options)
        : base(options)
    {
    }

    protected SnapCdDbContext(DbContextOptions options)
        : base(options)
    {
    }






    public DbSet<DestroyJobSaga> DestroyJobSagas { get; set; }
    public DbSet<ApplyJobSaga> ApplyJobSagas { get; set; }
    public DbSet<ModuleSaga> ModuleSagas { get; set; }
    public DbSet<SourceRefresherPreselection> SourceRefresherPreselections { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationUser> OrganizationUsers { get; set; }
    public DbSet<Stack> Stacks { get; set; }
    public DbSet<Namespace> Namespaces { get; set; }
    public DbSet<Module> Modules { get; set; }

    // Views for dependency graph
    public DbSet<Dependency> Dependencies { get; set; }
    public DbSet<RecursiveApplyDependency> RecursiveApplyDependencies { get; set; }
    public DbSet<RecursiveDestroyDependency> RecursiveDestroyDependencies { get; set; }

    // Views for group organizationUser
    public DbSet<RecursiveGroupMember> RecursiveGroupMembers { get; set; }


    public DbSet<NamespaceInput> NamespaceInputs { get; set; }

    // public DbSet<NamespaceInputFromDefinition> NamespaceInputFromDefinitions { get; set; }
    // public DbSet<NamespaceInputFromLiteral> NamespaceInputFromLiterals { get; set; }
    // public DbSet<NamespaceInputFromSecret> NamespaceInputFromSecrets { get; set; }

    public DbSet<NamespaceParamFromDefinition> NamespaceParamFromDefinitions { get; set; }
    public DbSet<NamespaceParamFromLiteral> NamespaceParamFromLiterals { get; set; }
    public DbSet<NamespaceParamFromSecret> NamespaceParamFromSecrets { get; set; }

    public DbSet<NamespaceEnvVarFromDefinition> NamespaceEnvVarFromDefinitions { get; set; }
    public DbSet<NamespaceEnvVarFromLiteral> NamespaceEnvVarFromLiterals { get; set; }
    public DbSet<NamespaceEnvVarFromSecret> NamespaceEnvVarFromSecrets { get; set; }


    public DbSet<ModuleInput> ModuleInputs { get; set; }
    public DbSet<ModuleExtraFile> ModuleExtraFiles { get; set; }
    public DbSet<NamespaceExtraFile> NamespaceExtraFiles { get; set; }
    public DbSet<DependsOnModule> DependsOnModules { get; set; }


    // public DbSet<ModuleInputFromDefinition> ModuleInputFromDefinitions { get; set; }
    // public DbSet<ModuleInputFromLiteral> ModuleInputFromLiterals { get; set; }
    // public DbSet<ModuleInputFromNamespace> ModuleInputFromNamespaces { get; set; }
    // public DbSet<ModuleInputFromOutput> ModuleInputFromOutputs { get; set; }
    // public DbSet<ModuleInputFromOutputSet> ModuleInputFromOutputSets { get; set; }
    // public DbSet<ModuleInputFromSecret> ModuleInputFromSecrets { get; set; }


    public DbSet<ModuleEnvVarFromDefinition> ModuleEnvVarFromDefinitions { get; set; }
    public DbSet<ModuleEnvVarFromLiteral> ModuleEnvVarFromLiterals { get; set; }
    public DbSet<ModuleEnvVarFromNamespace> ModuleEnvVarFromNamespaces { get; set; }
    public DbSet<ModuleEnvVarFromOutput> ModuleEnvVarFromOutputs { get; set; }
    public DbSet<ModuleEnvVarFromSecret> ModuleEnvVarFromSecrets { get; set; }


    public DbSet<ModuleParamFromDefinition> ModuleParamFromDefinitions { get; set; }
    public DbSet<ModuleParamFromLiteral> ModuleParamFromLiterals { get; set; }
    public DbSet<ModuleParamFromNamespace> ModuleParamFromNamespaces { get; set; }
    public DbSet<ModuleParamFromOutput> ModuleParamFromOutputs { get; set; }
    public DbSet<ModuleParamFromOutputSet> ModuleParamFromOutputSets { get; set; }
    public DbSet<ModuleParamFromSecret> ModuleParamFromSecrets { get; set; }

    public DbSet<ModuleJob> ModuleJobs { get; set; }
    public DbSet<ModuleJobApproval> ModuleJobApprovals { get; set; }
    public DbSet<ModuleJobMission> ModuleJobMissions { get; set; }
    public DbSet<ModuleJobMissionRun> ModuleJobMissionRuns { get; set; }
    public DbSet<ModuleJobMissionRunMilestone> ModuleJobMissionRunMilestones { get; set; }
    public DbSet<Integration> Integrations { get; set; }
    public DbSet<Entities.Definition.IntegrationSupplies.IntegrationStackSupply> IntegrationStackSupplies { get; set; }
    public DbSet<Entities.Definition.IntegrationSupplies.IntegrationNamespaceSupply> IntegrationNamespaceSupplies { get; set; }
    public DbSet<Entities.Definition.IntegrationSupplies.IntegrationModuleSupply> IntegrationModuleSupplies { get; set; }
    public DbSet<Entities.Definition.RoleAssignments.Org.Integration.Base.IntegrationRoleAssignment> IntegrationRoleAssignments { get; set; }
    public DbSet<Entities.Definition.RoleAssignments.Org.UserIntegrationRoleAssignment> UserIntegrationRoleAssignments { get; set; }
    public DbSet<Entities.Definition.RoleAssignments.Org.ServicePrincipalIntegrationRoleAssignment> ServicePrincipalIntegrationRoleAssignments { get; set; }
    public DbSet<Entities.Definition.RoleAssignments.Org.GroupIntegrationRoleAssignment> GroupIntegrationRoleAssignments { get; set; }
    public DbSet<Entities.Definition.IntegrationEvents.OrganizationIntegrationEvent> OrganizationIntegrationEvents { get; set; }
    public DbSet<Entities.Definition.IntegrationEvents.StackIntegrationEvent> StackIntegrationEvents { get; set; }
    public DbSet<Entities.Definition.IntegrationEvents.NamespaceIntegrationEvent> NamespaceIntegrationEvents { get; set; }
    public DbSet<Entities.Definition.IntegrationEvents.ModuleIntegrationEvent> ModuleIntegrationEvents { get; set; }
    public DbSet<Entities.Definition.IntegrationDelivery> IntegrationDeliveries { get; set; }
    public DbSet<OutputSet> OutputSets { get; set; }
    public DbSet<Output> Outputs { get; set; }
    public DbSet<LiteralOutput> LiteralOutputs { get; set; }

    public DbSet<SecretOutput> SecretOutputs { get; set; }

    public DbSet<VariableSet> VariableSets { get; set; }
    public DbSet<Variable> Variables { get; set; }

    public DbSet<Runner> Runners { get; set; }
    public DbSet<RunnerConnection> RunnerConnections { get; set; }
    public DbSet<RunnerConnectionJob> RunnerConnectionJobs { get; set; }
    public DbSet<JobRunnerAssignment> JobRunnerAssignments { get; set; }
    public DbSet<RunnerStackSupply> RunnerStackSupplies { get; set; }
    public DbSet<RunnerNamespaceSupply> RunnerNamespaceSupplies { get; set; }
    public DbSet<RunnerModuleSupply> RunnerModuleSupplies { get; set; }

    public DbSet<Agent> Agents { get; set; }
    public DbSet<AgentConnection> AgentConnections { get; set; }
    public DbSet<AgentStackSupply> AgentStackSupplies { get; set; }
    public DbSet<AgentNamespaceSupply> AgentNamespaceSupplies { get; set; }
    public DbSet<AgentModuleSupply> AgentModuleSupplies { get; set; }
    public DbSet<OrganizationMission> OrganizationMissions { get; set; }
    public DbSet<StackMission> StackMissions { get; set; }
    public DbSet<NamespaceMission> NamespaceMissions { get; set; }
    public DbSet<ModuleMission> ModuleMissions { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<PreviewFeatureAcceptance> PreviewFeatureAcceptances { get; set; }
    public DbSet<UserFavorite> UserFavorites { get; set; }
    public DbSet<UserColor> UserColors { get; set; }

    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<UserGroupMember> UserGroupMembers { get; set; }
    public DbSet<ServicePrincipalGroupMember> ServicePrincipalGroupMembers { get; set; }
    public DbSet<GroupGroupMember> GroupGroupMembers { get; set; }
    public DbSet<ServicePrincipal> ServicePrincipals { get; set; }
    public DbSet<Authorization> Authorizations { get; set; }
    public DbSet<Scope> Scopes { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<NamespaceSecret> NamespaceSecrets { get; set; }
    public DbSet<ModuleSecret> ModuleSecrets { get; set; }
    public DbSet<StackSecret> StackSecrets { get; set; }

    public DbSet<Secret> Secrets { get; set; }
    public DbSet<ModulePulumiFlag> ModulePulumiFlags { get; set; }
    public DbSet<ModulePulumiArrayFlag> ModulePulumiArrayFlags { get; set; }
    public DbSet<NamespacePulumiFlag> NamespacePulumiFlags { get; set; }
    public DbSet<NamespacePulumiArrayFlag> NamespacePulumiArrayFlags { get; set; }
    public DbSet<NamespaceTerraformFlag> NamespaceTerraformFlags { get; set; }
    public DbSet<NamespaceTerraformArrayFlag> NamespaceTerraformArrayFlags { get; set; }
    public DbSet<ModuleTerraformFlag> ModuleTerraformFlags { get; set; }
    public DbSet<ModuleTerraformArrayFlag> ModuleTerraformArrayFlags { get; set; }
    public DbSet<ModuleHook> ModuleHooks { get; set; }
    public DbSet<NamespaceHook> NamespaceHooks { get; set; }

    public DbSet<ServicePrincipalSystemRoleAssignment> ServicePrincipalSystemRoleAssignments { get; set; }


    public DbSet<OrganizationRoleAssignment> OrganizationRoleAssignments { get; set; }
    public DbSet<StackRoleAssignment> StackRoleAssignments { get; set; }
    public DbSet<NamespaceRoleAssignment> NamespaceRoleAssignments { get; set; }
    public DbSet<ModuleRoleAssignment> ModuleRoleAssignments { get; set; }

    public DbSet<ServicePrincipalOrganizationRoleAssignment> ServicePrincipalOrganizationRoleAssignments { get; set; }

    public DbSet<ServicePrincipalStackRoleAssignment> ServicePrincipalStackRoleAssignments { get; set; }

    public DbSet<ServicePrincipalNamespaceRoleAssignment> ServicePrincipalNamespaceRoleAssignments { get; set; }

    public DbSet<ServicePrincipalModuleRoleAssignment> ServicePrincipalModuleRoleAssignments { get; set; }


    public DbSet<UserSystemRoleAssignment> UserSystemRoleAssignments { get; set; }

    public DbSet<UserOrganizationRoleAssignment> UserOrganizationRoleAssignments { get; set; }

    public DbSet<UserStackRoleAssignment> UserStackRoleAssignments { get; set; }

    public DbSet<UserNamespaceRoleAssignment> UserNamespaceRoleAssignments { get; set; }

    public DbSet<UserModuleRoleAssignment> UserModuleRoleAssignments { get; set; }

    public DbSet<GroupOrganizationRoleAssignment> GroupOrganizationRoleAssignments { get; set; }

    public DbSet<GroupStackRoleAssignment> GroupStackRoleAssignments { get; set; }

    public DbSet<GroupNamespaceRoleAssignment> GroupNamespaceRoleAssignments { get; set; }

    public DbSet<GroupModuleRoleAssignment> GroupModuleRoleAssignments { get; set; }

    public DbSet<RunnerRoleAssignment> RunnerRoleAssignments { get; set; }

    public DbSet<UserRunnerRoleAssignment> UserRunnerRoleAssignments { get; set; }

    public DbSet<ServicePrincipalRunnerRoleAssignment> ServicePrincipalRunnerRoleAssignments { get; set; }

    public DbSet<GroupRunnerRoleAssignment> GroupRunnerRoleAssignments { get; set; }

    public DbSet<AgentRoleAssignment> AgentRoleAssignments { get; set; }

    public DbSet<UserAgentRoleAssignment> UserAgentRoleAssignments { get; set; }

    public DbSet<ServicePrincipalAgentRoleAssignment> ServicePrincipalAgentRoleAssignments { get; set; }

    public DbSet<GroupAgentRoleAssignment> GroupAgentRoleAssignments { get; set; }

    public DbSet<StateStore> StateStores { get; set; }
    public DbSet<StateFile> StateFiles { get; set; }
    public DbSet<StateFileVersion> StateFileVersions { get; set; }

    public DbSet<StateStoreRoleAssignment> StateStoreRoleAssignments { get; set; }

    public DbSet<UserStateStoreRoleAssignment> UserStateStoreRoleAssignments { get; set; }

    public DbSet<ServicePrincipalStateStoreRoleAssignment> ServicePrincipalStateStoreRoleAssignments { get; set; }

    public DbSet<GroupStateStoreRoleAssignment> GroupStateStoreRoleAssignments { get; set; }

    // Implement the Configurations property
    protected IEnumerable<ISagaClassMap> SagaClassMaps
    {
        get
        {
            yield return new ApplyJobSagaClassMap();
            yield return new DestroyJobSagaClassMap();
            yield return new ModuleSagaClassMap();
            yield return new ModuleModifiedSagaClassMap();
        }
    }

    public Guid? GetEntityIdByQuery<T>(
        Expression<Func<T, bool>> baseFilter,
        List<(Expression<Func<T, bool>>? condition, Exception? exception)> filters,
        Expression<Func<T, Guid>> idSelector,
        Func<IQueryable<T>, IQueryable<T>>? includeNavigation = null) where T : class
    {
        var query = Set<T>().Where(baseFilter);

        foreach (var (condition, exception) in filters)
        {
            if (condition != null)
            {
                query = query.Where(condition);
                break; // Apply the first valid filter and stop processing further filters
            }

            if (exception != null) throw exception;
        }

        if (includeNavigation != null) query = includeNavigation(query);

        return query.Select(idSelector).FirstOrDefault();
    }


    public override int SaveChanges()
    {
        SetCreationTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetCreationTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetCreationTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity is ICreationTrackable);

        foreach (var entry in
                 entries)
            ((ICreationTrackable)entry.Entity).CreatedDateTime = DateTime.UtcNow; // Use UTC for consistency
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { 
        ExceptionProcessorSqlServer.UseExceptionProcessor(optionsBuilder);
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all AuditPrincipalDiscriminator enum properties to be stored as strings with max length
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entityType.GetProperties())
            if (property.ClrType == typeof(AuditPrincipalDiscriminator))
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion<string>()
                    .HasMaxLength(50);

        foreach (var configuration in SagaClassMaps)
            configuration.Configure(modelBuilder);

        modelBuilder.ApplyConfiguration(new OrganizationClassMap());
        modelBuilder.ApplyConfiguration(new OrganizationUserClassMap());
        modelBuilder.ApplyConfiguration(new StackClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceClassMap());
        modelBuilder.ApplyConfiguration(new ModuleClassMap());

        modelBuilder.ApplyConfiguration(new NamespaceInputClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceParamFromLiteralClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceParamFromDefinitionClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceParamFromSecretClassMap());

        modelBuilder.ApplyConfiguration(new NamespaceEnvVarFromLiteralClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceEnvVarFromSecretClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceEnvVarFromDefinitionClassMap());

        modelBuilder.ApplyConfiguration(new ModuleInputClassMap());
        modelBuilder.ApplyConfiguration(new ModuleInputFromNamespaceClassMap());

        modelBuilder.ApplyConfiguration(new ModuleEnvVarFromLiteralClassMap());
        modelBuilder.ApplyConfiguration(new ModuleEnvVarFromDefinitionClassMap());
        modelBuilder.ApplyConfiguration(new ModuleEnvVarFromSecretClassMap());
        modelBuilder.ApplyConfiguration(new ModuleEnvVarFromNamespaceClassMap());
        modelBuilder.ApplyConfiguration(new ModuleEnvVarFromOutputClassMap());

        modelBuilder.ApplyConfiguration(new ModuleParamFromLiteralClassMap());
        modelBuilder.ApplyConfiguration(new ModuleParamFromDefinitionClassMap());
        modelBuilder.ApplyConfiguration(new ModuleParamFromSecretClassMap());
        modelBuilder.ApplyConfiguration(new ModuleParamFromNamespaceClassMap());
        modelBuilder.ApplyConfiguration(new ModuleParamFromOutputClassMap());
        modelBuilder.ApplyConfiguration(new ModuleParamFromOutputSetClassMap());

        modelBuilder.ApplyConfiguration(new NamespaceExtraFileClassMap());
        modelBuilder.ApplyConfiguration(new ModuleExtraFileClassMap());
        modelBuilder.ApplyConfiguration(new DependsOnModuleClassMap());

        modelBuilder.ApplyConfiguration(new OutputSetClassMap());
        modelBuilder.ApplyConfiguration(new OutputClassMap());
        modelBuilder.ApplyConfiguration(new VariableSetClassMap());
        modelBuilder.ApplyConfiguration(new VariableClassMap());
        modelBuilder.ApplyConfiguration(new ModuleJobClassMap());
        modelBuilder.ApplyConfiguration(new UserFavoriteClassMap());
        modelBuilder.ApplyConfiguration(new UserColorClassMap());
        modelBuilder.ApplyConfiguration(new ModuleJobApprovalClassMap());
        modelBuilder.ApplyConfiguration(new ModuleJobMissionClassMap());
        modelBuilder.ApplyConfiguration(new ModuleJobMissionRunClassMap());
        modelBuilder.ApplyConfiguration(new ModuleJobMissionRunMilestoneClassMap());
        modelBuilder.ApplyConfiguration(new IntegrationClassMap());
        modelBuilder.ApplyConfiguration(new IntegrationStackSupplyClassMap());
        modelBuilder.ApplyConfiguration(new IntegrationNamespaceSupplyClassMap());
        modelBuilder.ApplyConfiguration(new IntegrationModuleSupplyClassMap());
        modelBuilder.ApplyConfiguration(new ClassMaps.RoleAssignments.Org.IntegrationRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ClassMaps.RoleAssignments.Org.UserIntegrationRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ClassMaps.RoleAssignments.Org.ServicePrincipalIntegrationRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ClassMaps.RoleAssignments.Org.GroupIntegrationRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new OrganizationIntegrationEventClassMap());
        modelBuilder.ApplyConfiguration(new StackIntegrationEventClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceIntegrationEventClassMap());
        modelBuilder.ApplyConfiguration(new ModuleIntegrationEventClassMap());
        modelBuilder.ApplyConfiguration(new IntegrationDeliveryClassMap());
        modelBuilder.ApplyConfiguration(new RunnerClassMap());
        modelBuilder.ApplyConfiguration(new RunnerConnectionClassMap());
        modelBuilder.ApplyConfiguration(new RunnerConnectionJobClassMap());
        modelBuilder.ApplyConfiguration(new JobRunnerAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new SourceRefresherPreselectionClassMap());
        modelBuilder.ApplyConfiguration(new RunnerStackSupplyClassMap());
        modelBuilder.ApplyConfiguration(new RunnerNamespaceSupplyClassMap());
        modelBuilder.ApplyConfiguration(new RunnerModuleSupplyClassMap());
        modelBuilder.ApplyConfiguration(new AgentClassMap());
        modelBuilder.ApplyConfiguration(new AgentConnectionClassMap());
        modelBuilder.ApplyConfiguration(new AgentStackSupplyClassMap());
        modelBuilder.ApplyConfiguration(new AgentNamespaceSupplyClassMap());
        modelBuilder.ApplyConfiguration(new AgentModuleSupplyClassMap());
        modelBuilder.ApplyConfiguration(new OrganizationMissionClassMap());
        modelBuilder.ApplyConfiguration(new StackMissionClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceMissionClassMap());
        modelBuilder.ApplyConfiguration(new ModuleMissionClassMap());
        modelBuilder.ApplyConfiguration(new ServicePrincipalClassMap());
        modelBuilder.ApplyConfiguration(new GroupClassMap());
        modelBuilder.ApplyConfiguration(new GroupMemberClassMap());
        modelBuilder.ApplyConfiguration(new UserGroupMemberClassMap());
        modelBuilder.ApplyConfiguration(new ServicePrincipalGroupMemberClassMap());
        modelBuilder.ApplyConfiguration(new GroupGroupMemberClassMap());
        modelBuilder.ApplyConfiguration(new RecursiveGroupMemberClassMap());
        modelBuilder.ApplyConfiguration(new TokenClassMap());
        modelBuilder.ApplyConfiguration(new AuthorizationClassMap());

        modelBuilder.ApplyConfiguration(new SecretClassMap());

        modelBuilder.ApplyConfiguration(new StackSecretClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceSecretClassMap());
        modelBuilder.ApplyConfiguration(new ModuleSecretClassMap());
        modelBuilder.ApplyConfiguration(new SecretOutputClassMap());

        modelBuilder.ApplyConfiguration(new ModulePulumiFlagClassMap());
        modelBuilder.ApplyConfiguration(new ModulePulumiArrayFlagClassMap());
        modelBuilder.ApplyConfiguration(new NamespacePulumiFlagClassMap());
        modelBuilder.ApplyConfiguration(new NamespacePulumiArrayFlagClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceTerraformFlagClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceTerraformArrayFlagClassMap());
        modelBuilder.ApplyConfiguration(new ModuleTerraformFlagClassMap());
        modelBuilder.ApplyConfiguration(new ModuleTerraformArrayFlagClassMap());
        modelBuilder.ApplyConfiguration(new ModuleHookClassMap());
        modelBuilder.ApplyConfiguration(new NamespaceHookClassMap());

        // Organization Role Assignment ClassMaps
        modelBuilder.ApplyConfiguration(new OrganizationRoleAssignmentClassMap()); // Base TPH configuration
        modelBuilder.ApplyConfiguration(new StackRoleAssignmentClassMap()); // Base TPH configuration
        modelBuilder.ApplyConfiguration(new NamespaceRoleAssignmentClassMap()); // Base TPH configuration
        modelBuilder.ApplyConfiguration(new ModuleRoleAssignmentClassMap()); // Base TPH configuration
        modelBuilder.ApplyConfiguration(new RunnerRoleAssignmentClassMap()); // Base TPH configuration
        modelBuilder.ApplyConfiguration(new AgentRoleAssignmentClassMap()); // Base TPH configuration
        modelBuilder.ApplyConfiguration(new StateStoreRoleAssignmentClassMap()); // Base TPH configuration

        modelBuilder.ApplyConfiguration(new UserOrganizationRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new UserStackRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new UserNamespaceRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new UserModuleRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ServicePrincipalOrganizationRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ServicePrincipalStackRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ServicePrincipalNamespaceRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ServicePrincipalModuleRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new GroupOrganizationRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new GroupStackRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new GroupNamespaceRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new GroupModuleRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new UserRunnerRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ServicePrincipalRunnerRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new GroupRunnerRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new UserAgentRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ServicePrincipalAgentRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new GroupAgentRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new UserStateStoreRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ServicePrincipalStateStoreRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new GroupStateStoreRoleAssignmentClassMap());

        // StateStore and StateFile
        modelBuilder.ApplyConfiguration(new StateStoreClassMap());
        modelBuilder.ApplyConfiguration(new StateFileClassMap());
        modelBuilder.ApplyConfiguration(new StateFileVersionClassMap());

        // System Role Assignment ClassMaps
        modelBuilder.ApplyConfiguration(new UserSystemRoleAssignmentClassMap());
        modelBuilder.ApplyConfiguration(new ServicePrincipalSystemRoleAssignmentClassMap());

        modelBuilder.Entity<ServicePrincipal>(b => { b.ToTable("ServicePrincipals"); });
        modelBuilder.Entity<Authorization>(b => { b.ToTable("Authorizations"); });
        modelBuilder.Entity<Scope>(b => { b.ToTable("Scopes"); });
        modelBuilder.Entity<Token>(b => { b.ToTable("Tokens"); });
        modelBuilder.Entity<Definition_User>(b => { b.ToTable("Users"); });
        modelBuilder.Entity<IdentityUserClaim<Guid>>(b => { b.ToTable("UserClaims"); });
        modelBuilder.Entity<IdentityUserLogin<Guid>>(b => { b.ToTable("UserLogins"); });
        modelBuilder.Entity<IdentityUserToken<Guid>>(b => { b.ToTable("UserTokens"); });
        modelBuilder.Entity<IdentityRole<Guid>>(b => { b.ToTable("Roles"); });
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(b => { b.ToTable("RoleClaims"); });
        modelBuilder.Entity<IdentityUserRole<Guid>>(b => { b.ToTable("UserRoles"); });


        // Indexes for ICreationTrackable entities on CreatedDateTime
        modelBuilder.Entity<Stack>()
            .HasIndex(e => e.CreatedDateTime)
            .HasDatabaseName("IX_Stack_CreatedDateTime");

        modelBuilder.Entity<Namespace>()
            .HasIndex(e => e.CreatedDateTime)
            .HasDatabaseName("IX_Namespace_CreatedDateTime");

        modelBuilder.Entity<Module>()
            .HasIndex(e => e.CreatedDateTime)
            .HasDatabaseName("IX_Module_CreatedDateTime");

        modelBuilder.Entity<Runner>()
            .HasIndex(e => e.CreatedDateTime)
            .HasDatabaseName("IX_Runner_CreatedDateTime");

        modelBuilder.Entity<Definition_User>()
            .HasIndex(e => e.CreatedDateTime)
            .HasDatabaseName("IX_User_CreatedDateTime");

        modelBuilder.Entity<PreviewFeatureAcceptance>()
            .HasIndex(e => new { e.OrganizationId, e.PreviewFeature })
            .IsUnique()
            .HasDatabaseName("IX_PreviewFeatureAcceptance_OrgId_Feature");

        modelBuilder.Entity<PreviewFeatureAcceptance>()
            .Property(e => e.PreviewFeature)
            .HasConversion<string>();

        // Configure dependency graph views
        modelBuilder.Entity<Dependency>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_Dependencies");

                // Configure enum conversions
                entity.Property(e => e.DefinedLatestActualState)
                    .HasConversion<string>();
                entity.Property(e => e.ReferencedLatestActualState)
                    .HasConversion<string>();


                entity.Property(e => e.DefinedDesiredState)
                    .HasConversion<string>();
                entity.Property(e => e.ReferencedDesiredState)
                    .HasConversion<string>();

                entity.Property(e => e.DefinedQueuedDesiredState)
                    .HasConversion<string>();
                entity.Property(e => e.ReferencedQueuedDesiredState)
                    .HasConversion<string>();

                entity.Property(e => e.DefinedRunningDesiredState)
                    .HasConversion<string>();
                entity.Property(e => e.ReferencedRunningDesiredState)
                    .HasConversion<string>();

                entity.Property(e => e.ReferencedIsRunning).IsRequired(false);
                entity.Property(e => e.ReferencedIsQueued).IsRequired(false);

                // Configure navigation properties
                entity.HasOne(e => e.DefinedModule)
                    .WithMany()
                    .HasForeignKey(e => new { e.DefinedModuleId, e.DefinedOrganizationId })
                    .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.ReferencedModule)
                    .WithMany()
                    .HasForeignKey(e => new { e.ReferencedModuleId, e.ReferencedOrganizationId })
                    .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.DefinedNamespace)
                    .WithMany()
                    .HasForeignKey(e => new { e.DefinedNamespaceId, e.DefinedOrganizationId })
                    .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.ReferencedNamespace)
                    .WithMany()
                    .HasForeignKey(e => new { e.ReferencedNamespaceId, e.ReferencedOrganizationId })
                    .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.DefinedStack)
                    .WithMany()
                    .HasForeignKey(e => new { e.DefinedStackId, e.DefinedOrganizationId })
                    .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.ReferencedStack)
                    .WithMany()
                    .HasForeignKey(e => new { e.ReferencedStackId, e.ReferencedOrganizationId })
                    .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
                    .OnDelete(DeleteBehavior.NoAction);
            }
        );

        // Configure recursive dependency graph view
        modelBuilder.Entity<RecursiveApplyDependency>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_RecursiveApplyDependencies");

            // Configure enum conversions
            entity.Property(e => e.DefinedLatestActualState)
                .HasConversion<string>();
            entity.Property(e => e.ReferencedLatestActualState)
                .HasConversion<string>();
            entity.Property(e => e.RootLatestActualState)
                .HasConversion<string>();
            entity.Property(e => e.DefinedDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.ReferencedDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.RootDesiredState)
                .HasConversion<string>();

            entity.Property(e => e.DefinedQueuedDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.ReferencedQueuedDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.RootQueuedDesiredState)
                .HasConversion<string>();

            entity.Property(e => e.DefinedRunningDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.ReferencedRunningDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.RootRunningDesiredState)
                .HasConversion<string>();

            entity.Property(e => e.ReferencedIsRunning).IsRequired(false);
            entity.Property(e => e.ReferencedIsQueued).IsRequired(false);

            // Configure navigation properties
            entity.HasOne(e => e.DefinedModule)
                .WithMany()
                .HasForeignKey(e => new { e.DefinedModuleId, e.DefinedOrganizationId })
                .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ReferencedModule)
                .WithMany()
                .HasForeignKey(e => new { e.ReferencedModuleId, e.ReferencedOrganizationId })
                .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.DefinedNamespace)
                .WithMany()
                .HasForeignKey(e => new { e.DefinedNamespaceId, e.DefinedOrganizationId })
                .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ReferencedNamespace)
                .WithMany()
                .HasForeignKey(e => new { e.ReferencedNamespaceId, e.ReferencedOrganizationId })
                .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.DefinedStack)
                .WithMany()
                .HasForeignKey(e => new { e.DefinedStackId, e.DefinedOrganizationId })
                .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ReferencedStack)
                .WithMany()
                .HasForeignKey(e => new { e.ReferencedStackId, e.ReferencedOrganizationId })
                .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RecursiveDestroyDependency>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_RecursiveDestroyDependencies");

            // Configure enum conversions
            entity.Property(e => e.DefinedLatestActualState)
                .HasConversion<string>();
            entity.Property(e => e.ReferencedLatestActualState)
                .HasConversion<string>();
            entity.Property(e => e.RootLatestActualState)
                .HasConversion<string>();

            entity.Property(e => e.DefinedDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.ReferencedDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.RootDesiredState)
                .HasConversion<string>();

            entity.Property(e => e.DefinedQueuedDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.ReferencedQueuedDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.RootQueuedDesiredState)
                .HasConversion<string>();

            entity.Property(e => e.DefinedRunningDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.ReferencedRunningDesiredState)
                .HasConversion<string>();
            entity.Property(e => e.RootRunningDesiredState)
                .HasConversion<string>();

            entity.Property(e => e.ReferencedIsRunning).IsRequired(false);
            entity.Property(e => e.ReferencedIsQueued).IsRequired(false);

            // Configure navigation properties with composite keys
            entity.HasOne(e => e.DefinedModule)
                .WithMany()
                .HasForeignKey(e => new { e.DefinedModuleId, e.DefinedOrganizationId })
                .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ReferencedModule)
                .WithMany()
                .HasForeignKey(e => new { e.ReferencedModuleId, e.ReferencedOrganizationId })
                .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.DefinedNamespace)
                .WithMany()
                .HasForeignKey(e => new { e.DefinedNamespaceId, e.DefinedOrganizationId })
                .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ReferencedNamespace)
                .WithMany()
                .HasForeignKey(e => new { e.ReferencedNamespaceId, e.ReferencedOrganizationId })
                .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.DefinedStack)
                .WithMany()
                .HasForeignKey(e => new { e.DefinedStackId, e.DefinedOrganizationId })
                .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ReferencedStack)
                .WithMany()
                .HasForeignKey(e => new { e.ReferencedStackId, e.ReferencedOrganizationId })
                .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}