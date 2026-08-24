// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Contracts;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.Services.ViewManagement;
using SnapCd.Server.Host.Database;
using Testcontainers.MsSql;
using Module = SnapCd.Server.Core.Entities.Definition.Module;
using Namespace = SnapCd.Server.Core.Entities.Definition.Namespace;
using Organization = SnapCd.Server.Core.Entities.Definition.Organization;
using Runner = SnapCd.Server.Core.Entities.Definition.Runner;
using ServicePrincipal = SnapCd.Server.Core.Entities.Definition.ServicePrincipal;
using Stack = SnapCd.Server.Core.Entities.Definition.Stack;

namespace SnapCd.Server.Core.Tests.Infrastructure;

[CollectionDefinition("DependencyGraphConcurrency")]
public class DependencyGraphConcurrencyCollection : ICollectionFixture<DependencyGraphConcurrencyFixture>
{
}

/// <summary>
/// Dedicated database and stack for the dependency-graph concurrency tests. These run many
/// sessions against one module pair, so they need a scope no other test writes to.
/// </summary>
public class DependencyGraphConcurrencyFixture : IAsyncLifetime
{
    private const string DatabaseName = "SnapCdConcurrency";
    private const string SaPassword = "TestPass123!";

    private IContainer? _databaseContainer;
    private ServiceProvider? _serviceProvider;

    public string ConnectionString { get; private set; } = null!;
    public Guid OrganizationId { get; private set; }
    public Guid ConsumerModuleId { get; private set; }
    public Guid ProducerModuleId { get; private set; }

    public async Task InitializeAsync()
    {
        _databaseContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword(SaPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        await _databaseContainer.StartAsync();

        // Read-committed snapshot cannot be set on master, so these tests get their own database.
        var masterConnectionString = ((MsSqlContainer)_databaseContainer).GetConnectionString();
        await CreateDatabaseAsync(masterConnectionString);

        ConnectionString = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = DatabaseName,
            Password = SaPassword
        }.ConnectionString;

        var services = new ServiceCollection();
        services.AddDbContext<SelfHostedSnapCdDbContext>(options =>
        {
            options.UseSqlServer(ConnectionString, sql => sql.MigrationsAssembly("SnapCd.Server.Host"));
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        services.AddScoped<SnapCdDbContext>(sp => sp.GetRequiredService<SelfHostedSnapCdDbContext>());
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddScoped<IIdempotentSqlManager, IdempotentSqlManager>();
        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SelfHostedSnapCdDbContext>();
        await dbContext.Database.MigrateAsync();

        // Deployed databases run with read-committed snapshot on; the graph triggers rely on it.
        await dbContext.Database.ExecuteSqlRawAsync(
            $"ALTER DATABASE [{DatabaseName}] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;");
        await scope.ServiceProvider.GetRequiredService<IIdempotentSqlManager>().ApplyIdempotentSqlAsync();

        await SeedAsync(dbContext);
    }

    /// <summary>The container reports ready before SQL Server accepts logins, so retry briefly.</summary>
    private static async Task CreateDatabaseAsync(string masterConnectionString)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var master = new SqlConnection(masterConnectionString);
                await master.OpenAsync();
                await using var create = new SqlCommand($"CREATE DATABASE [{DatabaseName}];", master);
                await create.ExecuteNonQueryAsync();
                return;
            }
            catch (SqlException) when (attempt < 30)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }

    private async Task SeedAsync(SnapCdDbContext dbContext)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "ConcurrencyOrg" };
        dbContext.Add(org);
        await dbContext.SaveChangesAsync();
        OrganizationId = org.Id;

        var servicePrincipal = new ServicePrincipal { Id = Guid.NewGuid(), OrganizationId = org.Id };
        dbContext.Add(servicePrincipal);
        await dbContext.SaveChangesAsync();

        var runner = new Runner
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Name = "concurrency-runner",
            ServicePrincipalId = servicePrincipal.Id
        };
        dbContext.Add(runner);

        var stack = new Stack { Id = Guid.NewGuid(), OrganizationId = org.Id, Name = "concurrency-tests" };
        dbContext.Add(stack);
        await dbContext.SaveChangesAsync();

        var ns = new Namespace { Id = Guid.NewGuid(), OrganizationId = org.Id, StackId = stack.Id, Name = "dependency-edges" };
        dbContext.Add(ns);
        await dbContext.SaveChangesAsync();

        var consumer = NewModule(org.Id, ns.Id, runner.Id, "consumer");
        var producer = NewModule(org.Id, ns.Id, runner.Id, "producer");
        dbContext.Add(consumer);
        dbContext.Add(producer);
        await dbContext.SaveChangesAsync();

        ConsumerModuleId = consumer.Id;
        ProducerModuleId = producer.Id;
    }

    private static Module NewModule(Guid orgId, Guid namespaceId, Guid runnerId, string name)
    {
        var module = new Module
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            NamespaceId = namespaceId,
            RunnerId = runnerId,
            Name = name,
            SourceUrl = "https://example.invalid/repo.git",
            SourceRevision = "main",
            SourceSubdirectory = ".",
            CreatedDateTime = DateTime.UtcNow
        };
        module.ModuleSaga = new ModuleSaga
        {
            CorrelationId = module.Id,
            OrganizationId = orgId,
            RowVersion = [],
            CurrentState = nameof(ModuleStateMachine.Gatekeeping),
            DesiredStateHeadline = DesiredStateHeadline.Applied
        };
        module.ModuleModifiedSaga = new ModuleModifiedSaga
        {
            CorrelationId = module.Id,
            OrganizationId = orgId,
            RowVersion = [],
            CurrentState = nameof(ModuleModifiedStateMachine.Idle)
        };
        return module;
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
            await _serviceProvider.DisposeAsync();

        if (_databaseContainer is not null)
            await _databaseContainer.DisposeAsync();
    }
}
