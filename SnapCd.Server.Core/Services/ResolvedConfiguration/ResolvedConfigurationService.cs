// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Services.Crud.Secrets;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.Services.ResolvedConfiguration;

// "Declared" and "Applied" configuration

public class ResolvedConfigurationServiceFactory
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbFactory;
    private readonly StackSecretRepositoryFactory _stackSecretRepositoryFactory;
    private readonly NamespaceSecretRepositoryFactory _namespaceSecretRepositoryFactory;
    private readonly ModuleSecretRepositoryFactory _moduleSecretRepositoryFactory;
    private readonly SecretServiceFactory _secretServiceFactory;

    public ResolvedConfigurationServiceFactory(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        StackSecretRepositoryFactory stackSecretRepositoryFactory,
        NamespaceSecretRepositoryFactory namespaceSecretRepositoryFactory,
        ModuleSecretRepositoryFactory moduleSecretRepositoryFactory,
        // NamespaceInputFromSecretRepository<NamespaceEnvVarFromSecret> nsEnvVarSecretRepository,
        // NamespaceInputFromSecretRepository<NamespaceParamFromSecret> nsParamSecretRepository,
        SecretServiceFactory secretServiceFactory
    )
    {
        _dbFactory = dbFactory;
        _stackSecretRepositoryFactory = stackSecretRepositoryFactory;
        _namespaceSecretRepositoryFactory = namespaceSecretRepositoryFactory;
        _moduleSecretRepositoryFactory = moduleSecretRepositoryFactory;
        _secretServiceFactory = secretServiceFactory;
    }

    public ResolvedConfigurationService Create()
    {
        return new ResolvedConfigurationService(
            _dbFactory.CreateDbContext(),
            _stackSecretRepositoryFactory.Create(),
            _namespaceSecretRepositoryFactory.Create(),
            _moduleSecretRepositoryFactory.Create(),
            _secretServiceFactory
        );
    }
}

public class ResolvedConfigurationService : IDisposable
{
    private readonly SnapCdDbContext _dbContext;
    private readonly StackSecretRepository _stackSecretRepository;
    private readonly NamespaceSecretRepository _namespaceSecretRepository;
    private readonly ModuleSecretRepository _moduleSecretRepository;

    private readonly SecretServiceFactory _secretServiceFactory;
    // private readonly NamespaceInputFromSecretRepository<NamespaceEnvVarFromSecret> _nsEnvVarSecretRepository;
    // private readonly NamespaceInputFromSecretRepository<NamespaceParamFromSecret> _nsParamSecretRepository;

    public ResolvedConfigurationService(
        SnapCdDbContext dbContext,
        StackSecretRepository stackSecretRepository,
        NamespaceSecretRepository namespaceSecretRepository,
        ModuleSecretRepository moduleSecretRepository,
        SecretServiceFactory secretServiceFactory
    )
    {
        _dbContext = dbContext;
        _stackSecretRepository = stackSecretRepository;
        _namespaceSecretRepository = namespaceSecretRepository;
        _moduleSecretRepository = moduleSecretRepository;
        _secretServiceFactory = secretServiceFactory;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _stackSecretRepository.Dispose();
        _namespaceSecretRepository.Dispose();
        _moduleSecretRepository.Dispose();
    }

    public Task Deserialize(string resolvedConfigurationJson)
    {
        return Task.CompletedTask;
    }

    public Task<string> Serialize(ResolvedModule resolvedModule)
    {
        var options = new JsonSerializerOptions
            { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        return Task.FromResult(JsonSerializer.Serialize(resolvedModule, options));
    }

    public async Task<ResolvedModule> GetDeclared(Module module)
    {
        if (module.Runner.IsDisabled == true)
            throw new Exception($"The selected Runner (with name \"{module.Runner.Name}\") is disabled. Unable to execute Job.");


        // REMOVED: Output processing is now handled by client calls to OutputService
        // Client will call ListByModuleInputFromOutputs and ListByModuleInputFromOutputSets with module.Id


        // Handle new namespace scoped secret entities
        var paramSelectedNamespaceScopedSecrets = new List<SelectedNamespaceSecret>();
        paramSelectedNamespaceScopedSecrets.AddRange(await GetSelectedNamespaceScopedSecrets(module.Namespace.Id, module.Namespace.StackId, module.Namespace.Name,
            module.Namespace.NamespaceParamFromSecrets, module.OrganizationId));

        var envVarSelectedNamespaceScopedSecrets = new List<SelectedNamespaceSecret>();
        envVarSelectedNamespaceScopedSecrets.AddRange(await GetSelectedNamespaceScopedSecrets(module.Namespace.Id, module.Namespace.StackId, module.Namespace.Name,
            module.Namespace.NamespaceEnvVarFromSecrets, module.OrganizationId));


        // Handle new scoped secret entities
        var paramSelectedModuleScopedSecrets = new List<SelectedModuleSecret>();
        paramSelectedModuleScopedSecrets.AddRange(await GetSelectedModuleScopedSecrets(module.Id, module.NamespaceId, module.Namespace.StackId, module.Name, module.ModuleParamFromSecrets,
            module.OrganizationId));

        var envVarSelectedModuleScopedSecrets = new List<SelectedModuleSecret>();
        envVarSelectedModuleScopedSecrets.AddRange(await GetSelectedModuleScopedSecrets(module.Id, module.NamespaceId, module.Namespace.StackId, module.Name, module.ModuleEnvVarFromSecrets,
            module.OrganizationId));

        // Get dependencies from output references
        var outputDependencies = new List<DependsOnModuleResolved>();

        // Add dependencies from ModuleInputFromOutput
        outputDependencies.AddRange(module.ModuleParamFromOutputs
            .Select(x => new DependsOnModuleResolved { ModuleId = x.OutputModuleId }));
        outputDependencies.AddRange(module.ModuleEnvVarFromOutputs
            .Select(x => new DependsOnModuleResolved { ModuleId = x.OutputModuleId }));

        // Add dependencies from ModuleInputFromOutputSet
        outputDependencies.AddRange(module.ModuleParamFromOutputSets
            .Select(x => new DependsOnModuleResolved { ModuleId = x.OutputModuleId }));

        var dependsOnModules = outputDependencies
            // explicitly defined dependencies
            .Concat(GetExplicitDependencies(module.DependsOnModules))
            .GroupBy(x => x.ModuleId)
            .Select(g => g.First())
            .ToList();

        var moduleExtraFiles = module.ModuleExtraFiles.Select(ModuleExtraFileMapper.ToExtraFileDto).ToList();
        var namespaceExtraFiles = module.Namespace.NamespaceExtraFiles.Select(NamespaceExtraFileMapper.ToExtraFileDto).ToList();

        var declaredConfiguration = new ResolvedModule
        {
            ModuleId = module.Id,
            NamespaceId = module.Namespace.Id,
            StackId = module.Namespace.Stack.Id,
            OrganizationId = module.OrganizationId,
            RunnerId = module.RunnerId,

            ExtraFiles = await ResolveExtraFiles(moduleExtraFiles, namespaceExtraFiles, module.IgnoreNamespaceExtraFiles),

            ModuleName = module.Name,
            NamespaceName = module.Namespace.Name,
            StackName = module.Namespace.Stack.Name,
            RunnerName = module.Runner.Name,
            RunnerInstanceName = module.RunnerInstanceName,
            SourceSubdirectory = module.SourceSubdirectory,
            SourceType = module.SourceType,
            SourceRevisionType = module.SourceRevisionType,
            DependsOnModules = dependsOnModules,
            // REMOVED: Output selections are now handled by client calls to OutputService
            // SelectedModuleParamsFromOutputs = paramSelectedOutputs,
            // SelectedModuleOutputEnvVars = envVarSelectedOutputs,
            // SelectedModuleParamsFromOutputSets = paramSelectedOutputSets,
            // SelectedModuleOutputSetEnvVars = envVarSelectedOutputSets,

            SelectedModuleParamsFromSecrets = paramSelectedModuleScopedSecrets,
            SelectedModuleEnvVarsFromSecrets = envVarSelectedModuleScopedSecrets,

            SelectedNamespaceParamsFromSecrets = paramSelectedNamespaceScopedSecrets,
            SelectedNamespaceEnvVarsFromSecrets = envVarSelectedNamespaceScopedSecrets,

            ModuleEnvVarFromDefinitions = module.ModuleEnvVarFromDefinitions?.Select(ModuleInputFromDefinitionMapper.ToDto).ToList(),
            ModuleEnvVarFromLiterals = module.ModuleEnvVarFromLiterals?.Select(ModuleInputFromLiteralMapper.ToDto).ToList(),
            ModuleEnvVarFromNamespaces = module.ModuleEnvVarFromNamespaces?.Select(ModuleInputFromNamespaceMapper.ToDto).ToList(),

            ModuleParamFromDefinitions = module.ModuleParamFromDefinitions?.Select(ModuleInputFromDefinitionMapper.ToDto).ToList(),
            ModuleParamFromLiterals = module.ModuleParamFromLiterals?.Select(ModuleInputFromLiteralMapper.ToDto).ToList(),
            ModuleParamFromNamespaces = module.ModuleParamFromNamespaces?.Select(ModuleInputFromNamespaceMapper.ToDto).ToList(),

            NamespaceParamFromLiterals = module.Namespace.NamespaceParamFromLiterals.Select(NamespaceInputFromLiteralMapper.ToDto).ToList(),
            NamespaceEnvVarFromLiterals = module.Namespace.NamespaceEnvVarFromLiterals.Select(NamespaceInputFromLiteralMapper.ToDto).ToList(),

            NamespaceParamFromDefinitions = module.Namespace.NamespaceParamFromDefinitions.Select(NamespaceInputFromDefinitionMapper.ToDto).ToList(),
            NamespaceEnvVarFromDefinitions = module.Namespace.NamespaceEnvVarFromDefinitions.Select(NamespaceInputFromDefinitionMapper.ToDto).ToList(),

            InitBeforeHook = ResolveHook(HookTask.Init, HookPhase.Before, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            InitAfterHook = ResolveHook(HookTask.Init, HookPhase.After, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            IgnoreNamespaceExtraFiles = module.IgnoreNamespaceExtraFiles,

            CleanInitEnabled = module.CleanInitEnabled ?? module.Namespace.DefaultCleanInitEnabled ?? false,
            DriftCheckEnabled = module.DriftCheckEnabled ?? module.Namespace.DefaultDriftCheckEnabled ?? false,
            DriftCheckIntervalMinutes = module.DriftCheckIntervalMinutes ?? module.Namespace.DefaultDriftCheckIntervalMinutes,
            PlanBeforeHook = ResolveHook(HookTask.Plan, HookPhase.Before, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            PlanAfterHook = ResolveHook(HookTask.Plan, HookPhase.After, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            PlanDestroyBeforeHook = ResolveHook(HookTask.PlanDestroy, HookPhase.Before, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            PlanDestroyAfterHook = ResolveHook(HookTask.PlanDestroy, HookPhase.After, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            ApplyBeforeHook = ResolveHook(HookTask.Apply, HookPhase.Before, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            ApplyAfterHook = ResolveHook(HookTask.Apply, HookPhase.After, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            DestroyBeforeHook = ResolveHook(HookTask.Destroy, HookPhase.Before, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            DestroyAfterHook = ResolveHook(HookTask.Destroy, HookPhase.After, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            OutputBeforeHook = ResolveHook(HookTask.Output, HookPhase.Before, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            OutputAfterHook = ResolveHook(HookTask.Output, HookPhase.After, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            ValidateBeforeHook = ResolveHook(HookTask.Validate, HookPhase.Before, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            ValidateAfterHook = ResolveHook(HookTask.Validate, HookPhase.After, module.Hooks, module.Namespace.Hooks, module.IgnoreNamespaceHooks),
            SourceRevision = module.SourceRevision,
            SourceUrl = module.SourceUrl,

            ApprovalTimeoutMinutes = module.ApprovalTimeoutMinutes ?? module.Namespace.DefaultApprovalTimeoutMinutes,

            Engine = GetEngineString(module.Engine, module.Namespace.DefaultEngine),
            PulumiFlags = module.IgnoreNamespaceFlags
                ? module.PulumiFlags.Select(f => new PulumiFlagEntry { Task = f.Task, Flag = f.Flag, Value = f.Value }).ToList()
                : MergePulumiFlags(module.Namespace.PulumiFlags, module.PulumiFlags),
            PulumiArrayFlags = module.IgnoreNamespaceFlags
                ? module.PulumiArrayFlags.Select(f => new PulumiArrayFlagEntry { Task = f.Task, Flag = f.Flag, Value = f.Value }).ToList()
                : MergePulumiArrayFlags(module.Namespace.PulumiArrayFlags, module.PulumiArrayFlags),
            TerraformFlags = module.IgnoreNamespaceFlags
                ? module.TerraformFlags.Select(f => new TerraformFlagEntry { Task = f.Task, Flag = f.Flag, Value = f.Value }).ToList()
                : MergeTerraformFlags(module.Namespace.TerraformFlags, module.TerraformFlags),
            TerraformArrayFlags = module.IgnoreNamespaceFlags
                ? module.TerraformArrayFlags.Select(f => new TerraformArrayFlagEntry { Task = f.Task, Flag = f.Flag, Value = f.Value }).ToList()
                : MergeTerraformArrayFlags(module.Namespace.TerraformArrayFlags, module.TerraformArrayFlags)
        };

        // var appliedConfigurationJson = await Serialize(declaredConfiguration);
        //
        // declaredConfiguration.ModuleName = "foo";
        // declaredConfiguration.NamespaceParams = null;
        // declaredConfiguration.DependsOnModules.Add(new DependsOnModule()
        // {
        //     ModuleId = new Guid(),
        //     NamespaceId = new Guid(),
        // });
        // // declaredStateDto.DependsOnModules.RemoveAll(x =>
        // //     x.NamespaceId == new Guid("a941ab91-d3ae-41b4-9557-c69d20df4f2a") && x.ModuleId is null);
        //
        //
        // var declaredConfigurationJson = await Serialize(declaredConfiguration);
        //
        // var diff = JsonComparer.GetDifferences(appliedConfigurationJson, declaredConfigurationJson);

        return declaredConfiguration;
    }

    public Task<List<ExtraFileDto>> ResolveExtraFiles(List<ExtraFileDto> moduleExtraFiles, List<ExtraFileDto> namespaceExtraFiles, bool ignoreNamespaceExtraFiles = false)
    {
        // If ignoring namespace extra files, return only module extra files
        if (ignoreNamespaceExtraFiles) return Task.FromResult(moduleExtraFiles);

        // Start with namespace extra files as the base
        var mergedFiles = new List<ExtraFileDto>(namespaceExtraFiles);

        // Add module extra files, overriding namespace files with the same name
        foreach (var moduleFile in moduleExtraFiles)
        {
            // Remove any namespace file with the same name (module overrides namespace)
            mergedFiles.RemoveAll(nsFile => nsFile.FileName == moduleFile.FileName);
            // Add the module file
            mergedFiles.Add(moduleFile);
        }

        return Task.FromResult(mergedFiles);
    }


    public async Task<ResolvedModule> GetDeclared(Guid id, Guid organizationId)
    {
        var module = await _dbContext.Modules
            .AsNoTracking()
            .AsSplitQuery() // This has been added to prevent a single massive join and instead query every collection seperately. TODO replace with DB triggers?
            .Include(x => x.ModuleParamFromDefinitions)
            .Include(x => x.ModuleParamFromLiterals)
            .Include(x => x.ModuleParamFromSecrets)
            .ThenInclude(x => x.Secret)
            .Include(x => x.ModuleParamFromNamespaces)
            .Include(x => x.ModuleParamFromOutputs)
            .Include(x => x.ModuleParamFromOutputSets)
            .Include(x => x.ModuleEnvVarFromDefinitions)
            .Include(x => x.ModuleEnvVarFromLiterals)
            .Include(x => x.ModuleEnvVarFromSecrets)
            .ThenInclude(x => x.Secret)
            .Include(x => x.ModuleEnvVarFromNamespaces)
            .Include(x => x.ModuleEnvVarFromOutputs)
            .Include(x => x.ModuleExtraFiles)
            .Include(x => x.DependsOnModules)
            .Include(m => m.Runner)
            .Include(m => m.Namespace)
            .Include(m => m.Namespace.Stack)
            .Include(m => m.Namespace.NamespaceExtraFiles)
            .Include(m => m.Namespace.NamespaceEnvVarFromDefinitions)
            .Include(m => m.Namespace.NamespaceEnvVarFromLiterals)
            .Include(m => m.Namespace.NamespaceEnvVarFromSecrets)
            .ThenInclude(x => x.Secret)
            .Include(m => m.Namespace.NamespaceParamFromDefinitions)
            .Include(m => m.Namespace.NamespaceParamFromLiterals)
            .Include(m => m.Namespace.NamespaceParamFromSecrets)
            .ThenInclude(x => x.Secret)
            .Include(m => m.PulumiFlags)
            .Include(m => m.PulumiArrayFlags)
            .Include(m => m.TerraformFlags)
            .Include(m => m.TerraformArrayFlags)
            .Include(m => m.Hooks)
            .Include(m => m.Namespace.PulumiFlags)
            .Include(m => m.Namespace.PulumiArrayFlags)
            .Include(m => m.Namespace.TerraformFlags)
            .Include(m => m.Namespace.TerraformArrayFlags)
            .Include(m => m.Namespace.Hooks)
            .FirstOrDefaultAsync(m => m.Id == id && m.OrganizationId == organizationId);

        if (module == null) throw new EntityNotFoundException($"Module with id {id} not found");


        return await GetDeclared(module);
    }


    public async Task<List<SelectedNamespaceSecret>> GetSelectedNamespaceScopedSecrets<T>(
        Guid namespaceId, Guid stackId, string namespaceName, IEnumerable<T> inputsEnumerable, Guid organizationId)
        where T : Entities.Definition.Base.NamespaceInput
    {
        var inputs = inputsEnumerable.ToList();
        var result = new ConcurrentBag<SelectedNamespaceSecret>();

        var tasks = inputs.Select(async input =>
        {
            // Get the secret from the input based on its type
            Secret? secret;
            InputType type; // Default value

            switch (input)
            {
                case NamespaceInputFromSecret namespaceInputFromSecret:
                    secret = namespaceInputFromSecret.Secret;
                    type = namespaceInputFromSecret.Type;
                    break;
                default:
                    return; // Unknown type, skip
            }


            if (secret is ModuleSecret moduleSecret)
                throw new InvalidSecretScopeException(
                    $"The referenced NamespaceInputFromSecret within Id {secret.Id} is scoped to a Module. A NamespaceInputFromSecret must be referenced from a Secret scoped either to the Namespace or to its Stack.");

            if (secret is NamespaceSecret namespaceSecret)
                if (namespaceSecret.NamespaceId != namespaceId)
                    throw new InvalidSecretScopeException(
                        $"The referenced secret within Id {secret.Id} is scoped to a different namespace (ID {namespaceSecret.NamespaceId}) than the one being processed (ID {namespaceId}).");

            if (secret is StackSecret stackSecret)
                if (stackSecret.StackId != stackId)
                    throw new InvalidSecretScopeException(
                        $"The referenced secret within Id {secret.Id} is scoped to a different stack (ID {stackSecret.StackId}) than the one being processed (ID {stackId}).");


            var secretService = _secretServiceFactory.Create();
            var value = await secretService.GetRemoteNonsecured(secret.Id, organizationId);
            var hash = CreateValueHash(value);

            var selected = new SelectedNamespaceSecret
            {
                InputId = input.Id,
                InputName = input.Name,
                Type = type,
                NamespaceId = namespaceId,
                NamespaceName = namespaceName,
                UsageMode = input.UsageMode,
                SecretId = secret.Id,
                SecretName = secret.Name,
                Discriminator = secret is ISecretScoped scoped ? scoped.GetSecretDiscriminator() : SecretDiscriminator.NamespaceSecret,
                Hash = hash
            };

            result.Add(selected);
        });

        await Task.WhenAll(tasks);
        return result.ToList();
    }

    public async Task<List<SelectedModuleSecret>> GetSelectedModuleScopedSecrets<T>(
        Guid moduleId, Guid namespaceId, Guid stackId, string moduleName, IEnumerable<T> inputsEnumerable, Guid organizationId)
        where T : Entities.Definition.Base.ModuleInput
    {
        var inputs = inputsEnumerable.ToList();
        var result = new ConcurrentBag<SelectedModuleSecret>();

        var tasks = inputs.Select(async input =>
        {
            // Get the secret from the input based on its type
            Secret? secret = null;
            var type = InputType.String; // Default value

            switch (input)
            {
                case ModuleInputFromSecret moduleInputFromSecret:
                    secret = moduleInputFromSecret.Secret;
                    type = moduleInputFromSecret.Type;
                    break;
            }

            if (secret == null) return;

            var secretService = _secretServiceFactory.Create();
            var value = await secretService.GetRemoteNonsecured(secret.Id, organizationId);
            var hash = CreateValueHash(value);

            if (secret is ModuleSecret moduleSecret)
                if (moduleSecret.ModuleId != moduleId)
                    throw new InvalidSecretScopeException(
                        $"The referenced secret within Id {secret.Id} is scoped to a different module (ID {moduleSecret.ModuleId}) than the one being processed (ID {moduleId}).");

            if (secret is NamespaceSecret namespaceSecret)
                if (namespaceSecret.NamespaceId != namespaceId)
                    throw new InvalidSecretScopeException(
                        $"The referenced secret within Id {secret.Id} is scoped to a different namespace (ID {namespaceSecret.NamespaceId}) than the one being processed (ID {namespaceId}).");

            if (secret is StackSecret stackSecret)
                if (stackSecret.StackId != stackId)
                    throw new InvalidSecretScopeException(
                        $"The referenced secret within Id {secret.Id} is scoped to a different stack (ID {stackSecret.StackId}) than the one being processed (ID {stackId}).");


            var selected = new SelectedModuleSecret
            {
                InputName = input.Name,
                Type = type,
                ModuleId = moduleId,
                ModuleName = moduleName,
                SecretId = secret.Id,
                SecretName = secret.Name,
                Discriminator = secret is ISecretScoped scoped ? scoped.GetSecretDiscriminator() : SecretDiscriminator.ModuleSecret,
                Hash = hash
            };

            result.Add(selected);
        });

        await Task.WhenAll(tasks);
        return result.ToList();
    }

    // REMOVED: GetSelectedOutputs and GetSelectedOutputSets methods
    // Output processing is now handled by client calls to OutputService endpoints:
    // - ListByModuleInputFromOutputs(Guid moduleId)
    // - ListByModuleInputFromOutputSets(Guid moduleId)

    private string GetEngineString(StateManagementEngine? engine, StateManagementEngine? defaultEngine)
    {
        var globalDefaultEngine = StateManagementEngine.OpenTofu;
        var resolvedEngine = engine ?? defaultEngine ?? globalDefaultEngine;

        return resolvedEngine switch
        {
            StateManagementEngine.OpenTofu => "tofu",
            StateManagementEngine.Terraform => "terraform",
            StateManagementEngine.Pulumi => "pulumi",
            _ => throw new InvalidOperationException($"Unknown engine: {engine}")
        };
    }


    public List<DependsOnModuleResolved> GetExplicitDependencies(List<DependsOnModule> dependsOnModuleEntities)
    {
        return dependsOnModuleEntities.Select(dependency => new DependsOnModuleResolved
        {
            ModuleId = dependency.DependsOnModuleId,
            DepedencySourceMessage = $"Explicitly defined dependency to Module ID \"{dependency.DependsOnModuleId}\""
        }).ToList();
    }

    public static string CreateValueHash(string? value)
    {
        // TODO, this is deterministic and cannot be "unhashed". Nevertheless might want to salt this.
        if (value == null)
            return "";

        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(HmacHash(value));
            var hashBytes = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower(); // Convert to hex string
        }
    }

    public static string HmacHash(string? value)
    {
        var secretKey = Encoding.UTF8.GetBytes("Ödff?&dvb#sd14");

        if (string.IsNullOrEmpty(value))
            return "";

        using (var hmac = new HMACSHA256(secretKey))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hashBytes).ToLower(); // Convert to hex
        }
    }

    /// <summary>
    /// Merge namespace and module single-value Pulumi flags.
    /// Module flags override namespace flags for matching (Task, Flag) pairs.
    /// </summary>
    private static List<PulumiFlagEntry> MergePulumiFlags(
        List<NamespacePulumiFlag> namespaceFlags,
        List<ModulePulumiFlag> moduleFlags)
    {
        var merged = new Dictionary<(PulumiCommandTask, PulumiFlag), PulumiFlagEntry>();

        foreach (var nsFlag in namespaceFlags)
        {
            merged[(nsFlag.Task, nsFlag.Flag)] = new PulumiFlagEntry
            {
                Task = nsFlag.Task,
                Flag = nsFlag.Flag,
                Value = nsFlag.Value
            };
        }

        foreach (var modFlag in moduleFlags)
        {
            merged[(modFlag.Task, modFlag.Flag)] = new PulumiFlagEntry
            {
                Task = modFlag.Task,
                Flag = modFlag.Flag,
                Value = modFlag.Value
            };
        }

        return merged.Values.ToList();
    }

    /// <summary>
    /// Merge namespace and module array Pulumi flags.
    /// If the module has ANY entries for a given (Task, Flag), use only the module's entries.
    /// Otherwise, fall back to namespace defaults for that (Task, Flag).
    /// </summary>
    private static List<PulumiArrayFlagEntry> MergePulumiArrayFlags(
        List<NamespacePulumiArrayFlag> namespaceFlags,
        List<ModulePulumiArrayFlag> moduleFlags)
    {
        var moduleGroups = moduleFlags
            .GroupBy(f => (f.Task, f.Flag))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<PulumiArrayFlagEntry>();

        // Add all module-level array flags
        foreach (var modFlag in moduleFlags)
        {
            result.Add(new PulumiArrayFlagEntry
            {
                Task = modFlag.Task,
                Flag = modFlag.Flag,
                Value = modFlag.Value
            });
        }

        // Add namespace flags only where module has no entries for that (Task, Flag)
        foreach (var nsFlag in namespaceFlags)
        {
            if (!moduleGroups.ContainsKey((nsFlag.Task, nsFlag.Flag)))
            {
                result.Add(new PulumiArrayFlagEntry
                {
                    Task = nsFlag.Task,
                    Flag = nsFlag.Flag,
                    Value = nsFlag.Value
                });
            }
        }

        return result;
    }

    private static string? ResolveHook(
        HookTask task, HookPhase phase,
        List<ModuleHook> moduleHooks, List<NamespaceHook> namespaceHooks,
        bool ignoreNamespace)
    {
        var mh = moduleHooks.FirstOrDefault(h => h.Task == task && h.Phase == phase);
        if (mh != null) return mh.Script;
        if (ignoreNamespace) return null;
        var nh = namespaceHooks.FirstOrDefault(h => h.Task == task && h.Phase == phase);
        return nh?.Script;
    }

    /// <summary>
    /// Merge namespace and module single-value Terraform flags.
    /// Module flags override namespace flags for matching (Task, Flag) pairs.
    /// </summary>
    private static List<TerraformFlagEntry> MergeTerraformFlags(
        List<NamespaceTerraformFlag> namespaceFlags,
        List<ModuleTerraformFlag> moduleFlags)
    {
        var merged = new Dictionary<(TerraformCommandTask, TerraformFlag), TerraformFlagEntry>();

        foreach (var nsFlag in namespaceFlags)
        {
            merged[(nsFlag.Task, nsFlag.Flag)] = new TerraformFlagEntry
            {
                Task = nsFlag.Task,
                Flag = nsFlag.Flag,
                Value = nsFlag.Value
            };
        }

        foreach (var modFlag in moduleFlags)
        {
            merged[(modFlag.Task, modFlag.Flag)] = new TerraformFlagEntry
            {
                Task = modFlag.Task,
                Flag = modFlag.Flag,
                Value = modFlag.Value
            };
        }

        return merged.Values.ToList();
    }

    /// <summary>
    /// Merge namespace and module array Terraform flags.
    /// If the module has ANY entries for a given (Task, Flag), use only the module's entries.
    /// Otherwise, fall back to namespace defaults for that (Task, Flag).
    /// </summary>
    private static List<TerraformArrayFlagEntry> MergeTerraformArrayFlags(
        List<NamespaceTerraformArrayFlag> namespaceFlags,
        List<ModuleTerraformArrayFlag> moduleFlags)
    {
        var moduleGroups = moduleFlags
            .GroupBy(f => (f.Task, f.Flag))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<TerraformArrayFlagEntry>();

        // Add all module-level array flags
        foreach (var modFlag in moduleFlags)
        {
            result.Add(new TerraformArrayFlagEntry
            {
                Task = modFlag.Task,
                Flag = modFlag.Flag,
                Value = modFlag.Value
            });
        }

        // Add namespace flags only where module has no entries for that (Task, Flag)
        foreach (var nsFlag in namespaceFlags)
        {
            if (!moduleGroups.ContainsKey((nsFlag.Task, nsFlag.Flag)))
            {
                result.Add(new TerraformArrayFlagEntry
                {
                    Task = nsFlag.Task,
                    Flag = nsFlag.Flag,
                    Value = nsFlag.Value
                });
            }
        }

        return result;
    }
}