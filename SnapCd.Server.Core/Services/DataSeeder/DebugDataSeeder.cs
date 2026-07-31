// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Missions;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.DataSeeder;
using SnapCd.Server.Core.Settings.DataSeeder.ToSeed;
using SnapCd.Server.Core.Entities.Definition.IntegrationEvents;
using SnapCd.Server.Core.Services.Integrations;
using SnapCd.Server.Core.StateMachine.Gatekeeping;

namespace SnapCd.Server.Core.Services.DataSeeder;

public class DebugDataSeeder : ProductionDataSeeder
{
    // All "debug" entities — stack/namespace/module/runner share this id; secrets/SPs use suffix variants.
    private static readonly Guid DebugEntityId = new("99999999-9999-9999-9999-999999999999");

    // A second module in the debug namespace, backed by the real snapcd-samples/mock-module-vpc repo
    // (branch autofixtest), used by the Mission Test Bench AutoFix test so the mission fixes a real repo.
    private static readonly Guid MockModuleVpcId = new("99999999-9999-9999-9999-999999999910");
    private static readonly Guid MonorepoTestingModuleId = new("99999999-9999-9999-9999-999999999911");

    private static readonly Guid DebugUserId = new("99999999-9999-9999-9999-999999999990");

    private static readonly Guid DebugSpId = new("99999999-9999-9999-9999-999999999991");
    private static readonly Guid DebugSpTerraformerId = new("99999999-9999-9999-9999-999999999992");
    private static readonly Guid DebugSpRunnerId = new("99999999-9999-9999-9999-999999999993");
    private static readonly Guid DebugSpTestTarget1Id = new("99999999-9999-9999-9999-999999999994");
    private static readonly Guid DebugSpTestTarget2Id = new("99999999-9999-9999-9999-999999999995");

    private static readonly Guid DebugStackSecretId = new("99999999-9999-9999-9999-999999999901");
    private static readonly Guid DebugNamespaceSecretId = new("99999999-9999-9999-9999-999999999902");
    private static readonly Guid DebugModuleSecretId = new("99999999-9999-9999-9999-999999999903");

    private static readonly Guid DebugIntegrationId = new("99999999-9999-9999-9999-999999999920");
    private static readonly Guid DebugIntegrationEventJobFailedId = new("99999999-9999-9999-9999-999999999921");
    private static readonly Guid DebugIntegrationEventJobSucceededId = new("99999999-9999-9999-9999-999999999922");
    private static readonly Guid DebugIntegrationEventJobAwaitingApprovalId = new("99999999-9999-9999-9999-999999999923");
    private static readonly Guid DebugIntegrationEventJobCancelledId = new("99999999-9999-9999-9999-999999999924");
    private static readonly Guid DebugIntegrationEventMilestoneId = new("99999999-9999-9999-9999-999999999925");

    private readonly DebugDataSeederSettings _debugSettings;
    private readonly Guid _preseededOrganizationId;
    private readonly Guid _preseededAgentId;
    private readonly IServiceProvider _serviceProvider;

    public DebugDataSeeder(
        IServiceProvider serviceProvider,
        SnapCdDbContext dbContext,
        IOptions<ProductionDataSeederSettings> productionOptions,
        IOptions<ServerSettings> serverOptions,
        IOptions<DebugDataSeederSettings> debugOptions)
        : base(serviceProvider, dbContext, productionOptions, serverOptions)
    {
        _debugSettings = debugOptions.Value;
        _preseededOrganizationId = productionOptions.Value.Preseeded.Organization.Id ?? PreseededSettings.DefaultId;
        _preseededAgentId = productionOptions.Value.Preseeded.Agent.Id ?? PreseededSettings.DefaultAgentId;
        _serviceProvider = serviceProvider;
    }

    public override async Task SeedAsync()
    {
        await base.SeedAsync();

        await using var asyncServiceScope = _serviceProvider.CreateAsyncScope();

        await SeedDebugUser(asyncServiceScope);
        await SeedDebugServicePrincipals();
        await SeedDebugStackNamespaceModuleRunner(asyncServiceScope);
        await SeedDebugSecrets(asyncServiceScope);
        await SeedDebugIntegration(asyncServiceScope);
        await SeedConfiguredUsers(asyncServiceScope);
    }

    private async Task SeedDebugUser(AsyncServiceScope asyncServiceScope)
    {
        var debugUser = new UserToPreseed
        {
            Id = DebugUserId,
            Name = "debug@preseeded.io",
            Password = "Debug#123",
            OrganizationId = _preseededOrganizationId
        };
        await CreatePreseededUser(asyncServiceScope, debugUser);

        var userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbUser = await userManager.FindByNameAsync(debugUser.Name);
        if (dbUser == null) return;

        await SyncOrganizationRoles(dbUser.Id, _preseededOrganizationId, [OrganizationRole.Owner]);
        await GrantSystemAdminRole(dbUser.Id);
    }

    private async Task SeedDebugServicePrincipals()
    {
        var sps = new (Guid Id, string ClientId)[]
        {
            (DebugSpId, "debug"),
            (DebugSpTerraformerId, "debugTerraformer"),
            (DebugSpRunnerId, "debugRunner"),
            (DebugSpTestTarget1Id, "debugTestTarget1"),
            (DebugSpTestTarget2Id, "debugTestTarget2"),
        };

        foreach (var (id, clientId) in sps)
        {
            await CreateOrUpdateApplication(new ServicePrincipalToSeed
            {
                Id = id,
                ClientId = clientId,
                ClientSecret = clientId,
                ClientType = "confidential",
                DisplayName = clientId,
                Scopes = ["snapcd_scope"],
                ConsentType = null,
                LoginRedirectUri = null,
                LogoutRedirectUri = null,
                OrganizationId = _preseededOrganizationId,
                IsServicePrincipal = true
            });
            await SyncServicePrincipalOrganizationRoles(id, _preseededOrganizationId, [OrganizationRole.Owner]);
        }
    }

    private async Task SeedDebugStackNamespaceModuleRunner(AsyncServiceScope asyncServiceScope)
    {
        var dbContext = asyncServiceScope.ServiceProvider.GetRequiredService<SnapCdDbContext>();

        var existingRunner = await dbContext.Runners.FirstOrDefaultAsync(r => r.Id == DebugEntityId);
        if (existingRunner == null)
        {
            dbContext.Runners.Add(new Runner
            {
                Id = DebugEntityId,
                ServicePrincipalId = DebugSpRunnerId,
                Name = "debug",
                AllowMultipleInstances = true,
                IsSuppliedToAllModules = true,
                OrganizationId = _preseededOrganizationId
            });
        }
        else
        {
            existingRunner.Name = "debug";
            existingRunner.ServicePrincipalId = DebugSpRunnerId;
            existingRunner.AllowMultipleInstances = true;
            existingRunner.IsSuppliedToAllModules = true;
            existingRunner.OrganizationId = _preseededOrganizationId;
        }

        var existingStack = await dbContext.Stacks.FirstOrDefaultAsync(s => s.Id == DebugEntityId);
        if (existingStack == null)
        {
            dbContext.Stacks.Add(new Stack
            {
                Id = DebugEntityId,
                OrganizationId = _preseededOrganizationId,
                Name = "debug",
                CreatedDateTime = DateTime.UtcNow
            });
        }
        else
        {
            existingStack.Name = "debug";
            existingStack.OrganizationId = _preseededOrganizationId;
        }

        var existingNamespace = await dbContext.Namespaces.FirstOrDefaultAsync(n => n.Id == DebugEntityId);
        if (existingNamespace == null)
        {
            dbContext.Namespaces.Add(new Namespace
            {
                Id = DebugEntityId,
                OrganizationId = _preseededOrganizationId,
                StackId = DebugEntityId,
                Name = "debug",
                CreatedDateTime = DateTime.UtcNow
            });
        }
        else
        {
            existingNamespace.Name = "debug";
            existingNamespace.StackId = DebugEntityId;
            existingNamespace.OrganizationId = _preseededOrganizationId;
        }

        var existingModule = await dbContext.Modules.FirstOrDefaultAsync(m => m.Id == DebugEntityId);
        if (existingModule == null)
        {
            var debugModule = new Module
            {
                Id = DebugEntityId,
                OrganizationId = _preseededOrganizationId,
                NamespaceId = DebugEntityId,
                RunnerId = DebugEntityId,
                Name = "debug",
                SourceUrl = "https://github.com/schrieksoft/snapcd-samples.git",
                SourceRevision = "main",
                SourceSubdirectory = "dev/simple",
                SourceType = SourceType.Git,
                SourceRevisionType = SourceRevisionType.Default,
                CreatedDateTime = DateTime.UtcNow
            };
            debugModule.ModuleSaga = new ModuleSaga
            {
                CorrelationId = debugModule.Id,
                OrganizationId = _preseededOrganizationId,
                RowVersion = [],
                CurrentState = nameof(ModuleStateMachine.Gatekeeping),
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                QueuedDesiredStateHeadline = null
            };
            debugModule.ModuleModifiedSaga = new ModuleModifiedSaga
            {
                CorrelationId = debugModule.Id,
                OrganizationId = _preseededOrganizationId,
                RowVersion = [],
                CurrentState = nameof(ModuleModifiedStateMachine.Idle),
                LastUpdated = null,
                TimeoutTokenId = null
            };
            dbContext.Modules.Add(debugModule);
        }
        else
        {
            existingModule.Name = "debug";
            existingModule.NamespaceId = DebugEntityId;
            existingModule.RunnerId = DebugEntityId;
            existingModule.OrganizationId = _preseededOrganizationId;
        }

        // A second module backed by the real mock-module-vpc repo (branch autofixtest) so the AutoFix
        // harness test gives the mission an actual repo to clone, fix, and open a PR against.
        const string mockSourceUrl = "https://github.com/snapcd-samples/mock-module-vpc.git";
        const string mockSourceRevision = "autofixtest";
        var existingMockModule = await dbContext.Modules.FirstOrDefaultAsync(m => m.Id == MockModuleVpcId);
        if (existingMockModule == null)
        {
            var mockModule = new Module
            {
                Id = MockModuleVpcId,
                OrganizationId = _preseededOrganizationId,
                NamespaceId = DebugEntityId,
                RunnerId = DebugEntityId,
                Name = "mock-module-vpc",
                SourceUrl = mockSourceUrl,
                SourceRevision = mockSourceRevision,
                SourceSubdirectory = "",
                SourceType = SourceType.Git,
                SourceRevisionType = SourceRevisionType.Default,
                CreatedDateTime = DateTime.UtcNow
            };
            mockModule.ModuleSaga = new ModuleSaga
            {
                CorrelationId = mockModule.Id,
                OrganizationId = _preseededOrganizationId,
                RowVersion = [],
                CurrentState = nameof(ModuleStateMachine.Gatekeeping),
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                QueuedDesiredStateHeadline = null
            };
            mockModule.ModuleModifiedSaga = new ModuleModifiedSaga
            {
                CorrelationId = mockModule.Id,
                OrganizationId = _preseededOrganizationId,
                RowVersion = [],
                CurrentState = nameof(ModuleModifiedStateMachine.Idle),
                LastUpdated = null,
                TimeoutTokenId = null
            };
            dbContext.Modules.Add(mockModule);
        }
        else
        {
            existingMockModule.Name = "mock-module-vpc";
            existingMockModule.NamespaceId = DebugEntityId;
            existingMockModule.RunnerId = DebugEntityId;
            existingMockModule.OrganizationId = _preseededOrganizationId;
            existingMockModule.SourceUrl = mockSourceUrl;
            existingMockModule.SourceRevision = mockSourceRevision;
            existingMockModule.SourceSubdirectory = "";
        }

        // A third module on the public monorepo-testing fixture repo with the trigger path filter enabled, so
        // the full path-scoped refresh loop (watched-path union -> runner hashing + discovery -> closure
        // decision) is exercisable in any dev environment. Watches modules/app-a, which references
        // shared/network -> shared/naming; commits to the fixture's docs/ must not trigger it.
        const string monorepoTestingUrl = "https://github.com/schrieksoft/monorepo-testing.git";
        var existingMonorepoModule = await dbContext.Modules.FirstOrDefaultAsync(m => m.Id == MonorepoTestingModuleId);
        if (existingMonorepoModule == null)
        {
            var monorepoModule = new Module
            {
                Id = MonorepoTestingModuleId,
                OrganizationId = _preseededOrganizationId,
                NamespaceId = DebugEntityId,
                RunnerId = DebugEntityId,
                Name = "monorepo-testing-app-a",
                SourceUrl = monorepoTestingUrl,
                SourceRevision = "main",
                SourceSubdirectory = "modules/app-a",
                SourceType = SourceType.Git,
                SourceRevisionType = SourceRevisionType.Default,
                TriggerOnSourceChanged = true,
                TriggerPathFilterEnabled = true,
                CreatedDateTime = DateTime.UtcNow
            };
            monorepoModule.ModuleSaga = new ModuleSaga
            {
                CorrelationId = monorepoModule.Id,
                OrganizationId = _preseededOrganizationId,
                RowVersion = [],
                CurrentState = nameof(ModuleStateMachine.Gatekeeping),
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                QueuedDesiredStateHeadline = null
            };
            monorepoModule.ModuleModifiedSaga = new ModuleModifiedSaga
            {
                CorrelationId = monorepoModule.Id,
                OrganizationId = _preseededOrganizationId,
                RowVersion = [],
                CurrentState = nameof(ModuleModifiedStateMachine.Idle),
                LastUpdated = null,
                TimeoutTokenId = null
            };
            dbContext.Modules.Add(monorepoModule);
        }
        else
        {
            existingMonorepoModule.Name = "monorepo-testing-app-a";
            existingMonorepoModule.NamespaceId = DebugEntityId;
            existingMonorepoModule.RunnerId = DebugEntityId;
            existingMonorepoModule.OrganizationId = _preseededOrganizationId;
            existingMonorepoModule.SourceUrl = monorepoTestingUrl;
            existingMonorepoModule.SourceRevision = "main";
            existingMonorepoModule.SourceSubdirectory = "modules/app-a";
            existingMonorepoModule.TriggerPathFilterEnabled = true;
        }

        // AutoFix is scoped to the mock module (not org-wide) so the existing AutoDiagnose harness tests
        // on the debug module keep dispatching AutoDiagnose; a failed job on mock-module-vpc dispatches
        // AutoFix (it takes precedence). Targets the preseeded default Agent (assigned to all modules).
        var existingAutoFix = await dbContext.ModuleMissions.FirstOrDefaultAsync(m =>
            m.ModuleId == MockModuleVpcId && m.AgentId == _preseededAgentId
            && m.OrganizationId == _preseededOrganizationId && m.MissionType == MissionType.AutoFix);
        if (existingAutoFix == null)
        {
            dbContext.ModuleMissions.Add(new ModuleMission
            {
                Id = Guid.NewGuid(),
                OrganizationId = _preseededOrganizationId,
                AgentId = _preseededAgentId,
                ModuleId = MockModuleVpcId,
                MissionType = MissionType.AutoFix,
                IsDisabled = false
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedDebugSecrets(AsyncServiceScope asyncServiceScope)
    {
        var dbContext = asyncServiceScope.ServiceProvider.GetRequiredService<SnapCdDbContext>();

        await CreatePreseededStackSecret(asyncServiceScope, DebugStackSecretId, "debug", "debug-secret-value", DebugEntityId, _preseededOrganizationId);

        var existingNamespaceSecret = await dbContext.Secrets
            .OfType<NamespaceSecret>()
            .FirstOrDefaultAsync(s => s.Id == DebugNamespaceSecretId);
        if (existingNamespaceSecret == null)
        {
            dbContext.Secrets.Add(new NamespaceSecret
            {
                Id = DebugNamespaceSecretId,
                OrganizationId = _preseededOrganizationId,
                NamespaceId = DebugEntityId,
                Name = "debug"
            });
        }
        else
        {
            existingNamespaceSecret.Name = "debug";
            existingNamespaceSecret.NamespaceId = DebugEntityId;
            existingNamespaceSecret.OrganizationId = _preseededOrganizationId;
        }

        var existingModuleSecret = await dbContext.Secrets
            .OfType<ModuleSecret>()
            .FirstOrDefaultAsync(s => s.Id == DebugModuleSecretId);
        if (existingModuleSecret == null)
        {
            dbContext.Secrets.Add(new ModuleSecret
            {
                Id = DebugModuleSecretId,
                OrganizationId = _preseededOrganizationId,
                ModuleId = DebugEntityId,
                Name = "debug"
            });
        }
        else
        {
            existingModuleSecret.Name = "debug";
            existingModuleSecret.ModuleId = DebugEntityId;
            existingModuleSecret.OrganizationId = _preseededOrganizationId;
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedDebugIntegration(AsyncServiceScope asyncServiceScope)
    {
        var dbContext = asyncServiceScope.ServiceProvider.GetRequiredService<SnapCdDbContext>();

        var existing = await dbContext.Integrations.FirstOrDefaultAsync(i => i.Id == DebugIntegrationId);
        if (existing == null)
        {
            dbContext.Integrations.Add(new Integration
            {
                Id = DebugIntegrationId,
                OrganizationId = _preseededOrganizationId,
                Name = "debug-slack",
                IntegrationType = IntegrationType.Slack,
                Enabled = true,
                IsSuppliedToAllModules = true,
                CreatedDateTime = DateTime.UtcNow
            });
        }
        else
        {
            existing.Name = "debug-slack";
            existing.IntegrationType = IntegrationType.Slack;
            existing.Enabled = true;
            existing.IsSuppliedToAllModules = true;
            existing.OrganizationId = _preseededOrganizationId;
        }

        var events = new (Guid Id, IntegrationTrigger Trigger)[]
        {
            (DebugIntegrationEventJobFailedId, IntegrationTrigger.JobFailed),
            (DebugIntegrationEventJobSucceededId, IntegrationTrigger.JobSucceeded),
            (DebugIntegrationEventJobAwaitingApprovalId, IntegrationTrigger.JobAwaitingApproval),
            (DebugIntegrationEventJobCancelledId, IntegrationTrigger.JobCancelled),
            (DebugIntegrationEventMilestoneId, IntegrationTrigger.MissionMilestoneReported),
        };

        foreach (var (id, trigger) in events)
        {
            var existingEvent = await dbContext.OrganizationIntegrationEvents
                .FirstOrDefaultAsync(e => e.Id == id);
            if (existingEvent == null)
            {
                dbContext.OrganizationIntegrationEvents.Add(new OrganizationIntegrationEvent
                {
                    Id = id,
                    OrganizationId = _preseededOrganizationId,
                    IntegrationId = DebugIntegrationId,
                    Trigger = trigger,
                    IsDisabled = false,
                    CreatedDateTime = DateTime.UtcNow
                });
            }
            else
            {
                existingEvent.OrganizationId = _preseededOrganizationId;
                existingEvent.IntegrationId = DebugIntegrationId;
                existingEvent.Trigger = trigger;
                existingEvent.IsDisabled = false;
            }
        }

        await dbContext.SaveChangesAsync();

        var slack = _debugSettings.SlackIntegration;
        var botToken = slack?.BotToken;
        var defaultChannel = slack?.DefaultChannel;
        if (!string.IsNullOrWhiteSpace(botToken) && !string.IsNullOrWhiteSpace(defaultChannel))
        {
            var secretStore = asyncServiceScope.ServiceProvider.GetRequiredService<IntegrationSecretStore>();
            var connectionJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                BotToken = botToken,
                DefaultChannel = defaultChannel
            });
            await secretStore.WriteAsync(_preseededOrganizationId, DebugIntegrationId, connectionJson);
        }
    }

    private async Task SeedConfiguredUsers(AsyncServiceScope asyncServiceScope)
    {
        if (_debugSettings.Users.Count == 0) return;

        var userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<User>>();

        foreach (var user in _debugSettings.Users)
        {
            await CreatePreseededUser(asyncServiceScope, new UserToPreseed
            {
                Id = user.Id,
                Name = user.Email,
                Password = user.Password,
                OrganizationId = _preseededOrganizationId
            });

            var dbUser = await userManager.FindByNameAsync(user.Email);
            if (dbUser == null) continue;

            await SyncOrganizationRoles(dbUser.Id, _preseededOrganizationId, [OrganizationRole.Owner]);
            if (user.IsSystemAdministrator)
                await GrantSystemAdminRole(dbUser.Id);
        }
    }
}
