// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Missions;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Misc.Utils;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.DataSeeder;
using SnapCd.Server.Core.Settings.DataSeeder.ToSeed;

namespace SnapCd.Server.Core.Services.DataSeeder;

public class ProductionDataSeeder : IDataSeeder

{
    private readonly SnapCdDbContext _dbContext;
    private readonly ProductionDataSeederSettings _settings;
    private readonly ServerSettings _serverSettings;
    private readonly IServiceProvider _serviceProvider;

    public ProductionDataSeeder(IServiceProvider serviceProvider, SnapCdDbContext dbContext,
        IOptions<ProductionDataSeederSettings> options, IOptions<ServerSettings> serverOptions)
    {
        _dbContext = dbContext;
        _settings = options.Value;
        _serverSettings = serverOptions.Value;
        _serviceProvider = serviceProvider;
    }


    public virtual async Task SeedAsync()
    {
        await using var asyncServiceScope = _serviceProvider.CreateAsyncScope();

        var platformOrganizationId = _settings.Preseeded.Organization.Id ?? PreseededSettings.DefaultId;
        await CreatePreseededOrganization(_settings.Preseeded.Organization.Name, platformOrganizationId);

        await CreateScopesAsync(asyncServiceScope, new Dictionary<string, List<string>>
        {
            { "snapcd_scope", new List<string> { "snapcd" } }
        });

        await CreateOrUpdateApplication(new ServicePrincipalToSeed
            {
                ClientId = "SwaggerClient",
                ClientType = "public",
                DisplayName = "Swagger Client",
                Scopes = ["snapcd_scope"],
                ConsentType = "implicit",
                LoginRedirectUri = $"{_serverSettings.Host}/swagger/oauth2-redirect.html",
                LogoutRedirectUri = $"{_serverSettings.Host}/swagger/index.html",
                OrganizationId = platformOrganizationId,
                IsServicePrincipal = false
            }
        );

        // Seed preseeded entities if enabled
        if (_settings.Preseeded.Enabled)
        {
            await SeedPreseededEntities(asyncServiceScope);
        }
    }

    protected async Task CreatePreseededUser(AsyncServiceScope asyncServiceScope, UserToPreseed userToCreate)
    {
        var userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Check if the user already exists
        var user = await userManager.FindByNameAsync(userToCreate.Name);

        if (user == null)
        {
            // Create the user
            user = new User
            {
                Id = userToCreate.Id ?? Guid.NewGuid(),
                UserName = userToCreate.Name,
                Email = userToCreate.Name,
                IsDisabled = false,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true
            };

            var createResult = await userManager.CreateAsync(user, userToCreate.Password);
            if (!createResult.Succeeded)
                throw new Exception(
                    $"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

            // Add claims
            var claims = new List<Claim>
            {
            };

            var claimResult = await userManager.AddClaimsAsync(user, claims);
            if (!claimResult.Succeeded)
                throw new Exception(
                    $"Failed to add claims: {string.Join(", ", claimResult.Errors.Select(e => e.Description))}");
        }
        else
        {
            // Update the password
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, resetToken, userToCreate.Password);

            if (!resetResult.Succeeded)
                throw new Exception(
                    $"Failed to reset password: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");

            user.Email = userToCreate.Name;
            user.IsDisabled = false;
            user.EmailConfirmed = true;
            user.PhoneNumberConfirmed = false;
            user.TwoFactorEnabled = false;
            user.LockoutEnabled = true;

            await userManager.UpdateAsync(user);
        }

        // Add user to the default organization
        var existingOrgUser = await _dbContext.OrganizationUsers
            .FirstOrDefaultAsync(ou => ou.UserId == user.Id && ou.OrganizationId == userToCreate.OrganizationId);

        if (existingOrgUser == null)
        {
            var orgUser = new OrganizationUser
            {
                Id = Guid.NewGuid(),
                OrganizationId = userToCreate.OrganizationId,
                UserId = user.Id,
                JoinedAt = DateTime.UtcNow,
                IsDeactivated = false,
                InvitationCompleted = true,
                InvitationCompletedDateTime = DateTime.UtcNow
            };

            _dbContext.OrganizationUsers.Add(orgUser);
            await _dbContext.SaveChangesAsync();
        }
    }


    private async Task CreateScopesAsync(AsyncServiceScope asyncServiceScope,
        Dictionary<string, List<string>>? scopes = null)
    {
        var manager = asyncServiceScope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        if (scopes == null) return;

        foreach (var (scope, resources) in scopes)
            if (await manager.FindByNameAsync(scope) is null)
            {
                var descriptor = new OpenIddictScopeDescriptor
                {
                    Name = scope
                };
                foreach (var resource in resources) descriptor.Resources.Add(resource);
                await manager.CreateAsync(descriptor);
            }
    }

    protected async Task CreateOrUpdateApplication(ServicePrincipalToSeed spToSeed)
    {
        // Only prefix ClientId for actual service principals (client_credentials flow)
        // System applications like SwaggerClient don't need organization scoping
        var storedClientId = spToSeed.IsServicePrincipal
            ? $"{spToSeed.OrganizationId}:{spToSeed.ClientId}"
            : spToSeed.ClientId;

        // Check if ServicePrincipal already exists in the *same organization* — search by both
        // new and old ClientId formats to handle migration from old format to new prefixed format.
        // Restricting by OrganizationId is critical: ServicePrincipal has an alternate key on
        // (Id, OrganizationId), so finding a row from a different org and reassigning OrganizationId
        // throws "OrganizationId is part of a key and so cannot be modified".
        var existingServicePrincipal = await _dbContext.ServicePrincipals
            .FirstOrDefaultAsync(sp =>
                sp.OrganizationId == spToSeed.OrganizationId &&
                (sp.ClientId == storedClientId || sp.ClientId == spToSeed.ClientId));


        string? permissions = null;
        string? requirements = null;

        var loginRedirects = spToSeed.LoginRedirectUri == null ? null : $"[\"{string.Join("\",\"", [spToSeed.LoginRedirectUri])}\"]";
        var logoutRedirects = spToSeed.LogoutRedirectUri == null ? null : $"[\"{string.Join("\",\"", [spToSeed.LogoutRedirectUri])}\"]";

        if (!string.IsNullOrEmpty(spToSeed.ClientSecret) && spToSeed.ClientSecret != null)
            spToSeed.ClientSecret = SecretHashingHelper.ObfuscateClientSecret(spToSeed.ClientSecret);

        if (spToSeed.IsServicePrincipal)
        {
            var permissionsList = new List<string>
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
            };
            foreach (var scope in spToSeed.Scopes)
                permissionsList.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
            permissions = $"[\"{string.Join("\",\"", permissionsList)}\"]";
        }
        else
        {
            var permissionsList = new List<string>
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles
            };
            foreach (var scope in spToSeed.Scopes)
                permissionsList.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
            permissions = $"[\"{string.Join("\",\"", permissionsList)}\"]";
            var requirementsList = new List<string> { OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange };
            requirements = $"[\"{string.Join("\",\"", requirementsList)}\"]";
        }

        if (existingServicePrincipal == null)
        {
            // Create new ServicePrincipal directly with OrganizationId
            var servicePrincipal = new ServicePrincipal
            {
                Id = spToSeed.Id ?? Guid.NewGuid(),
                ClientId = storedClientId,
                ClientSecret = spToSeed.ClientSecret,
                ConsentType = spToSeed.ConsentType,
                DisplayName = spToSeed.DisplayName,
                ClientType = spToSeed.ClientType,
                OrganizationId = spToSeed.OrganizationId,
                ConcurrencyToken = Guid.NewGuid().ToString(),
                IsDisabled = false
            };

            // Set OpenIddict-specific properties


            // Set permissions

            servicePrincipal.Permissions = permissions;
            servicePrincipal.Requirements = requirements;
            servicePrincipal.RedirectUris = loginRedirects;
            servicePrincipal.PostLogoutRedirectUris = logoutRedirects;

            _dbContext.ServicePrincipals.Add(servicePrincipal);

            await _dbContext.SaveChangesAsync();
        }
        else
        {
            // Update existing ServicePrincipal
            // Migrate ClientId to prefixed format if needed.
            // Do NOT reassign OrganizationId: it's part of an alternate key (see ServicePrincipalClassMap),
            // so EF refuses to modify it. The lookup above is already constrained to the target org.
            existingServicePrincipal.ClientId = storedClientId;
            existingServicePrincipal.ClientSecret = spToSeed.ClientSecret;
            existingServicePrincipal.ConsentType = spToSeed.ConsentType;
            existingServicePrincipal.DisplayName = spToSeed.DisplayName;
            existingServicePrincipal.ClientType = spToSeed.ClientType;

            // Update OpenIddict-specific properties
            existingServicePrincipal.RedirectUris = $"[\"{spToSeed.LoginRedirectUri}\"]";
            existingServicePrincipal.PostLogoutRedirectUris = $"[\"{spToSeed.LogoutRedirectUri}\"]";


            existingServicePrincipal.Permissions = permissions;
            existingServicePrincipal.Requirements = requirements;
            existingServicePrincipal.RedirectUris = loginRedirects;
            existingServicePrincipal.PostLogoutRedirectUris = logoutRedirects;

            _dbContext.ServicePrincipals.Update(existingServicePrincipal);
            await _dbContext.SaveChangesAsync();
        }
    }

    protected virtual async Task SeedPreseededEntities(AsyncServiceScope asyncServiceScope)
    {
        var preseeded = _settings.Preseeded;
        // Organization is already created unconditionally in SeedAsync.
        var organizationId = preseeded.Organization.Id ?? PreseededSettings.DefaultId;

        await SeedPreseededUserAsync(asyncServiceScope, preseeded, organizationId);
        await SeedPreseededRunnerAsync(asyncServiceScope, preseeded, organizationId);
        await SeedPreseededAgentAsync(asyncServiceScope, preseeded, organizationId);
    }

    protected virtual async Task SeedPreseededUserAsync(AsyncServiceScope asyncServiceScope, PreseededSettings preseeded, Guid organizationId)
    {
        var userToSeed = new UserToPreseed
        {
            Id = preseeded.User.Id,
            Name = preseeded.User.Email,
            Password = preseeded.User.Password,
            OrganizationId = organizationId
        };
        await CreatePreseededUser(asyncServiceScope, userToSeed);

        var userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByNameAsync(preseeded.User.Email);
        if (user is null)
            return;

        await SyncOrganizationRoles(user.Id, organizationId, [OrganizationRole.Owner]);
        if (preseeded.User.IsSystemAdministrator)
            await GrantSystemAdminRole(user.Id);
    }

    protected virtual async Task SeedPreseededRunnerAsync(AsyncServiceScope asyncServiceScope, PreseededSettings preseeded, Guid organizationId)
    {
        var spId = preseeded.Runner.ServicePrincipalId ?? Guid.NewGuid();
        await CreateOrUpdateApplication(new ServicePrincipalToSeed
        {
            Id = spId,
            ClientId = preseeded.Runner.ServicePrincipalClientId,
            ClientSecret = preseeded.Runner.ServicePrincipalClientSecret,
            ClientType = "confidential",
            DisplayName = preseeded.Runner.ServicePrincipalClientId,
            Scopes = ["snapcd_scope"],
            ConsentType = null,
            LoginRedirectUri = null,
            LogoutRedirectUri = null,
            OrganizationId = organizationId,
            IsServicePrincipal = true
        });
        await SyncServicePrincipalOrganizationRoles(spId, organizationId, [OrganizationRole.Owner]);

        var runnerId = preseeded.Runner.Id ?? Guid.NewGuid();
        await CreatePreseededRunner(runnerId, preseeded.Runner.Name, organizationId, spId);

        var stackId = preseeded.Stack.Id ?? Guid.NewGuid();
        await CreatePreseededStack(stackId, preseeded.Stack.Name, organizationId);
        await AssignRunnerToStack(runnerId, stackId, organizationId);
        var secretId = preseeded.Stack.SampleSecretId ?? Guid.NewGuid();
        await CreatePreseededStackSecret(
            asyncServiceScope, secretId, preseeded.Stack.SampleSecretName, preseeded.Stack.SampleSecretValue, stackId, organizationId);
    }

    protected virtual async Task SeedPreseededAgentAsync(AsyncServiceScope asyncServiceScope, PreseededSettings preseeded, Guid organizationId)
    {
        // Agent's Service Principal is distinct from the Runner's (they share the ServicePrincipals
        // primary key space, so they can't reuse the same id). The SnapCd.Agent orchestrator connects
        // as this Agent.
        var agentSpId = preseeded.Agent.ServicePrincipalId ?? Guid.NewGuid();
        await CreateOrUpdateApplication(new ServicePrincipalToSeed
        {
            Id = agentSpId,
            ClientId = preseeded.Agent.ServicePrincipalClientId,
            ClientSecret = preseeded.Agent.ServicePrincipalClientSecret,
            ClientType = "confidential",
            DisplayName = preseeded.Agent.ServicePrincipalClientId,
            Scopes = ["snapcd_scope"],
            ConsentType = null,
            LoginRedirectUri = null,
            LogoutRedirectUri = null,
            OrganizationId = organizationId,
            IsServicePrincipal = true
        });
        await SyncServicePrincipalOrganizationRoles(agentSpId, organizationId, [OrganizationRole.Owner]);

        var agentId = preseeded.Agent.Id ?? Guid.NewGuid();
        await CreatePreseededAgent(agentId, preseeded.Agent.Name, organizationId, agentSpId);
        await CreatePreseededOrganizationMissions(agentId, organizationId, preseeded.Agent.Missions);
    }

    protected async Task CreatePreseededAgent(Guid agentId, string name, Guid organizationId, Guid servicePrincipalId)
    {
        var existing = await _dbContext.Agents.FirstOrDefaultAsync(a => a.Id == agentId);
        if (existing != null)
        {
            existing.Name = name;
            existing.ServicePrincipalId = servicePrincipalId;
            // Single-instance default so the orchestrator can connect without supplying an instance name.
            existing.AllowMultipleInstances = false;
            existing.IsSuppliedToAllModules = true;
            existing.IsDisabled = false;
        }
        else
        {
            _dbContext.Agents.Add(new Agent
            {
                Id = agentId,
                OrganizationId = organizationId,
                ServicePrincipalId = servicePrincipalId,
                Name = name,
                AllowMultipleInstances = false,
                IsSuppliedToAllModules = true,
                IsDisabled = false
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    protected async Task CreatePreseededOrganizationMissions(Guid agentId, Guid organizationId, List<string> missionTypeNames)
    {
        foreach (var missionTypeName in missionTypeNames)
        {
            if (!Enum.TryParse<MissionType>(missionTypeName, ignoreCase: true, out var missionType))
                continue;

            var existing = await _dbContext.OrganizationMissions.FirstOrDefaultAsync(m =>
                m.AgentId == agentId && m.OrganizationId == organizationId && m.MissionType == missionType);
            if (existing != null)
                continue;

            _dbContext.OrganizationMissions.Add(new OrganizationMission
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AgentId = agentId,
                MissionType = missionType,
                IsDisabled = false
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    protected async Task CreatePreseededRunner(Guid runnerId, string name, Guid organizationId, Guid servicePrincipalId)
    {
        var existingRunner = await _dbContext.Runners
            .FirstOrDefaultAsync(r => r.Id == runnerId);

        if (existingRunner != null)
        {
            // Update existing runner
            existingRunner.Name = name;
            existingRunner.ServicePrincipalId = servicePrincipalId;
            existingRunner.IsSuppliedToAllModules = false;
            existingRunner.AllowMultipleInstances = true;
            existingRunner.IsDisabled = false;
        }
        else
        {
            // Create new runner — explicitly scoped via RunnerStackSupply, not all-modules.
            var runner = new Runner
            {
                Id = runnerId,
                OrganizationId = organizationId,
                ServicePrincipalId = servicePrincipalId,
                Name = name,
                IsSuppliedToAllModules = false,
                AllowMultipleInstances = true,
                IsDisabled = false
            };
            _dbContext.Runners.Add(runner);
        }

        await _dbContext.SaveChangesAsync();
    }

    protected async Task CreatePreseededStack(Guid stackId, string name, Guid organizationId)
    {
        var existing = await _dbContext.Stacks.FirstOrDefaultAsync(s => s.Id == stackId);
        if (existing != null)
        {
            existing.Name = name;
            existing.OrganizationId = organizationId;
        }
        else
        {
            _dbContext.Stacks.Add(new Stack
            {
                Id = stackId,
                OrganizationId = organizationId,
                Name = name,
                CreatedDateTime = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();
    }

    protected async Task AssignRunnerToStack(Guid runnerId, Guid stackId, Guid organizationId)
    {
        var existing = await _dbContext.Set<RunnerStackSupply>()
            .FirstOrDefaultAsync(a => a.RunnerId == runnerId && a.StackId == stackId && a.OrganizationId == organizationId);
        if (existing != null) return;

        _dbContext.Set<RunnerStackSupply>().Add(new RunnerStackSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RunnerId = runnerId,
            StackId = stackId
        });
        await _dbContext.SaveChangesAsync();
    }

    protected async Task CreatePreseededStackSecret(
        AsyncServiceScope asyncServiceScope,
        Guid secretId, string name, string value, Guid stackId, Guid organizationId)
    {
        // 1. Entity row in DB
        var existing = await _dbContext.Secrets.OfType<StackSecret>().FirstOrDefaultAsync(s => s.Id == secretId);
        if (existing != null)
        {
            existing.Name = name;
            existing.StackId = stackId;
            existing.OrganizationId = organizationId;
        }
        else
        {
            _dbContext.Secrets.Add(new StackSecret
            {
                Id = secretId,
                OrganizationId = organizationId,
                StackId = stackId,
                Name = name
            });
        }
        await _dbContext.SaveChangesAsync();

        // 2. Value in the configured vault. Name convention matches
        // SecretService.MakeRemoteSecretName: "{scope}--{orgId}--{secretId}".
        var vaultFactory = asyncServiceScope.ServiceProvider.GetRequiredService<IVaultFactory>();
        var secretStoreSettings = asyncServiceScope.ServiceProvider
            .GetRequiredService<IOptions<SecretStoreSettings>>().Value;
        var inputVaultUrl = secretStoreSettings.AzureKeyVault.DefaultInputKeyVaultUrl;
        using var vault = vaultFactory.Create(inputVaultUrl);
        await vault.SetIfChanged($"stack--{organizationId}--{secretId}", value);
    }

    protected async Task CreatePreseededOrganization(string name, Guid organizationId)
    {
        var now = DateTime.UtcNow;
        var existingOrg = await _dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId);

        if (existingOrg != null)
        {
            existingOrg.Name = name;
            existingOrg.Status = OrganizationStatus.Active;
        }
        else
        {
            var organization = new Organization
            {
                Id = organizationId,
                Name = name,
                Status = OrganizationStatus.Active,
                CreatedDateTime = now
            };
            _dbContext.Organizations.Add(organization);
        }

        await _dbContext.SaveChangesAsync();
    }

    protected async Task SyncOrganizationRoles(Guid userId, Guid organizationId, List<OrganizationRole> organizationRoles)
    {
        foreach (var orgRole in organizationRoles)
        {
            var existingRoleAssignment = await _dbContext.Set<UserOrganizationRoleAssignment>()
                .FirstOrDefaultAsync(ra => ra.UserId == userId && ra.OrganizationId == organizationId && ra.RoleName == orgRole);

            if (existingRoleAssignment == null)
            {
                var userOrgRoleAssignment = new UserOrganizationRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    OrganizationId = organizationId,
                    RoleName = orgRole
                };
                _dbContext.Set<UserOrganizationRoleAssignment>().Add(userOrgRoleAssignment);
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    protected async Task SyncServicePrincipalOrganizationRoles(Guid servicePrincipalId, Guid organizationId, List<OrganizationRole> organizationRoles)
    {
        foreach (var orgRole in organizationRoles)
        {
            var existingRoleAssignment = await _dbContext.Set<ServicePrincipalOrganizationRoleAssignment>()
                .FirstOrDefaultAsync(ra => ra.ServicePrincipalId == servicePrincipalId && ra.OrganizationId == organizationId && ra.RoleName == orgRole);

            if (existingRoleAssignment == null)
            {
                var spOrgRoleAssignment = new ServicePrincipalOrganizationRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    ServicePrincipalId = servicePrincipalId,
                    OrganizationId = organizationId,
                    RoleName = orgRole
                };
                _dbContext.Set<ServicePrincipalOrganizationRoleAssignment>().Add(spOrgRoleAssignment);
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    protected async Task GrantSystemAdminRole(Guid userId)
    {
        var existingRoleAssignment = await _dbContext.Set<UserSystemRoleAssignment>()
            .FirstOrDefaultAsync(ra => ra.UserId == userId && ra.RoleName == SystemRole.Administrator);

        if (existingRoleAssignment == null)
        {
            var systemRoleAssignment = new UserSystemRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleName = SystemRole.Administrator,
                CreatedDateTime = DateTime.UtcNow
            };
            _dbContext.Set<UserSystemRoleAssignment>().Add(systemRoleAssignment);
            await _dbContext.SaveChangesAsync();
        }
    }
}
