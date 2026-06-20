// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapCd.Contracts;
using Microsoft.Extensions.Caching.Memory;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Host.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.ViewManagement;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.StateMachine;
using Testcontainers.MsSql;
using Group = SnapCd.Server.Core.Entities.Definition.Group;
using Module = SnapCd.Server.Core.Entities.Definition.Module;
using ModuleJob = SnapCd.Server.Core.Entities.Definition.ModuleJob;
using ModuleParamFromOutput = SnapCd.Server.Core.Entities.Definition.ModuleParamFromOutput;
using ModuleParamFromOutputSet = SnapCd.Server.Core.Entities.Definition.ModuleParamFromOutputSet;
using ModuleParamFromSecret = SnapCd.Server.Core.Entities.Definition.ModuleParamFromSecret;
using Namespace = SnapCd.Server.Core.Entities.Definition.Namespace;
using NamespaceParamFromSecret = SnapCd.Server.Core.Entities.Definition.NamespaceParamFromSecret;
using Organization = SnapCd.Server.Core.Entities.Definition.Organization;
using Output = SnapCd.Server.Core.Entities.Definition.Output;
using OutputSet = SnapCd.Server.Core.Entities.Definition.OutputSet;
using Runner = SnapCd.Server.Core.Entities.Definition.Runner;
using Stack = SnapCd.Server.Core.Entities.Definition.Stack;
using Variable = SnapCd.Server.Core.Entities.Definition.Variable;
using VariableSet = SnapCd.Server.Core.Entities.Definition.VariableSet;

namespace SnapCd.Server.Core.Tests.Infrastructure;

[CollectionDefinition("NewRoleBasedSharedFixture")]
public class NewRoleBasedSharedCollection : ICollectionFixture<Fixture>
{
}

/// <summary>
/// Groups all principal types for a specific role assignment.
/// </summary>
public class RolePrincipals
{
    public SnapCd.Server.Core.Entities.Definition.User DirectUser { get; set; } = null!;
    public SnapCd.Server.Core.Entities.Definition.ServicePrincipal DirectServicePrincipal { get; set; } = null!;
    public SnapCd.Server.Core.Entities.Definition.User GroupUser { get; set; } = null!;
    public SnapCd.Server.Core.Entities.Definition.ServicePrincipal GroupServicePrincipal { get; set; } = null!;
    public SnapCd.Server.Core.Entities.Definition.User NestedGroupUser { get; set; } = null!;
    public SnapCd.Server.Core.Entities.Definition.ServicePrincipal NestedGroupServicePrincipal { get; set; } = null!;
    public Group Group { get; set; } = null!;
}

/// <summary>
/// Contains dedicated entities for a single test class's Update/Delete tests.
/// These entities are NOT shared and will be modified/deleted during testing.
/// </summary>
public class ModuleUpdateDeleteEntities
{
    public Module? UpdateCanModule { get; set; }
    public Module? UpdateCannotModule { get; set; }
    public Module? DeleteCanModule { get; set; }
    public Module? DeleteCannotModule { get; set; }
}

/// <summary>
/// New role-based test fixture using binary tree hierarchy (0/1 path notation).
/// Organized by role type rather than entity type for better test coverage.
/// This is a copy of RoleBasedSharedFixture for iterative improvements.
/// </summary>
public class Fixture : IAsyncLifetime
{
    private IContainer? _databaseContainer;
    private ServiceProvider? _serviceProvider;
    private string? _connectionString;

    public string ConnectionString => _connectionString ?? throw new InvalidOperationException("Fixture not initialized");

    // Binary tree hierarchy - entities keyed by path (e.g., "0", "00", "000", "0000")
    public Dictionary<string, Organization> Organizations { get; } = new();
    public Dictionary<string, Stack> Stacks { get; } = new();
    public Dictionary<string, Namespace> Namespaces { get; } = new();
    public Dictionary<string, Module> Modules { get; } = new();
    public Dictionary<string, ModuleInput> ModuleInputs { get; } = new();
    public Dictionary<string, NamespaceInput> NamespaceInputs { get; } = new();
    public Dictionary<string, OutputSet> OutputSets { get; } = new();
    public Dictionary<string, Output> Outputs { get; } = new();
    public Dictionary<string, VariableSet> VariableSets { get; } = new();
    public Dictionary<string, Variable> Inputs { get; } = new();
    public Dictionary<string, Runner> Runners { get; } = new();
    public Dictionary<string, StackSecret> StackSecrets { get; } = new();
    public Dictionary<string, NamespaceSecret> NamespaceSecrets { get; } = new();
    public Dictionary<string, ModuleSecret> ModuleSecrets { get; } = new();
    public Dictionary<string, ModuleParamFromSecret> ModuleInputFromSecrets { get; } = new();
    public Dictionary<string, NamespaceParamFromSecret> NamespaceInputFromSecrets { get; } = new();
    public Dictionary<string, ModuleJob> ModuleJobs { get; } = new();
    // Tier A (Smoke) — dedicated per-test-class Update/Delete entities. Keyed by
    // "{TestClassName}_UpdateCan" or "{TestClassName}_DeleteCan". Each entity is mutated/deleted
    // by exactly one test, so they're cheap to allocate one-per-test-class up front.
    public Dictionary<string, Stack> SmokeStacks { get; } = new();
    public Dictionary<string, Namespace> SmokeNamespaces { get; } = new();
    public Dictionary<string, Module> SmokeModules { get; } = new();
    public Dictionary<string, ModuleHook> SmokeModuleHooks { get; } = new();
    public Dictionary<string, AgentModuleSupply> SmokeAgentSupplies { get; } = new();
    public Dictionary<string, RunnerModuleSupply> SmokeRunnerSupplies { get; } = new();

    public Dictionary<string, Agent> Agents { get; } = new();
    public Dictionary<string, AgentModuleSupply> AgentModuleSupplies { get; } = new();
    public Dictionary<string, RunnerModuleSupply> RunnerModuleSupplies { get; } = new();
    public Dictionary<string, SnapCd.Server.Core.Entities.Definition.Missions.OrganizationMission> OrganizationMissions { get; } = new();
    public Dictionary<string, SnapCd.Server.Core.Entities.Definition.Missions.StackMission> StackMissions { get; } = new();
    public Dictionary<string, SnapCd.Server.Core.Entities.Definition.Missions.NamespaceMission> NamespaceMissions { get; } = new();
    public Dictionary<string, SnapCd.Server.Core.Entities.Definition.Missions.ModuleMission> ModuleMissions { get; } = new();
    public Dictionary<string, UserOrganizationRoleAssignment> UserOrganizationRoleAssignments { get; } = new();
    public Dictionary<string, UserStackRoleAssignment> UserStackRoleAssignments { get; } = new();
    public Dictionary<string, UserNamespaceRoleAssignment> UserNamespaceRoleAssignments { get; } = new();
    public Dictionary<string, UserModuleRoleAssignment> UserModuleRoleAssignments { get; } = new();

    // Role-based principal lookups - [org-path][role] => principals
    public Dictionary<string, Dictionary<OrganizationRole, RolePrincipals>> OrganizationPrincipals { get; } = new();
    public Dictionary<string, Dictionary<RunnerRole, RolePrincipals>> RunnerPrincipals { get; } = new();

    // Tier B (RoleResolution) — direct-User principals seeded per scope-role per scope row.
    // Only "Reader" of each scope-role is seeded; Tier B asserts visibility, which the minimal
    // sufficient role grants. Keyed by Tier B test naming convention:
    //   "Stack00.Reader" / "Stack01.Reader" / "Namespace000.Reader" / "Module0000.Reader" / etc.
    public Dictionary<string, SnapCd.Server.Core.Entities.Definition.User> ScopeReaderUsers { get; } = new();

    // Test-specific entities for Update/Delete operations (not shared, will be modified/deleted)
    public Dictionary<string, Stack> StackAdditionalTestEntities { get; } = new();
    public Dictionary<string, Namespace> NamespaceAdditionalTestEntities { get; } = new();
    public Dictionary<string, Runner> RunnerAdditionalTestEntities { get; } = new();
    public Dictionary<string, Module> ModuleAdditionalTestEntities { get; } = new();
    public Dictionary<string, ModuleInput> ModuleInputAdditionalTestEntities { get; } = new();
    public Dictionary<string, NamespaceInput> NamespaceInputAdditionalTestEntities { get; } = new();
    public Dictionary<string, Output> OutputAdditionalTestEntities { get; } = new();
    public Dictionary<string, OutputSet> OutputSetAdditionalTestEntities { get; } = new();
    public Dictionary<string, StackSecret> StackSecretAdditionalTestEntities { get; } = new();
    public Dictionary<string, NamespaceSecret> NamespaceSecretAdditionalTestEntities { get; } = new();
    public Dictionary<string, ModuleSecret> ModuleSecretAdditionalTestEntities { get; } = new();
    public Dictionary<string, Variable> InputAdditionalTestEntities { get; } = new();
    public Dictionary<string, VariableSet> VariableSetAdditionalTestEntities { get; } = new();
    public Dictionary<string, UserOrganizationRoleAssignment> UserOrganizationRoleAssignmentAdditionalTestEntities { get; } = new();
    public Dictionary<string, UserStackRoleAssignment> UserStackRoleAssignmentAdditionalTestEntities { get; } = new();
    public Dictionary<string, UserNamespaceRoleAssignment> UserNamespaceRoleAssignmentAdditionalTestEntities { get; } = new();
    public Dictionary<string, UserModuleRoleAssignment> UserModuleRoleAssignmentAdditionalTestEntities { get; } = new();

    // Orphaned job cleanup test data
    public Dictionary<string, ModuleJob> OrphanedJobTestData { get; } = new();
    public Dictionary<string, ApplyJobSaga> OrphanedJobTestSagas { get; } = new();

    // User with no permissions (for negative testing)
    public SnapCd.Server.Core.Entities.Definition.User NoPermissionUser { get; private set; } = null!;
    public SnapCd.Server.Core.Entities.Definition.ServicePrincipal NoPermissionServicePrincipal { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Start SQL Server container
        _databaseContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("TestPass123!")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(1433))
            .Build();

        await _databaseContainer.StartAsync();
        _connectionString = ((MsSqlContainer)_databaseContainer).GetConnectionString();

        // Configure services
        var services = new ServiceCollection();
        // Migrations target SelfHostedSnapCdDbContext (subclass of SnapCdDbContext) and live in
        // SnapCd.Server.Host. Register the subclass so MigrateAsync() finds and applies them, then
        // expose SnapCdDbContext as the same scoped instance so existing test code can resolve it.
        services.AddDbContext<SelfHostedSnapCdDbContext>(options =>
        {
            options.UseSqlServer(_connectionString, sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly("SnapCd.Server.Host");
            });
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<SelfHostedSnapCdDbContext>());

        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Register ViewManager for test fixture
        services.AddScoped<IViewManager, ViewManager>();

        _serviceProvider = services.BuildServiceProvider();

        // Run migrations against the SelfHosted subclass — that's the DbContext type the
        // migrations are tied to (see *.Designer.cs files in SnapCd.Server.Host/Database/Migrations).
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SelfHostedSnapCdDbContext>();
        await dbContext.Database.MigrateAsync();

        // Apply database views after migrations
        var viewManager = scope.ServiceProvider.GetRequiredService<IViewManager>();
        await viewManager.ApplyViewsAsync();

        // Seed all test data
        await SeedTestData(dbContext);
    }

    private async Task SeedTestData(SnapCdDbContext dbContext)
    {
        // Create organizations using binary tree paths
        Organizations["0"] = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Org0",
        };
        Organizations["1"] = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Org1",
        };

        dbContext.Organizations.AddRange(Organizations.Values);

        // Create ServicePrincipals for Runners
        var runner0ServicePrincipal = CreateServicePrincipal("runner0-sp", Organizations["0"].Id);
        var runner1ServicePrincipal = CreateServicePrincipal("runner1-sp", Organizations["1"].Id);
        dbContext.ServicePrincipals.Add(runner0ServicePrincipal);
        dbContext.ServicePrincipals.Add(runner1ServicePrincipal);

        // Create Runners for each organization
        Runners["0"] = CreateRunner("Runner0", Organizations["0"].Id, runner0ServicePrincipal.Id);
        Runners["1"] = CreateRunner("Runner1", Organizations["1"].Id, runner1ServicePrincipal.Id);
        dbContext.Runners.AddRange(Runners.Values);

        // Create binary tree hierarchy for Org "0" (primary)
        Stacks["00"] = CreateStack("Stack00", Organizations["0"].Id);
        Stacks["01"] = CreateStack("Stack01", Organizations["0"].Id);
        dbContext.Stacks.AddRange(Stacks["00"], Stacks["01"]);

        Namespaces["000"] = CreateNamespace("Namespace000", Stacks["00"].Id, Organizations["0"].Id);
        Namespaces["001"] = CreateNamespace("Namespace001", Stacks["00"].Id, Organizations["0"].Id);
        Namespaces["010"] = CreateNamespace("Namespace010", Stacks["01"].Id, Organizations["0"].Id);
        Namespaces["011"] = CreateNamespace("Namespace011", Stacks["01"].Id, Organizations["0"].Id);
        dbContext.Namespaces.AddRange(Namespaces["000"], Namespaces["001"], Namespaces["010"], Namespaces["011"]);

        // Create modules in Org "0" tree (8 total - 2 per namespace)
        Modules["0000"] = CreateModule("Module0000", Namespaces["000"].Id, Runners["0"].Id, Organizations["0"].Id);
        Modules["0001"] = CreateModule("Module0001", Namespaces["000"].Id, Runners["0"].Id, Organizations["0"].Id);
        Modules["0010"] = CreateModule("Module0010", Namespaces["001"].Id, Runners["0"].Id, Organizations["0"].Id);
        Modules["0011"] = CreateModule("Module0011", Namespaces["001"].Id, Runners["0"].Id, Organizations["0"].Id);
        Modules["0100"] = CreateModule("Module0100", Namespaces["010"].Id, Runners["0"].Id, Organizations["0"].Id);
        Modules["0101"] = CreateModule("Module0101", Namespaces["010"].Id, Runners["0"].Id, Organizations["0"].Id);
        Modules["0110"] = CreateModule("Module0110", Namespaces["011"].Id, Runners["0"].Id, Organizations["0"].Id);
        Modules["0111"] = CreateModule("Module0111", Namespaces["011"].Id, Runners["0"].Id, Organizations["0"].Id);

        // Create minimal hierarchy for Org "1" (for cross-org isolation testing)
        Stacks["10"] = CreateStack("Stack10", Organizations["1"].Id);
        dbContext.Stacks.Add(Stacks["10"]);

        Namespaces["100"] = CreateNamespace("Namespace100", Stacks["10"].Id, Organizations["1"].Id);
        dbContext.Namespaces.Add(Namespaces["100"]);

        Modules["1000"] = CreateModule("Module1000", Namespaces["100"].Id, Runners["1"].Id, Organizations["1"].Id);

        dbContext.Modules.AddRange(Modules.Values);

        // Create ModuleInputs for testing
        ModuleInputs["00000"] = CreateModuleInput("ModuleInput00000", Modules["0000"].Id, Organizations["0"].Id);
        ModuleInputs["00001"] = CreateModuleInput("ModuleInput00001", Modules["0000"].Id, Organizations["0"].Id);
        ModuleInputs["10000"] = CreateModuleInput("ModuleInput10000", Modules["1000"].Id, Organizations["1"].Id);
        dbContext.ModuleInputs.AddRange(ModuleInputs.Values);

        // Create NamespaceInputs for testing
        NamespaceInputs["00000"] = CreateNamespaceInput("NamespaceInput00000", Namespaces["000"].Id, Organizations["0"].Id);
        NamespaceInputs["00001"] = CreateNamespaceInput("NamespaceInput00001", Namespaces["000"].Id, Organizations["0"].Id);
        NamespaceInputs["10000"] = CreateNamespaceInput("NamespaceInput10000", Namespaces["100"].Id, Organizations["1"].Id);
        dbContext.NamespaceInputs.AddRange(NamespaceInputs.Values);

        // Create OutputSets and Outputs for testing
        OutputSets["00000"] = CreateOutputSet("OutputSet00000", Modules["0000"].Id, Organizations["0"].Id);
        OutputSets["00001"] = CreateOutputSet("OutputSet00001", Modules["0000"].Id, Organizations["0"].Id);
        OutputSets["10000"] = CreateOutputSet("OutputSet10000", Modules["1000"].Id, Organizations["1"].Id);
        dbContext.OutputSets.AddRange(OutputSets.Values);

        Outputs["00000"] = CreateOutput("Output00000", OutputSets["00000"].Id, Organizations["0"].Id);
        Outputs["00001"] = CreateOutput("Output00001", OutputSets["00001"].Id, Organizations["0"].Id);
        Outputs["10000"] = CreateOutput("Output10000", OutputSets["10000"].Id, Organizations["1"].Id);
        dbContext.Outputs.AddRange(Outputs.Values);

        // Create VariableSets and Inputs for testing
        VariableSets["00000"] = CreateVariableSet("VariableSet00000", Modules["0000"].Id, Organizations["0"].Id);
        VariableSets["00001"] = CreateVariableSet("VariableSet00001", Modules["0000"].Id, Organizations["0"].Id);
        VariableSets["10000"] = CreateVariableSet("VariableSet10000", Modules["1000"].Id, Organizations["1"].Id);
        dbContext.VariableSets.AddRange(VariableSets.Values);

        Inputs["00000"] = CreateInput("Input00000", VariableSets["00000"].Id, Organizations["0"].Id);
        Inputs["00001"] = CreateInput("Input00001", VariableSets["00001"].Id, Organizations["0"].Id);
        Inputs["10000"] = CreateInput("Input10000", VariableSets["10000"].Id, Organizations["1"].Id);
        dbContext.Variables.AddRange(Inputs.Values);

        // Create StackSecrets for testing
        StackSecrets["000"] = CreateStackSecret("StackSecret000", Stacks["00"].Id, Organizations["0"].Id);
        StackSecrets["001"] = CreateStackSecret("StackSecret001", Stacks["00"].Id, Organizations["0"].Id);
        StackSecrets["100"] = CreateStackSecret("StackSecret100", Stacks["10"].Id, Organizations["1"].Id);
        dbContext.StackSecrets.AddRange(StackSecrets.Values);

        // Create NamespaceSecrets for testing
        NamespaceSecrets["000"] = CreateNamespaceSecret("NamespaceSecret000", Namespaces["000"].Id, Organizations["0"].Id);
        NamespaceSecrets["001"] = CreateNamespaceSecret("NamespaceSecret001", Namespaces["000"].Id, Organizations["0"].Id);
        NamespaceSecrets["100"] = CreateNamespaceSecret("NamespaceSecret100", Namespaces["100"].Id, Organizations["1"].Id);
        dbContext.NamespaceSecrets.AddRange(NamespaceSecrets.Values);

        // Create ModuleSecrets for testing
        ModuleSecrets["0000"] = CreateModuleSecret("ModuleSecret0000", Modules["0000"].Id, Organizations["0"].Id);
        ModuleSecrets["0001"] = CreateModuleSecret("ModuleSecret0001", Modules["0000"].Id, Organizations["0"].Id);
        ModuleSecrets["1000"] = CreateModuleSecret("ModuleSecret1000", Modules["1000"].Id, Organizations["1"].Id);
        dbContext.ModuleSecrets.AddRange(ModuleSecrets.Values);

        // Create ModuleParamFromSecret for testing (RunnerRunner has read access)
        ModuleInputFromSecrets["00000"] = CreateModuleInputFromSecret("ModuleInputFromSecret00000", Modules["0000"].Id, ModuleSecrets["0000"].Id, Organizations["0"].Id);
        ModuleInputFromSecrets["00001"] = CreateModuleInputFromSecret("ModuleInputFromSecret00001", Modules["0000"].Id, ModuleSecrets["0001"].Id, Organizations["0"].Id);
        ModuleInputFromSecrets["10000"] = CreateModuleInputFromSecret("ModuleInputFromSecret10000", Modules["1000"].Id, ModuleSecrets["1000"].Id, Organizations["1"].Id);
        dbContext.ModuleInputs.AddRange(ModuleInputFromSecrets.Values);

        // Create NamespaceParamFromSecret for testing (RunnerRunner has read access per user request)
        NamespaceInputFromSecrets["00000"] = CreateNamespaceInputFromSecret("NamespaceInputFromSecret00000", Namespaces["000"].Id, NamespaceSecrets["000"].Id, Organizations["0"].Id);
        NamespaceInputFromSecrets["00001"] = CreateNamespaceInputFromSecret("NamespaceInputFromSecret00001", Namespaces["000"].Id, NamespaceSecrets["001"].Id, Organizations["0"].Id);
        NamespaceInputFromSecrets["10000"] = CreateNamespaceInputFromSecret("NamespaceInputFromSecret10000", Namespaces["100"].Id, NamespaceSecrets["100"].Id, Organizations["1"].Id);
        dbContext.NamespaceInputs.AddRange(NamespaceInputFromSecrets.Values);

        // Create ModuleJobs for testing (RunnerRunner CanPostLogs permission)
        ModuleJobs["00000"] = CreateModuleJob(Modules["0000"].Id, Organizations["0"].Id);
        ModuleJobs["00001"] = CreateModuleJob(Modules["0000"].Id, Organizations["0"].Id);
        ModuleJobs["10000"] = CreateModuleJob(Modules["1000"].Id, Organizations["1"].Id);
        dbContext.ModuleJobs.AddRange(ModuleJobs.Values);

        // Create three additional modules for Output "ReferencedByModule" testing
        // Module "0002" - will have OutputSet referenced via ModuleParamFromOutputSet (all outputs readable)
        Modules["0002"] = CreateModule("Module0002", Namespaces["000"].Id, Runners["0"].Id, Organizations["0"].Id);
        OutputSets["00002"] = CreateOutputSet("OutputSet00002", Modules["0002"].Id, Organizations["0"].Id);
        Outputs["00002"] = CreateOutput("Output00002", OutputSets["00002"].Id, Organizations["0"].Id);
        Outputs["00003"] = CreateOutput("Output00003", OutputSets["00002"].Id, Organizations["0"].Id);
        dbContext.Modules.Add(Modules["0002"]);
        dbContext.OutputSets.Add(OutputSets["00002"]);
        dbContext.Outputs.AddRange(Outputs["00002"], Outputs["00003"]);

        // Module "0003" - will have one Output referenced via ModuleParamFromOutput, one unreferenced
        Modules["0003"] = CreateModule("Module0003", Namespaces["000"].Id, Runners["0"].Id, Organizations["0"].Id);
        OutputSets["00003"] = CreateOutputSet("OutputSet00003", Modules["0003"].Id, Organizations["0"].Id);
        Outputs["00004"] = CreateOutput("Output00004", OutputSets["00003"].Id, Organizations["0"].Id); // Will be referenced
        Outputs["00005"] = CreateOutput("Output00005", OutputSets["00003"].Id, Organizations["0"].Id); // Not referenced
        dbContext.Modules.Add(Modules["0003"]);
        dbContext.OutputSets.Add(OutputSets["00003"]);
        dbContext.Outputs.AddRange(Outputs["00004"], Outputs["00005"]);

        // Module "0004" - will have no references (not readable by Runner Runner)
        Modules["0004"] = CreateModule("Module0004", Namespaces["000"].Id, Runners["0"].Id, Organizations["0"].Id);
        OutputSets["00004"] = CreateOutputSet("OutputSet00004", Modules["0004"].Id, Organizations["0"].Id);
        Outputs["00006"] = CreateOutput("Output00006", OutputSets["00004"].Id, Organizations["0"].Id);
        dbContext.Modules.Add(Modules["0004"]);
        dbContext.OutputSets.Add(OutputSets["00004"]);
        dbContext.Outputs.Add(Outputs["00006"]);

        // Create references from Module "0000" (which has Runner Runner role assigned)
        // Reference to Module "0002" via ModuleParamFromOutputSet (all outputs in the most recent OutputSet should be readable)
        var moduleParamFromOutputSet = new ModuleParamFromOutputSet
        {
            Id = Guid.NewGuid(),
            Name = "referenced_outputset",
            ModuleId = Modules["0000"].Id,
            OutputModuleId = Modules["0002"].Id,
            OrganizationId = Organizations["0"].Id
        };
        dbContext.ModuleInputs.Add(moduleParamFromOutputSet);

        // Reference to Output "Output00004" from Module "0003" via ModuleParamFromOutput (only this specific output by name is readable)
        var moduleParamFromOutput = new ModuleParamFromOutput
        {
            Id = Guid.NewGuid(),
            Name = "referenced_output",
            ModuleId = Modules["0000"].Id,
            OutputModuleId = Modules["0003"].Id,
            OutputName = "Output00004",
            OrganizationId = Organizations["0"].Id
        };
        dbContext.ModuleInputs.Add(moduleParamFromOutput);


        // Create UserOrganizationRoleAssignments for testing (Org "1" for cross-org isolation)
        var org1User = CreateUser("org1-reader@test.com", Organizations["1"].Id);
        dbContext.Users.Add(org1User);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(org1User.Id, Organizations["1"].Id));
        UserOrganizationRoleAssignments["Org1Reader"] = new UserOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Organizations["1"].Id,
            UserId = org1User.Id,
            RoleName = OrganizationRole.Reader
        };
        dbContext.UserOrganizationRoleAssignments.Add(UserOrganizationRoleAssignments["Org1Reader"]);

        // Create UserStackRoleAssignments for testing (one in Org "0", one in Org "1" for cross-org isolation)
        var stack00User = CreateUser("stack00-user@test.com", Organizations["0"].Id);
        dbContext.Users.Add(stack00User);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(stack00User.Id, Organizations["0"].Id));
        UserStackRoleAssignments["Stack00Reader"] = new UserStackRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Organizations["0"].Id,
            StackId = Stacks["00"].Id,
            UserId = stack00User.Id,
            RoleName = StackRole.Reader
        };
        dbContext.UserStackRoleAssignments.Add(UserStackRoleAssignments["Stack00Reader"]);

        var stack10User = CreateUser("stack10-user@test.com", Organizations["1"].Id);
        dbContext.Users.Add(stack10User);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(stack10User.Id, Organizations["1"].Id));
        UserStackRoleAssignments["Stack10Reader"] = new UserStackRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Organizations["1"].Id,
            StackId = Stacks["10"].Id,
            UserId = stack10User.Id,
            RoleName = StackRole.Reader
        };
        dbContext.UserStackRoleAssignments.Add(UserStackRoleAssignments["Stack10Reader"]);

        // Create UserNamespaceRoleAssignments for testing (one in Org "0", one in Org "1" for cross-org isolation)
        var namespace000User = CreateUser("namespace000-user@test.com", Organizations["0"].Id);
        dbContext.Users.Add(namespace000User);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(namespace000User.Id, Organizations["0"].Id));
        UserNamespaceRoleAssignments["Namespace000Reader"] = new UserNamespaceRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Organizations["0"].Id,
            NamespaceId = Namespaces["000"].Id,
            UserId = namespace000User.Id,
            RoleName = NamespaceRole.Reader
        };
        dbContext.UserNamespaceRoleAssignments.Add(UserNamespaceRoleAssignments["Namespace000Reader"]);

        var namespace100User = CreateUser("namespace100-user@test.com", Organizations["1"].Id);
        dbContext.Users.Add(namespace100User);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(namespace100User.Id, Organizations["1"].Id));
        UserNamespaceRoleAssignments["Namespace100Reader"] = new UserNamespaceRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Organizations["1"].Id,
            NamespaceId = Namespaces["100"].Id,
            UserId = namespace100User.Id,
            RoleName = NamespaceRole.Reader
        };
        dbContext.UserNamespaceRoleAssignments.Add(UserNamespaceRoleAssignments["Namespace100Reader"]);

        // Create UserModuleRoleAssignments for testing (one in Org "0", one in Org "1" for cross-org isolation)
        var module0000User = CreateUser("module0000-user@test.com", Organizations["0"].Id);
        dbContext.Users.Add(module0000User);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(module0000User.Id, Organizations["0"].Id));
        UserModuleRoleAssignments["Module0000Reader"] = new UserModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Organizations["0"].Id,
            ModuleId = Modules["0000"].Id,
            UserId = module0000User.Id,
            RoleName = ModuleRole.Reader
        };
        dbContext.UserModuleRoleAssignments.Add(UserModuleRoleAssignments["Module0000Reader"]);

        var module1000User = CreateUser("module1000-user@test.com", Organizations["1"].Id);
        dbContext.Users.Add(module1000User);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(module1000User.Id, Organizations["1"].Id));
        UserModuleRoleAssignments["Module1000Reader"] = new UserModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Organizations["1"].Id,
            ModuleId = Modules["1000"].Id,
            UserId = module1000User.Id,
            RoleName = ModuleRole.Reader
        };
        dbContext.UserModuleRoleAssignments.Add(UserModuleRoleAssignments["Module1000Reader"]);

        // Create principals for no-permission testing
        NoPermissionUser = CreateUser("no-permission@test.com", Organizations["0"].Id);
        NoPermissionServicePrincipal = CreateServicePrincipal("no-permission-sp", Organizations["0"].Id);
        dbContext.Users.Add(NoPermissionUser);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(NoPermissionUser.Id, Organizations["0"].Id));
        dbContext.ServicePrincipals.Add(NoPermissionServicePrincipal);

        // Systematically create principals for Organization "0" roles
        await CreateOrganizationRolePrincipals_Org0(dbContext);

        // Systematically create principals for Runner "0" roles
        await CreateRunnerRolePrincipals_Org0(dbContext);

        // Tier B scope-role Reader principals — one direct-User per scope row.
        CreateScopeReaderPrincipals_Org0(dbContext);

        // Tier B Agent + RunnerModuleSupply seed: Agent0 + Agent0Sibling in Org0, Agent1 in Org1,
        // AgentModuleSupply0 + AgentModuleSupply0Sibling, plus RunnerModuleSupply0Sibling
        // so the runner-chain tests have a sibling row to test isolation against. Includes AgentReader
        // and RunnerReaderSibling Users.
        CreateAgentRunnerScopeEntities(dbContext);

        // Tier A dedicated per-test-class Update/Delete entities — one positive Update + one Delete
        // per smoke class. Each test mutates its own row; no cross-test contention.
        CreateSmokeTestEntities(dbContext);

        // Seed orphaned job cleanup test data
        SeedOrphanedJobTestData(dbContext);

        await dbContext.SaveChangesAsync();
    }

    private async Task CreateOrganizationRolePrincipals_Org0(SnapCdDbContext dbContext)
    {
        var org = Organizations["0"];
        OrganizationPrincipals["0"] = new Dictionary<OrganizationRole, RolePrincipals>();

        foreach (var role in Enum.GetValues<OrganizationRole>())
        {
            var principals = new RolePrincipals();

            // Direct user assignment
            var directUser = CreateUser($"org0-{role.ToString().ToLower()}@test.com", org.Id);
            principals.DirectUser = directUser;
            dbContext.Users.Add(directUser);
            dbContext.OrganizationUsers.Add(CreateOrganizationUser(directUser.Id, org.Id));
            var userRoleAssignment = new UserOrganizationRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                UserId = directUser.Id,
                RoleName = role
            };
            dbContext.UserOrganizationRoleAssignments.Add(userRoleAssignment);

            // Capture Owner role assignment for Get/List tests
            if (role == OrganizationRole.Owner) UserOrganizationRoleAssignments["Owner"] = userRoleAssignment;

            // Direct service principal assignment
            var directSp = CreateServicePrincipal($"org0-{role.ToString().ToLower()}-sp", org.Id);
            principals.DirectServicePrincipal = directSp;
            dbContext.ServicePrincipals.Add(directSp);
            dbContext.ServicePrincipalOrganizationRoleAssignments.Add(new ServicePrincipalOrganizationRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                ServicePrincipalId = directSp.Id,
                RoleName = role
            });

            // Group-based assignment
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = $"Org0{role}Group",
                OrganizationId = org.Id,
                CreatedDateTime = DateTime.UtcNow
            };
            principals.Group = group;
            dbContext.Groups.Add(group);
            dbContext.GroupOrganizationRoleAssignments.Add(new GroupOrganizationRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                GroupId = group.Id,
                RoleName = role
            });

            // User member of group
            var groupUser = CreateUser($"org0-{role.ToString().ToLower()}-groupuser@test.com", org.Id);
            principals.GroupUser = groupUser;
            dbContext.Users.Add(groupUser);
            dbContext.OrganizationUsers.Add(CreateOrganizationUser(groupUser.Id, org.Id));
            dbContext.UserGroupMembers.Add(new UserGroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                UserId = groupUser.Id,
                OrganizationId = org.Id
            });

            // Service principal member of group
            var groupSp = CreateServicePrincipal($"org0-{role.ToString().ToLower()}-groupsp", org.Id);
            principals.GroupServicePrincipal = groupSp;
            dbContext.ServicePrincipals.Add(groupSp);
            dbContext.ServicePrincipalGroupMembers.Add(new ServicePrincipalGroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                ServicePrincipalId = groupSp.Id,
                OrganizationId = org.Id
            });

            // Nested group hierarchy for this role
            var nestedHierarchy = CreateNestedGroupHierarchy(role, org, dbContext);
            principals.NestedGroupUser = nestedHierarchy.User;
            principals.NestedGroupServicePrincipal = nestedHierarchy.ServicePrincipal;

            OrganizationPrincipals["0"][role] = principals;
        }

        await Task.CompletedTask;
    }

    private (SnapCd.Server.Core.Entities.Definition.User User, SnapCd.Server.Core.Entities.Definition.ServicePrincipal ServicePrincipal) CreateNestedGroupHierarchy(
        OrganizationRole role, Organization org, SnapCdDbContext dbContext)
    {
        // Create Grandchild → Child → Parent group hierarchy for this role
        var grandchildGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Org0{role}NestedGrandchildGroup",
            OrganizationId = org.Id,
            CreatedDateTime = DateTime.UtcNow
        };

        var childGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Org0{role}NestedChildGroup",
            OrganizationId = org.Id,
            CreatedDateTime = DateTime.UtcNow
        };

        var parentGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Org0{role}NestedParentGroup",
            OrganizationId = org.Id,
            CreatedDateTime = DateTime.UtcNow
        };

        dbContext.Groups.AddRange(grandchildGroup, childGroup, parentGroup);

        // Parent group has the role
        dbContext.GroupOrganizationRoleAssignments.Add(new GroupOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            GroupId = parentGroup.Id,
            RoleName = role
        });

        // Child group is member of Parent group
        dbContext.GroupGroupMembers.Add(new GroupGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = parentGroup.Id,
            MemberGroupId = childGroup.Id,
            OrganizationId = org.Id
        });

        // Grandchild group is member of Child group
        dbContext.GroupGroupMembers.Add(new GroupGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = childGroup.Id,
            MemberGroupId = grandchildGroup.Id,
            OrganizationId = org.Id
        });

        // User is member of Grandchild group (should inherit Parent's permissions)
        var nestedUser = CreateUser($"org0-{role.ToString().ToLower()}-nested@test.com", org.Id);
        dbContext.Users.Add(nestedUser);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(nestedUser.Id, org.Id));
        dbContext.UserGroupMembers.Add(new UserGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = grandchildGroup.Id,
            UserId = nestedUser.Id,
            OrganizationId = org.Id
        });

        // Service Principal is member of Grandchild group
        var nestedSp = CreateServicePrincipal($"org0-{role.ToString().ToLower()}-nestedsp", org.Id);
        dbContext.ServicePrincipals.Add(nestedSp);
        dbContext.ServicePrincipalGroupMembers.Add(new ServicePrincipalGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = grandchildGroup.Id,
            ServicePrincipalId = nestedSp.Id,
            OrganizationId = org.Id
        });

        return (nestedUser, nestedSp);
    }

    private async Task CreateRunnerRolePrincipals_Org0(SnapCdDbContext dbContext)
    {
        var org = Organizations["0"];
        var runner = Runners["0"];
        RunnerPrincipals["0"] = new Dictionary<RunnerRole, RolePrincipals>();

        foreach (var role in Enum.GetValues<RunnerRole>())
        {
            var principals = new RolePrincipals();

            // Direct user assignment
            var directUser = CreateUser($"rp0-{role.ToString().ToLower()}@test.com", org.Id);
            principals.DirectUser = directUser;
            dbContext.Users.Add(directUser);
            dbContext.OrganizationUsers.Add(CreateOrganizationUser(directUser.Id, org.Id));
            dbContext.UserRunnerRoleAssignments.Add(new UserRunnerRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                RunnerId = runner.Id,
                UserId = directUser.Id,
                RoleName = role
            });

            // Direct service principal assignment
            var directSp = CreateServicePrincipal($"rp0-{role.ToString().ToLower()}-sp", org.Id);
            principals.DirectServicePrincipal = directSp;
            dbContext.ServicePrincipals.Add(directSp);
            dbContext.ServicePrincipalRunnerRoleAssignments.Add(new ServicePrincipalRunnerRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                RunnerId = runner.Id,
                ServicePrincipalId = directSp.Id,
                RoleName = role
            });

            // Group-based assignment
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = $"Rp0{role}Group",
                OrganizationId = org.Id,
                CreatedDateTime = DateTime.UtcNow
            };
            principals.Group = group;
            dbContext.Groups.Add(group);
            dbContext.GroupRunnerRoleAssignments.Add(new GroupRunnerRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                RunnerId = runner.Id,
                GroupId = group.Id,
                RoleName = role
            });

            // User member of group
            var groupUser = CreateUser($"rp0-{role.ToString().ToLower()}-groupuser@test.com", org.Id);
            principals.GroupUser = groupUser;
            dbContext.Users.Add(groupUser);
            dbContext.OrganizationUsers.Add(CreateOrganizationUser(groupUser.Id, org.Id));
            dbContext.UserGroupMembers.Add(new UserGroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                UserId = groupUser.Id,
                OrganizationId = org.Id
            });

            // Service principal member of group
            var groupSp = CreateServicePrincipal($"rp0-{role.ToString().ToLower()}-groupsp", org.Id);
            principals.GroupServicePrincipal = groupSp;
            dbContext.ServicePrincipals.Add(groupSp);
            dbContext.ServicePrincipalGroupMembers.Add(new ServicePrincipalGroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                ServicePrincipalId = groupSp.Id,
                OrganizationId = org.Id
            });

            // Nested group hierarchy for this role
            var nestedHierarchy = CreateNestedRunnerGroupHierarchy(role, org, runner, dbContext);
            principals.NestedGroupUser = nestedHierarchy.User;
            principals.NestedGroupServicePrincipal = nestedHierarchy.ServicePrincipal;

            RunnerPrincipals["0"][role] = principals;
        }

        // Create Runner → Module assignments for testing
        // Assign Runner "0" to Module "0000" (direct module assignment)
        dbContext.RunnerModuleSupplies.Add(new RunnerModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            RunnerId = runner.Id,
            ModuleId = Modules["0000"].Id
        });

        await Task.CompletedTask;
    }

    private (SnapCd.Server.Core.Entities.Definition.User User, SnapCd.Server.Core.Entities.Definition.ServicePrincipal ServicePrincipal) CreateNestedRunnerGroupHierarchy(
        RunnerRole role, Organization org, Runner runner, SnapCdDbContext dbContext)
    {
        // Create Grandchild → Child → Parent group hierarchy for this role
        var grandchildGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Rp0{role}NestedGrandchildGroup",
            OrganizationId = org.Id,
            CreatedDateTime = DateTime.UtcNow
        };

        var childGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Rp0{role}NestedChildGroup",
            OrganizationId = org.Id,
            CreatedDateTime = DateTime.UtcNow
        };

        var parentGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"Rp0{role}NestedParentGroup",
            OrganizationId = org.Id,
            CreatedDateTime = DateTime.UtcNow
        };

        dbContext.Groups.AddRange(grandchildGroup, childGroup, parentGroup);

        // Parent group has the role
        dbContext.GroupRunnerRoleAssignments.Add(new GroupRunnerRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            RunnerId = runner.Id,
            GroupId = parentGroup.Id,
            RoleName = role
        });

        // Child group is member of Parent group
        dbContext.GroupGroupMembers.Add(new GroupGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = parentGroup.Id,
            MemberGroupId = childGroup.Id,
            OrganizationId = org.Id
        });

        // Grandchild group is member of Child group
        dbContext.GroupGroupMembers.Add(new GroupGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = childGroup.Id,
            MemberGroupId = grandchildGroup.Id,
            OrganizationId = org.Id
        });

        // User is member of Grandchild group (should inherit Parent's permissions)
        var nestedUser = CreateUser($"rp0-{role.ToString().ToLower()}-nested@test.com", org.Id);
        dbContext.Users.Add(nestedUser);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(nestedUser.Id, org.Id));
        dbContext.UserGroupMembers.Add(new UserGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = grandchildGroup.Id,
            UserId = nestedUser.Id,
            OrganizationId = org.Id
        });

        // Service Principal is member of Grandchild group
        var nestedSp = CreateServicePrincipal($"rp0-{role.ToString().ToLower()}-nestedsp", org.Id);
        dbContext.ServicePrincipals.Add(nestedSp);
        dbContext.ServicePrincipalGroupMembers.Add(new ServicePrincipalGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = grandchildGroup.Id,
            ServicePrincipalId = nestedSp.Id,
            OrganizationId = org.Id
        });

        return (nestedUser, nestedSp);
    }

    /// <summary>
    /// Tier B (RoleResolution) seed — creates one direct-User per scope-role-Reader per scope row
    /// in Org "0". Only Reader is seeded; Tier B tests visibility, which the minimal sufficient
    /// role grants. The scope tree mirrors the binary-tree hierarchy:
    ///   Stack00 / Stack01 (sibling) → Namespace000 / Namespace001 (sibling) → Module0000 / Module0001 (sibling).
    /// </summary>
    private void CreateScopeReaderPrincipals_Org0(SnapCdDbContext dbContext)
    {
        var org = Organizations["0"];

        // StackReader on Stack00 + Stack01
        SeedStackReader(dbContext, org.Id, Stacks["00"].Id, "Stack00.Reader");
        SeedStackReader(dbContext, org.Id, Stacks["01"].Id, "Stack01.Reader");

        // NamespaceReader on Namespace000 + Namespace001 (siblings under Stack00)
        SeedNamespaceReader(dbContext, org.Id, Namespaces["000"].Id, "Namespace000.Reader");
        SeedNamespaceReader(dbContext, org.Id, Namespaces["001"].Id, "Namespace001.Reader");

        // ModuleReader on Module0000 + Module0001 (siblings under Namespace000)
        SeedModuleReader(dbContext, org.Id, Modules["0000"].Id, "Module0000.Reader");
        SeedModuleReader(dbContext, org.Id, Modules["0001"].Id, "Module0001.Reader");
    }

    private void SeedStackReader(SnapCdDbContext dbContext, Guid orgId, Guid stackId, string key)
    {
        var user = CreateUser($"{key.ToLower()}@test.com", orgId);
        dbContext.Users.Add(user);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(user.Id, orgId));
        dbContext.UserStackRoleAssignments.Add(new UserStackRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            StackId = stackId,
            UserId = user.Id,
            RoleName = StackRole.Reader,
        });
        ScopeReaderUsers[key] = user;
    }

    private void SeedNamespaceReader(SnapCdDbContext dbContext, Guid orgId, Guid namespaceId, string key)
    {
        var user = CreateUser($"{key.ToLower()}@test.com", orgId);
        dbContext.Users.Add(user);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(user.Id, orgId));
        dbContext.UserNamespaceRoleAssignments.Add(new UserNamespaceRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            NamespaceId = namespaceId,
            UserId = user.Id,
            RoleName = NamespaceRole.Reader,
        });
        ScopeReaderUsers[key] = user;
    }

    private void SeedModuleReader(SnapCdDbContext dbContext, Guid orgId, Guid moduleId, string key)
    {
        var user = CreateUser($"{key.ToLower()}@test.com", orgId);
        dbContext.Users.Add(user);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(user.Id, orgId));
        dbContext.UserModuleRoleAssignments.Add(new UserModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ModuleId = moduleId,
            UserId = user.Id,
            RoleName = ModuleRole.Reader,
        });
        ScopeReaderUsers[key] = user;
    }

    /// <summary>
    /// Tier B (RoleResolution) seed for Agent / Runner chains. Creates:
    ///   Org0 — Agent0 + Agent0Sibling + their AgentModuleSupplies + RunnerModuleSupply0Sibling
    ///         (on Module0001 via Runner0).
    ///   Org1 — Agent1 (for cross-org sweep tests).
    ///   AgentRole.Reader and Sibling Users on each Agent and Runner so the visibility-by-scope tests
    ///   have all the principals they need.
    /// Keyed entries:
    ///   Agents["0"], Agents["0Sibling"], Agents["1"]
    ///   AgentModuleSupplies["0"] (Agent0 → Module0000), AgentModuleSupplies["0Sibling"] (Agent0Sibling → Module0001)
    ///   RunnerModuleSupplies["0Sibling"] (Runner0 → Module0001)
    ///   ScopeReaderUsers["Agent0.Reader"], ["Agent0Sibling.Reader"], ["Runner0Sibling.Reader"]
    /// </summary>
    private void CreateAgentRunnerScopeEntities(SnapCdDbContext dbContext)
    {
        var org0 = Organizations["0"];
        var org1 = Organizations["1"];

        // ---- Agent0 + ServicePrincipal ----
        var agent0Sp = CreateServicePrincipal("agent0-sp", org0.Id);
        dbContext.ServicePrincipals.Add(agent0Sp);
        Agents["0"] = new Agent
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            ServicePrincipalId = agent0Sp.Id,
            Name = "Agent0",
            IsDisabled = false,
            AllowMultipleInstances = false,
            IsSuppliedToAllModules = false,
        };
        dbContext.Agents.Add(Agents["0"]);

        // ---- Agent0Sibling ----
        var agent0SiblingSp = CreateServicePrincipal("agent0sibling-sp", org0.Id);
        dbContext.ServicePrincipals.Add(agent0SiblingSp);
        Agents["0Sibling"] = new Agent
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            ServicePrincipalId = agent0SiblingSp.Id,
            Name = "Agent0Sibling",
            IsDisabled = false,
            AllowMultipleInstances = false,
            IsSuppliedToAllModules = false,
        };
        dbContext.Agents.Add(Agents["0Sibling"]);

        // ---- Agent1 (cross-org) ----
        var agent1Sp = CreateServicePrincipal("agent1-sp", org1.Id);
        dbContext.ServicePrincipals.Add(agent1Sp);
        Agents["1"] = new Agent
        {
            Id = Guid.NewGuid(),
            OrganizationId = org1.Id,
            ServicePrincipalId = agent1Sp.Id,
            Name = "Agent1",
            IsDisabled = false,
            AllowMultipleInstances = false,
            IsSuppliedToAllModules = false,
        };
        dbContext.Agents.Add(Agents["1"]);

        // ---- AgentModuleSupply0 (Agent0 → Module0000) ----
        AgentModuleSupplies["0"] = new AgentModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            AgentId = Agents["0"].Id,
            ModuleId = Modules["0000"].Id,
        };
        dbContext.AgentModuleSupplies.Add(AgentModuleSupplies["0"]);

        // ---- AgentModuleSupply0Sibling (Agent0Sibling → Module0001) ----
        AgentModuleSupplies["0Sibling"] = new AgentModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            AgentId = Agents["0Sibling"].Id,
            ModuleId = Modules["0001"].Id,
        };
        dbContext.AgentModuleSupplies.Add(AgentModuleSupplies["0Sibling"]);

        // ---- Runner0Sibling + assignment + Reader principal ----
        // The base seed at CreateRunnerRolePrincipals_Org0 creates Runner0 + assignment to Module0000.
        // Add a sibling runner with its own assignment so RunnerChain tests can verify isolation
        // (Runner0.Reader sees Runner0's assignment but not Runner0Sibling's).
        var runner0SiblingSp = CreateServicePrincipal("runner0sibling-sp", org0.Id);
        dbContext.ServicePrincipals.Add(runner0SiblingSp);
        var runner0Sibling = CreateRunner("Runner0Sibling", org0.Id, runner0SiblingSp.Id);
        Runners["0Sibling"] = runner0Sibling;
        dbContext.Runners.Add(runner0Sibling);

        RunnerModuleSupplies["0Sibling"] = new RunnerModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            RunnerId = runner0Sibling.Id,
            ModuleId = Modules["0001"].Id,
        };
        dbContext.RunnerModuleSupplies.Add(RunnerModuleSupplies["0Sibling"]);

        SeedRunnerReader(dbContext, org0.Id, runner0Sibling.Id, "Runner0Sibling.Reader");

        // ---- AgentReader on Agent0 ----
        SeedAgentReader(dbContext, org0.Id, Agents["0"].Id, "Agent0.Reader");
        // ---- AgentReaderSibling on Agent0Sibling ----
        SeedAgentReader(dbContext, org0.Id, Agents["0Sibling"].Id, "Agent0Sibling.Reader");

        // ---- Missions at each scope, owned by Agent0 ----
        // Used by MissionCrossScope_RoleResolutionTests to verify both scope-side and agent-side
        // visibility on the bespoke ReadQuery overrides.
        OrganizationMissions["0"] = new SnapCd.Server.Core.Entities.Definition.Missions.OrganizationMission
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            AgentId = Agents["0"].Id,
            MissionType = MissionType.AutoDiagnose,
            IsDisabled = false,
        };
        dbContext.OrganizationMissions.Add(OrganizationMissions["0"]);

        StackMissions["0"] = new SnapCd.Server.Core.Entities.Definition.Missions.StackMission
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            AgentId = Agents["0"].Id,
            StackId = Stacks["00"].Id,
            MissionType = MissionType.AutoDiagnose,
            IsDisabled = false,
        };
        dbContext.StackMissions.Add(StackMissions["0"]);

        NamespaceMissions["0"] = new SnapCd.Server.Core.Entities.Definition.Missions.NamespaceMission
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            AgentId = Agents["0"].Id,
            NamespaceId = Namespaces["000"].Id,
            MissionType = MissionType.AutoDiagnose,
            IsDisabled = false,
        };
        dbContext.NamespaceMissions.Add(NamespaceMissions["0"]);

        ModuleMissions["0"] = new SnapCd.Server.Core.Entities.Definition.Missions.ModuleMission
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            AgentId = Agents["0"].Id,
            ModuleId = Modules["0000"].Id,
            MissionType = MissionType.AutoDiagnose,
            IsDisabled = false,
        };
        dbContext.ModuleMissions.Add(ModuleMissions["0"]);

        // Sibling mission rows owned by Agent0Sibling — for cross-agent isolation assertions.
        ModuleMissions["0Sibling"] = new SnapCd.Server.Core.Entities.Definition.Missions.ModuleMission
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            AgentId = Agents["0Sibling"].Id,
            ModuleId = Modules["0001"].Id,
            MissionType = MissionType.AutoDiagnose,
            IsDisabled = false,
        };
        dbContext.ModuleMissions.Add(ModuleMissions["0Sibling"]);
    }

    private void SeedAgentReader(SnapCdDbContext dbContext, Guid orgId, Guid agentId, string key)
    {
        var user = CreateUser($"{key.ToLower()}@test.com", orgId);
        dbContext.Users.Add(user);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(user.Id, orgId));
        dbContext.UserAgentRoleAssignments.Add(new UserAgentRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AgentId = agentId,
            UserId = user.Id,
            RoleName = AgentRole.Reader,
        });
        ScopeReaderUsers[key] = user;
    }

    private void SeedRunnerReader(SnapCdDbContext dbContext, Guid orgId, Guid runnerId, string key)
    {
        var user = CreateUser($"{key.ToLower()}@test.com", orgId);
        dbContext.Users.Add(user);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(user.Id, orgId));
        dbContext.UserRunnerRoleAssignments.Add(new UserRunnerRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            RunnerId = runnerId,
            UserId = user.Id,
            RoleName = RunnerRole.Reader,
        });
        ScopeReaderUsers[key] = user;
    }

    private void CreateSmokeTestEntities(SnapCdDbContext dbContext)
    {
        var org0 = Organizations["0"];

        // Stack_SmokeTests: dedicated Update + Delete Stack rows in Org0.
        SmokeStacks["Stack_SmokeTests_UpdateCan"] = CreateTestStack("Stack_SmokeTests_UpdateCan", org0.Id, dbContext);
        SmokeStacks["Stack_SmokeTests_DeleteCan"] = CreateTestStack("Stack_SmokeTests_DeleteCan", org0.Id, dbContext);

        // Namespace_SmokeTests: dedicated under Stack00.
        SmokeNamespaces["Namespace_SmokeTests_UpdateCan"] = CreateTestNamespace("Namespace_SmokeTests_UpdateCan", "00", dbContext);
        SmokeNamespaces["Namespace_SmokeTests_DeleteCan"] = CreateTestNamespace("Namespace_SmokeTests_DeleteCan", "00", dbContext);

        // Module_SmokeTests: dedicated under Namespace000, using Runner0.
        var updateModule = CreateModule("Module_SmokeTests_UpdateCan", Namespaces["000"].Id, Runners["0"].Id, org0.Id);
        var deleteModule = CreateModule("Module_SmokeTests_DeleteCan", Namespaces["000"].Id, Runners["0"].Id, org0.Id);
        dbContext.Modules.Add(updateModule);
        dbContext.Modules.Add(deleteModule);
        SmokeModules["Module_SmokeTests_UpdateCan"] = updateModule;
        SmokeModules["Module_SmokeTests_DeleteCan"] = deleteModule;

        // ModuleHook_SmokeTests: dedicated under Module0000. There's a unique index on
        // (ModuleId, Task, Phase), so vary Phase between the two seeded rows.
        SmokeModuleHooks["ModuleHook_SmokeTests_UpdateCan"] =
            CreateAndAddModuleHook(dbContext, org0.Id, Modules["0000"].Id, HookTask.Apply, HookPhase.Before, "Update");
        SmokeModuleHooks["ModuleHook_SmokeTests_DeleteCan"] =
            CreateAndAddModuleHook(dbContext, org0.Id, Modules["0000"].Id, HookTask.Apply, HookPhase.After, "Delete");

        // AgentModuleSupply_SmokeTests: dedicated under Agent0Sibling.
        // Use Modules 0002 + 0003 (created by the orphaned-job seed elsewhere) to avoid
        // duplicate-assignment conflicts with the Tier B seed.
        var agentUpdate = new AgentModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            AgentId = Agents["0Sibling"].Id,
            ModuleId = Modules["0002"].Id,
        };
        var agentDelete = new AgentModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            AgentId = Agents["0Sibling"].Id,
            ModuleId = Modules["0003"].Id,
        };
        dbContext.AgentModuleSupplies.Add(agentUpdate);
        dbContext.AgentModuleSupplies.Add(agentDelete);
        SmokeAgentSupplies["AgentModuleSupply_SmokeTests_UpdateCan"] = agentUpdate;
        SmokeAgentSupplies["AgentModuleSupply_SmokeTests_DeleteCan"] = agentDelete;

        // RunnerModuleSupply_SmokeTests: dedicated under Runner0Sibling.
        var runnerUpdate = new RunnerModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            RunnerId = Runners["0Sibling"].Id,
            ModuleId = Modules["0002"].Id,
        };
        var runnerDelete = new RunnerModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = org0.Id,
            RunnerId = Runners["0Sibling"].Id,
            ModuleId = Modules["0003"].Id,
        };
        dbContext.RunnerModuleSupplies.Add(runnerUpdate);
        dbContext.RunnerModuleSupplies.Add(runnerDelete);
        SmokeRunnerSupplies["RunnerModuleSupply_SmokeTests_UpdateCan"] = runnerUpdate;
        SmokeRunnerSupplies["RunnerModuleSupply_SmokeTests_DeleteCan"] = runnerDelete;
    }

    private static ModuleHook CreateAndAddModuleHook(SnapCdDbContext dbContext, Guid orgId, Guid moduleId, HookTask task, HookPhase phase, string nameSuffix)
    {
        var hook = new ModuleHook
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ModuleId = moduleId,
            Task = task,
            Phase = phase,
            Script = $"echo 'hook-{nameSuffix}'",
        };
        dbContext.ModuleHooks.Add(hook);
        return hook;
    }

    #region Helper Methods

    private User CreateUser(string email, Guid organizationId)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            IsDisabled = false,
            CreatedDateTime = DateTime.UtcNow
        };
    }

    private OrganizationUser CreateOrganizationUser(Guid userId, Guid organizationId)
    {
        return new OrganizationUser
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = organizationId,
            JoinedAt = DateTime.UtcNow,
            InvitationCompleted = true
        };
    }

    private ServicePrincipal CreateServicePrincipal(string name, Guid organizationId)
    {
        return new ServicePrincipal
        {
            Id = Guid.NewGuid(),
            DisplayName = name,
            ClientId = name.ToLower().Replace(" ", "-"),
            IsDisabled = false,
            OrganizationId = organizationId
        };
    }

    private Stack CreateStack(string name, Guid organizationId)
    {
        return new Stack
        {
            Id = Guid.NewGuid(),
            Name = name,
            OrganizationId = organizationId,
            CreatedDateTime = DateTime.UtcNow
        };
    }

    private Namespace CreateNamespace(string name, Guid stackId, Guid organizationId)
    {
        return new Namespace
        {
            Id = Guid.NewGuid(),
            Name = name,
            StackId = stackId,
            OrganizationId = organizationId,
            CreatedDateTime = DateTime.UtcNow
        };
    }

    private Runner CreateRunner(string name, Guid organizationId, Guid servicePrincipalId)
    {
        return new Runner
        {
            Id = Guid.NewGuid(),
            Name = name,
            OrganizationId = organizationId,
            ServicePrincipalId = servicePrincipalId
        };
    }

    private Module CreateModule(string name, Guid namespaceId, Guid runnerId, Guid organizationId)
    {
        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = name,
            NamespaceId = namespaceId,
            RunnerId = runnerId,
            OrganizationId = organizationId,
            SourceUrl = $"https://github.com/test/{name.ToLower()}",
            SourceRevision = "main",
            SourceSubdirectory = "terraform",
            CreatedDateTime = DateTime.UtcNow
        };
        module.ModuleSaga = new ModuleSaga
        {
            CorrelationId = module.Id,
            OrganizationId = organizationId,
            RowVersion = [],
            CurrentState = nameof(ModuleStateMachine.Gatekeeping),
            DesiredStateHeadline = DesiredStateHeadline.Applied,
            QueuedDesiredStateHeadline = null
        };
        module.ModuleModifiedSaga = new ModuleModifiedSaga
        {
            CorrelationId = module.Id,
            OrganizationId = organizationId,
            RowVersion = [],
            CurrentState = nameof(ModuleModifiedStateMachine.Idle),
            LastUpdated = null,
            TimeoutTokenId = null
        };
        return module;
    }

    private ModuleInput CreateModuleInput(string name, Guid moduleId, Guid organizationId)
    {
        return new ModuleInput
        {
            Id = Guid.NewGuid(),
            Name = name,
            ModuleId = moduleId,
            OrganizationId = organizationId,
            InputKind = InputKind.Param
        };
    }

    private NamespaceInput CreateNamespaceInput(string name, Guid namespaceId, Guid organizationId)
    {
        return new NamespaceInput
        {
            Id = Guid.NewGuid(),
            Name = name,
            NamespaceId = namespaceId,
            OrganizationId = organizationId,
            InputKind = InputKind.Param,
            UsageMode = NamespaceInputUsageMode.UseIfSelected
        };
    }

    private OutputSet CreateOutputSet(string checksum, Guid moduleId, Guid organizationId)
    {
        return new OutputSet
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = organizationId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Checksum = checksum
        };
    }

    private Output CreateOutput(string name, Guid outputSetId, Guid organizationId)
    {
        return new LiteralOutput
        {
            Id = Guid.NewGuid(),
            Name = name,
            OutputSetId = outputSetId,
            OrganizationId = organizationId,
            Type = "string",
            Value = "test-value"
        };
    }

    private VariableSet CreateVariableSet(string checksum, Guid moduleId, Guid organizationId)
    {
        return new VariableSet
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = organizationId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Checksum = checksum
        };
    }

    private Variable CreateInput(string name, Guid variableSetId, Guid organizationId)
    {
        return new Variable
        {
            Id = Guid.NewGuid(),
            Name = name,
            VariableSetId = variableSetId,
            OrganizationId = organizationId,
            Type = "string"
        };
    }

    private StackSecret CreateStackSecret(string name, Guid stackId, Guid organizationId)
    {
        return new StackSecret
        {
            Id = Guid.NewGuid(),
            Name = name,
            StackId = stackId,
            OrganizationId = organizationId
        };
    }

    private NamespaceSecret CreateNamespaceSecret(string name, Guid namespaceId, Guid organizationId)
    {
        return new NamespaceSecret
        {
            Id = Guid.NewGuid(),
            Name = name,
            NamespaceId = namespaceId,
            OrganizationId = organizationId
        };
    }

    private ModuleSecret CreateModuleSecret(string name, Guid moduleId, Guid organizationId)
    {
        return new ModuleSecret
        {
            Id = Guid.NewGuid(),
            Name = name,
            ModuleId = moduleId,
            OrganizationId = organizationId
        };
    }

    private ModuleParamFromSecret CreateModuleInputFromSecret(string name, Guid moduleId, Guid secretId, Guid organizationId)
    {
        return new ModuleParamFromSecret
        {
            Id = Guid.NewGuid(),
            Name = name,
            ModuleId = moduleId,
            SecretId = secretId,
            OrganizationId = organizationId
        };
    }

    private NamespaceParamFromSecret CreateNamespaceInputFromSecret(string name, Guid namespaceId, Guid secretId, Guid organizationId)
    {
        return new NamespaceParamFromSecret
        {
            Id = Guid.NewGuid(),
            Name = name,
            NamespaceId = namespaceId,
            SecretId = secretId,
            OrganizationId = organizationId,
            InputKind = InputKind.Param,
            UsageMode = NamespaceInputUsageMode.UseIfSelected
        };
    }

    private ModuleJob CreateModuleJob(Guid moduleId, Guid organizationId)
    {
        return new ModuleJob
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = organizationId,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            JobType = "Apply"
        };
    }

    /// <summary>
    /// Creates a test module for use in Update/Delete tests.
    /// The module is added to the dbContext but NOT saved yet.
    /// </summary>
    public Module CreateTestModule(string name, string namespacePath, SnapCdDbContext dbContext)
    {
        var ns = Namespaces[namespacePath];
        var module = CreateModule(name, ns.Id, Runners["0"].Id, ns.OrganizationId);
        dbContext.Modules.Add(module);
        return module;
    }

    /// <summary>
    /// Creates a test stack for use in Update/Delete tests.
    /// The stack is added to the dbContext but NOT saved yet.
    /// </summary>
    public Stack CreateTestStack(string name, Guid organizationId, SnapCdDbContext dbContext)
    {
        var stack = CreateStack(name, organizationId);
        dbContext.Stacks.Add(stack);
        return stack;
    }

    /// <summary>
    /// Creates a test namespace for use in Update/Delete tests.
    /// The namespace is added to the dbContext but NOT saved yet.
    /// </summary>
    public Namespace CreateTestNamespace(string name, string stackPath, SnapCdDbContext dbContext)
    {
        var stack = Stacks[stackPath];
        var ns = CreateNamespace(name, stack.Id, stack.OrganizationId);
        dbContext.Namespaces.Add(ns);
        return ns;
    }

    /// <summary>
    /// Creates a test Runner for use in Update/Delete tests.
    /// The Runner is added to the dbContext but NOT saved yet.
    /// </summary>
    public Runner CreateTestRunner(string name, Guid organizationId, SnapCdDbContext dbContext)
    {
        var servicePrincipal = CreateServicePrincipal($"{name}-sp", organizationId);
        dbContext.ServicePrincipals.Add(servicePrincipal);
        var runner = CreateRunner(name, organizationId, servicePrincipal.Id);
        dbContext.Runners.Add(runner);
        return runner;
    }

    /// <summary>
    /// Creates a test module input for use in Update/Delete tests.
    /// The module input is added to the dbContext but NOT saved yet.
    /// </summary>
    public ModuleInput CreateTestModuleInput(string name, Guid moduleId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var moduleInput = CreateModuleInput(name, moduleId, organizationId);
        dbContext.ModuleInputs.Add(moduleInput);
        return moduleInput;
    }

    /// <summary>
    /// Creates a test namespace input for use in Update/Delete tests.
    /// The namespace input is added to the dbContext but NOT saved yet.
    /// </summary>
    public NamespaceInput CreateTestNamespaceInput(string name, Guid namespaceId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var namespaceInput = CreateNamespaceInput(name, namespaceId, organizationId);
        dbContext.NamespaceInputs.Add(namespaceInput);
        return namespaceInput;
    }

    /// <summary>
    /// Creates a test output set for use in Update/Delete tests.
    /// The output set is added to the dbContext but NOT saved yet.
    /// </summary>
    public OutputSet CreateTestOutputSet(string checksum, Guid moduleId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var outputSet = CreateOutputSet(checksum, moduleId, organizationId);
        dbContext.OutputSets.Add(outputSet);
        return outputSet;
    }

    /// <summary>
    /// Creates a test output for use in Update/Delete tests.
    /// The output is added to the dbContext but NOT saved yet.
    /// </summary>
    public Output CreateTestOutput(string name, Guid outputSetId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var output = CreateOutput(name, outputSetId, organizationId);
        dbContext.Outputs.Add(output);
        return output;
    }

    /// <summary>
    /// Creates a test input set for use in Update/Delete tests.
    /// The input set is added to the dbContext but NOT saved yet.
    /// </summary>
    public VariableSet CreateTestVariableSet(string checksum, Guid moduleId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var variableSet = CreateVariableSet(checksum, moduleId, organizationId);
        dbContext.VariableSets.Add(variableSet);
        return variableSet;
    }

    /// <summary>
    /// Creates a test input for use in Update/Delete tests.
    /// The input is added to the dbContext but NOT saved yet.
    /// </summary>
    public Variable CreateTestInput(string name, Guid variableSetId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var input = CreateInput(name, variableSetId, organizationId);
        dbContext.Variables.Add(input);
        return input;
    }

    /// <summary>
    /// Creates a test secret scoped to stack for use in Update/Delete tests.
    /// The secret is added to the dbContext but NOT saved yet.
    /// </summary>
    public StackSecret CreateTestStackSecret(string name, Guid stackId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var secret = CreateStackSecret(name, stackId, organizationId);
        dbContext.StackSecrets.Add(secret);
        return secret;
    }

    /// <summary>
    /// Creates a test secret scoped to namespace for use in Update/Delete tests.
    /// The secret is added to the dbContext but NOT saved yet.
    /// </summary>
    public NamespaceSecret CreateTestNamespaceSecret(string name, Guid namespaceId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var secret = CreateNamespaceSecret(name, namespaceId, organizationId);
        dbContext.NamespaceSecrets.Add(secret);
        return secret;
    }

    /// <summary>
    /// Creates a test secret scoped to module for use in Update/Delete tests.
    /// The secret is added to the dbContext but NOT saved yet.
    /// </summary>
    public ModuleSecret CreateTestModuleSecret(string name, Guid moduleId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var secret = CreateModuleSecret(name, moduleId, organizationId);
        dbContext.ModuleSecrets.Add(secret);
        return secret;
    }

    /// <summary>
    /// Creates a test user for use in role assignment tests.
    /// The user is added to the dbContext but NOT saved yet.
    /// </summary>
    public User CreateTestUser(string email, Guid organizationId, SnapCdDbContext dbContext)
    {
        var user = CreateUser(email, organizationId);
        dbContext.Users.Add(user);
        dbContext.OrganizationUsers.Add(CreateOrganizationUser(user.Id, organizationId));
        return user;
    }

    private UserOrganizationRoleAssignment CreateUserOrganizationRoleAssignment(Guid userId, Guid organizationId)
    {
        return new UserOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            RoleName = OrganizationRole.Reader
        };
    }

    /// <summary>
    /// Creates a test user organization role assignment for use in Update/Delete tests.
    /// The assignment is added to the dbContext but NOT saved yet.
    /// </summary>
    public UserOrganizationRoleAssignment CreateTestUserOrganizationRoleAssignment(Guid userId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var assignment = CreateUserOrganizationRoleAssignment(userId, organizationId);
        dbContext.UserOrganizationRoleAssignments.Add(assignment);
        return assignment;
    }

    private UserStackRoleAssignment CreateUserStackRoleAssignment(Guid userId, Guid stackId, Guid organizationId)
    {
        return new UserStackRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StackId = stackId,
            UserId = userId,
            RoleName = StackRole.Reader
        };
    }

    /// <summary>
    /// Creates a test user stack role assignment for use in Update/Delete tests.
    /// The assignment is added to the dbContext but NOT saved yet.
    /// </summary>
    public UserStackRoleAssignment CreateTestUserStackRoleAssignment(Guid userId, Guid stackId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var assignment = CreateUserStackRoleAssignment(userId, stackId, organizationId);
        dbContext.UserStackRoleAssignments.Add(assignment);
        return assignment;
    }

    private UserNamespaceRoleAssignment CreateUserNamespaceRoleAssignment(Guid userId, Guid namespaceId, Guid organizationId)
    {
        return new UserNamespaceRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = namespaceId,
            UserId = userId,
            RoleName = NamespaceRole.Reader
        };
    }

    /// <summary>
    /// Creates a test user namespace role assignment for use in Update/Delete tests.
    /// The assignment is added to the dbContext but NOT saved yet.
    /// </summary>
    public UserNamespaceRoleAssignment CreateTestUserNamespaceRoleAssignment(Guid userId, Guid namespaceId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var assignment = CreateUserNamespaceRoleAssignment(userId, namespaceId, organizationId);
        dbContext.UserNamespaceRoleAssignments.Add(assignment);
        return assignment;
    }

    private UserModuleRoleAssignment CreateUserModuleRoleAssignment(Guid userId, Guid moduleId, Guid organizationId)
    {
        return new UserModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = moduleId,
            UserId = userId,
            RoleName = ModuleRole.Reader
        };
    }

    /// <summary>
    /// Creates a test user module role assignment for use in Update/Delete tests.
    /// The assignment is added to the dbContext but NOT saved yet.
    /// </summary>
    public UserModuleRoleAssignment CreateTestUserModuleRoleAssignment(Guid userId, Guid moduleId, Guid organizationId, SnapCdDbContext dbContext)
    {
        var assignment = CreateUserModuleRoleAssignment(userId, moduleId, organizationId);
        dbContext.UserModuleRoleAssignments.Add(assignment);
        return assignment;
    }

    #endregion

    #region Orphaned Job Test Data

    private void SeedOrphanedJobTestData(SnapCdDbContext dbContext)
    {
        var org = Organizations["0"];
        var module = Modules["0000"];

        // 1. Orphaned Apply job (unfinalized, no saga)
        OrphanedJobTestData["OrphanedApply"] = new ModuleJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            ModuleId = module.Id,
            TimestampStart = DateTimeOffset.UtcNow,
            TimestampEnd = null, // Unfinalized
            JobType = nameof(ApplyJobSaga),
            Status = ExecutionStatus.Running,
            CreatedDateTime = DateTime.UtcNow
        };
        dbContext.ModuleJobs.Add(OrphanedJobTestData["OrphanedApply"]);

        // 2. Orphaned Destroy job (unfinalized, no saga)
        OrphanedJobTestData["OrphanedDestroy"] = new ModuleJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            ModuleId = module.Id,
            TimestampStart = DateTimeOffset.UtcNow,
            TimestampEnd = null, // Unfinalized
            JobType = nameof(DestroyJobSaga),
            Status = ExecutionStatus.Running,
            CreatedDateTime = DateTime.UtcNow
        };
        dbContext.ModuleJobs.Add(OrphanedJobTestData["OrphanedDestroy"]);

        // 3. Non-orphaned Apply job (unfinalized, with saga)
        OrphanedJobTestData["NonOrphanedApply"] = new ModuleJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            ModuleId = module.Id,
            TimestampStart = DateTimeOffset.UtcNow,
            TimestampEnd = null, // Unfinalized
            JobType = nameof(ApplyJobSaga),
            Status = ExecutionStatus.Running,
            CreatedDateTime = DateTime.UtcNow
        };
        dbContext.ModuleJobs.Add(OrphanedJobTestData["NonOrphanedApply"]);

        // Create matching saga for non-orphaned job
        OrphanedJobTestSagas["NonOrphanedApply"] = new ApplyJobSaga
        {
            CorrelationId = OrphanedJobTestData["NonOrphanedApply"].Id,
            OrganizationId = org.Id,
            CurrentState = "Running",
            ModuleId = module.Id,
            RunnerId = Runners["0"].Id,
            DeclaredJson = "{}",
            RunnerName = "TestRunner"
        };
        dbContext.Set<ApplyJobSaga>().Add(OrphanedJobTestSagas["NonOrphanedApply"]);

        // 4. Finalized job without saga (should NOT be detected as orphaned)
        OrphanedJobTestData["FinalizedNoSaga"] = new ModuleJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            ModuleId = module.Id,
            TimestampStart = DateTimeOffset.UtcNow.AddMinutes(-5),
            TimestampEnd = DateTimeOffset.UtcNow, // Finalized
            JobType = nameof(ApplyJobSaga),
            Status = ExecutionStatus.Completed,
            CreatedDateTime = DateTime.UtcNow
        };
        dbContext.ModuleJobs.Add(OrphanedJobTestData["FinalizedNoSaga"]);
    }

    #endregion

    #region Test Helpers

    public SnapCdDbContext CreateDbContext()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Fixture not initialized");

        var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<SnapCdDbContext>();
    }

    public TestPrincipalProvider CreatePrincipalProvider(Guid principalId, PrincipalDiscriminator discriminator, Guid organizationId)
    {
        return new TestPrincipalProvider(principalId, discriminator, organizationId);
    }

    public IBus CreateMockBus()
    {
        var mockBus = new Mock<IBus>();
        mockBus.Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mockBus.Object;
    }

    public QuotaService CreateMockQuotaService()
    {
        var mockLicenseInfoProvider = new Mock<ILicenseInfoProvider>();
        var quotaGatingService = new QuotaGatingService(mockLicenseInfoProvider.Object);
        return new QuotaService(quotaGatingService);
    }

    public IOptions<ModuleRepositorySettings> CreateModuleSettings(bool emitCreateEvents = true, bool emitUpdateEvents = true, bool emitDeleteEvents = true)
    {
        return Options.Create(new ModuleRepositorySettings
        {
            EmitCreateEvents = emitCreateEvents,
            EmitUpdateEvents = emitUpdateEvents,
            EmitDeleteEvents = emitDeleteEvents
        });
    }

    public IOptions<NamespaceRepositorySettings> CreateNamespaceSettings(bool emitCreateEvents = true, bool emitUpdateEvents = true, bool emitDeleteEvents = true)
    {
        return Options.Create(new NamespaceRepositorySettings
        {
            EmitCreateEvents = emitCreateEvents,
            EmitUpdateEvents = emitUpdateEvents,
            EmitDeleteEvents = emitDeleteEvents
        });
    }

    #endregion

    public async Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        if (_databaseContainer != null) await _databaseContainer.DisposeAsync();
    }
}