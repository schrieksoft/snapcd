// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.ParamResolver.Helpers;

namespace SnapCd.Server.Core.Services.ParamResolver;

public class ParamResolver<TModuleInputFromOutput>
    where TModuleInputFromOutput : ModuleInputFromOutput
{
    private readonly ServerTaskContext _context;

    // Define the parameters with their corresponding values
    private readonly Dictionary<DefinitionInputType, string> _definitionParams;

    private readonly ILogger<ParamResolver<TModuleInputFromOutput>> _logger;

    private readonly NamespaceParamResolver _nsParamResolver;

    private readonly OutputParamResolver<TModuleInputFromOutput> _outputParamResolver;
    private readonly OutputSetParamResolver? _outputSetParamResolver;
    private readonly SecretParamResolver _secretParamResolver;
    private readonly List<ModuleInputFromDefinitionReadDto> _fromDefinitionParams;
    private readonly List<ModuleInputFromLiteralReadDto> _fromLiteralParams;
    private readonly List<SelectedModuleSecret> _selectedModuleSecrets;
    private readonly Guid _moduleId;
    private readonly Guid _organizationId;
    private readonly string _engine;


    public ParamResolver(
        ServerTaskContext context,
        List<ModuleInputFromDefinitionReadDto> fromDefinitionParams,
        List<ModuleInputFromLiteralReadDto> fromLiteralParams,
        List<ModuleInputFromNamespaceReadDto> fromNamespaceParams,
        List<NamespaceInputFromLiteralReadDto> fromLiteralNamespaceParams,
        List<NamespaceInputFromDefinitionReadDto> fromDefinitionNamespaceParams,
        OutputParamResolver<TModuleInputFromOutput> outputParamResolver,
        OutputSetParamResolver? outputSetParamResolver,
        SecretParamResolver secretParamResolver,
        ILogger<ParamResolver<TModuleInputFromOutput>> logger,
        ILogger<NamespaceParamResolver> nsLogger,
        List<SelectedModuleSecret> selectedModuleSecrets,
        List<SelectedNamespaceSecret> selectedNamespaceSecrets,
        // Properties previously from Declared
        Guid stackId,
        string stackName,
        Guid namespaceId,
        string namespaceName,
        Guid moduleId,
        string moduleName,
        string sourceRevision,
        string sourceUrl,
        string sourceSubdirectory,
        Guid organizationId,
        string engine
    )
    {
        // module params
        _fromDefinitionParams = fromDefinitionParams;
        _fromLiteralParams = fromLiteralParams;
        _outputParamResolver = outputParamResolver;
        _outputSetParamResolver = outputSetParamResolver;
        _secretParamResolver = secretParamResolver;
        _logger = logger;
        _context = context;
        _moduleId = moduleId;
        _organizationId = organizationId;
        _engine = engine;
        _selectedModuleSecrets = selectedModuleSecrets;

        _definitionParams = new Dictionary<DefinitionInputType, string>
        {
            { DefinitionInputType.StackId, stackId.ToString() },
            { DefinitionInputType.StackName, stackName },

            { DefinitionInputType.NamespaceId, namespaceId.ToString() },
            { DefinitionInputType.NamespaceName, namespaceName },

            { DefinitionInputType.ModuleId, moduleId.ToString() },
            { DefinitionInputType.ModuleName, moduleName },

            { DefinitionInputType.SourceRevision, sourceRevision },
            { DefinitionInputType.SourceUrl, sourceUrl },
            { DefinitionInputType.SourceSubdirectory, sourceSubdirectory }
        };


        _nsParamResolver =
            new NamespaceParamResolver(
                fromNamespaceParams,
                fromLiteralNamespaceParams,
                fromDefinitionNamespaceParams,
                _secretParamResolver,
                nsLogger,
                context,
                _definitionParams,
                selectedNamespaceSecrets,
                organizationId);
    }

    private async Task<List<ModuleResolvedInput>> GetModuleInputFromSecrets(bool formatStrings = true)
    {
        var result = new List<ModuleResolvedInput>();

        // Call service directly instead of HTTP client
        foreach (var discriminator in SecretDiscriminatorConstants.AllSecretDiscriminators)
        {
            var secrets = _selectedModuleSecrets
                .Where(x => x.Discriminator == discriminator)
                .ToList();

            var mappedSecrets = await _secretParamResolver.ListRemoteByIds(
                secrets
                    .Where(x => x.SecretId.HasValue)
                    .Select(x => x.SecretId!.Value)
                    .ToList(),
                _organizationId);

            foreach (var secret in secrets)
            {
                var value = mappedSecrets.FirstOrDefault(x => x.Id == secret.SecretId)?.Value ?? string.Empty;

                value = formatStrings ? FormatValue(value, secret.Type) : value;

                var resolvedInput = new ModuleResolvedInput
                {
                    Name = secret.InputName,
                    ResolvedValue = value,
                    Source = ModuleInputSource.ModuleSecret,
                    OriginalValue = secret.SecretName ?? string.Empty
                };

                result.Add(resolvedInput);
            }
        }

        return result.ToList();
    }

    private async Task<List<ModuleResolvedInput>> GetModuleInputFromOutputSets(bool formatStrings = true)
    {
        if (_outputSetParamResolver == null)
            return new List<ModuleResolvedInput>();

        return await _outputSetParamResolver.ListByModuleInputFromOutputSets(_moduleId, _organizationId, _engine, formatStrings);
    }


    private async Task<List<ModuleResolvedInput>> GetModuleInputFromOutputs(bool formatStrings = true)
    {
        return await _outputParamResolver.ListByModuleInputFromOutputs(_moduleId, _organizationId, formatStrings);
    }

    private async Task<List<ModuleResolvedInput>> GetInputFromLiterals(bool formatStrings = true)
    {
        var exceptions = new List<Exception>();
        var literalParams = _fromLiteralParams
            .ToList();


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        var tasks = literalParams.Select(async entry =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            try
            {
                var value = formatStrings ? FormatValue(entry.LiteralValue, entry.Type) : entry.LiteralValue;

                _context.LogInformation($"Successfully resolved parameter \"{entry.Name}\" from source \"{ModuleInputSource.Literal.ToString()}\" with value \"{entry.LiteralValue}\"");

                return new ModuleResolvedInput
                {
                    Name = entry.Name,
                    OriginalValue = entry.LiteralValue,
                    ResolvedValue = value,
                    Source = ModuleInputSource.Literal
                };
            }
            catch (Exception ex)
            {
                exceptions.Add(new Exception($"Error resolving literal parameter {entry.Name}", ex));
                _context.LogError($"Failed to resolve parameter \"{entry.Name}\" from source \"{ModuleInputSource.Literal.ToString()}\" with value \"{entry.LiteralValue}\"");
                return null;
            }
        });

        var results = (await Task.WhenAll(tasks)).Where(p => p != null).Cast<ModuleResolvedInput>().ToList();

        if (exceptions.Any())
            throw new AggregateException("Errors occurred while resolving literal parameters.", exceptions);

        return results;
    }

    // private Task<List<ModuleResolvedInput>> GetDefinitionParams(bool formatStrings = true)
    // {
    //     return Task.FromResult(_definitionParams
    //         .Select(kvp => new ModuleResolvedInput
    //         {
    //             Name = kvp.Key.ToString(),
    //             OriginalValue = kvp.Value,
    //             ResolvedValue = formatStrings ? FormatValue(kvp.Value, InputType.String) : kvp.Value,
    //             Source = ModuleInputSource.Definition
    //         })
    //         .ToList());
    // }

    private async Task<List<ModuleResolvedInput>> GetFilteredDefinitionInputs(bool formatStrings = true)
    {
        var exceptions = new List<Exception>();

        // Create a lookup for param.Value to multiple param.Name in the _params collection
        var paramDefinitionLookup = _fromDefinitionParams
            .ToLookup(param => param.DefinitionName, param => param.Name);

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        var tasks = _definitionParams.Select(async entry =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            try
            {
                // Get the matching param names from the lookup
                var resolvedNames = string.Join(", ", paramDefinitionLookup[entry.Key]);

                if (resolvedNames == "") return Enumerable.Empty<ModuleResolvedInput>();

                _context.LogInformation($"Successfully resolved parameter(s) \"{resolvedNames}\" from source \"{ModuleInputSource.Definition}\" with value \"{entry.Value}\"");

                // Create a ResolvedParam for each matching Name
                return paramDefinitionLookup[entry.Key].Select(name => new ModuleResolvedInput
                {
                    Name = name,
                    OriginalValue = entry.Key.ToString(),
                    ResolvedValue = formatStrings ? FormatValue(entry.Value, InputType.String) : entry.Value,
                    Source = ModuleInputSource.Literal
                });
            }
            catch (Exception ex)
            {
                // Log the error for the failed resolution


                var failedNames = string.Join(", ",
                    paramDefinitionLookup[(DefinitionInputType)Enum.Parse(typeof(DefinitionInputType), entry.Value)]);
                exceptions.Add(new Exception($"Error resolving definition parameter(s) {failedNames}", ex));

                _context.LogError($"Failed to resolve parameter(s) \"{failedNames}\" from source \"{ModuleInputSource.Definition}\" with value \"{entry.Value}\". Error: {ex.Message}");

                return Enumerable.Empty<ModuleResolvedInput>();
            }
        });

        // Await all tasks and flatten results
        var results = (await Task.WhenAll(tasks)).SelectMany(p => p).ToList();

        if (exceptions.Any())
            throw new AggregateException("Errors occurred while resolving definition parameters.", exceptions);

        return results;
    }


    public async Task<List<ModuleResolvedInput>> GetAllInputs(bool formatStrings = true)
    {
        var exceptions = new List<Exception>();
        var allItems = new List<ModuleResolvedInput>();

        try
        {
            allItems.AddRange(await GetModuleInputFromSecrets(formatStrings));
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.InnerExceptions);
        }

        try
        {
            allItems.AddRange(await GetModuleInputFromOutputs(formatStrings));
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.InnerExceptions);
        }

        try
        {
            allItems.AddRange(await GetFilteredDefinitionInputs(formatStrings));
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.InnerExceptions);
        }

        try
        {
            allItems.AddRange(await GetInputFromLiterals(formatStrings));
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.InnerExceptions);
        }

        try
        {
            allItems.AddRange(await GetModuleInputFromOutputSets(formatStrings));
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.InnerExceptions);
        }


        if (exceptions.Any()) throw new AggregateException("Errors occurred while getting all parameters.", exceptions);

        return allItems;
    }

    private Task<List<ModuleResolvedInput>> DealWithConflicts(List<ModuleResolvedInput> allItems)
    {
        // If OutputSet and *one* other source is set, then the other source "overrides" the OutputSet
        // If however more than one of the others are set, then throw an exception (there is no "priority" in that case)

        // New conflict resolution logic
        var resolvedItems = new List<ModuleResolvedInput>();
        var conflictExceptions = new List<Exception>();

        foreach (var group in allItems.GroupBy(i => i.Name))
        {
            var nonOutputSetItems = group.Where(i => i.Source != ModuleInputSource.ModuleOutputSet).ToList();
            var outputSetItems = group.Where(i => i.Source == ModuleInputSource.ModuleOutputSet).ToList();

            // Check for conflicts between non-OutputSet sources
            var distinctNonOutputSources = nonOutputSetItems.Select(i => i.Source).Distinct().ToList();
            if (distinctNonOutputSources.Count > 1)
            {
                conflictExceptions.Add(new InvalidOperationException(
                    $"Input '{group.Key}' has conflicting sources: {string.Join(", ", distinctNonOutputSources)}"
                ));
                continue;
            }

            // Check for presence of both non-OutputSet and OutputSet inputs

            foreach (var nonOutputSetItem in nonOutputSetItems)
                if (outputSetItems.Any(x => x.Name == nonOutputSetItem.Name))
                    _context.LogWarning($"Input '{group.Key}' has both {nonOutputSetItems.First().Source} and OutputSet sources. " +
                                        $"Using Input from {nonOutputSetItems.First().Source} source.");


            // Resolve according to rules
            if (nonOutputSetItems.Any())
                // Take the first non-OutputSet item (all are same source due to previous check)
                resolvedItems.Add(nonOutputSetItems.First());
            else if (outputSetItems.Any())
                // Take the first OutputSet item if no others exist
                resolvedItems.Add(outputSetItems.First());
        }

        if (conflictExceptions.Any()) throw new AggregateException("Conflicts detected in input sources.", conflictExceptions);

        return Task.FromResult(resolvedItems);
    }

    private async Task<List<ModuleResolvedInput>> GetMergedParameters(bool formatStrings = true)
    {
        var resolvedParams = await GetAllInputs(formatStrings);
        resolvedParams = await DealWithConflicts(resolvedParams);
        var resolvedNamespaceParams = await _nsParamResolver.GetAllInputs(formatStrings);

        var paramDictionary = new Dictionary<string, ModuleResolvedInput>();

        // Add parameters from resolvedParams with priority
        foreach (var param in resolvedParams) paramDictionary[param.Name] = param;

        // Add parameters from resolvedNamespaceParams and check for conflicts
        foreach (var nsParam in resolvedNamespaceParams)
            if (paramDictionary.ContainsKey(nsParam.Name) && nsParam.IsFromNamespaceDefault)
                // Print a warning if there's a conflict
                _context.LogWarning($"Parameter with name '{nsParam.Name}' was set as a Namespace default, but was also set on Module directly. Using the value set on Module.");
            else
                paramDictionary[nsParam.Name] = nsParam;

        return paramDictionary.Values.ToList();
    }

    public async Task<Dictionary<string, string>> ResolveParameters()
    {
        _context.LogInformation("Now attempting to resolve parameters");

        var resolvedParams = await GetMergedParameters();
        CheckResolvedParamsForDuplicates(resolvedParams);

        return resolvedParams.ToDictionary(p => p.Name, p => p.ResolvedValue);
    }

    public async Task<Dictionary<string, string>> ResolveEnvVariables()
    {
        var resolvedParams = await GetMergedParameters(false);
        CheckResolvedParamsForDuplicates(resolvedParams);

        var resolvedValues = resolvedParams
            .ToDictionary(
                p => p.Name,
                p => p.ResolvedValue
            );
        return resolvedValues;
    }


    public void CheckResolvedParamsForDuplicates(
        List<ModuleResolvedInput> resolvedParams
    )
    {
        if (!resolvedParams.Any())
            return;

        // Group by 'Name' and find duplicates
        var duplicateGroups = resolvedParams
            .GroupBy(param => param.Name)
            .Where(group => group.Count() > 1)
            .ToList();

        if (!duplicateGroups.Any())
            return;

        // Log each duplicate and collect details for the exception
        var duplicateDetails = new List<string>();

        foreach (var group in duplicateGroups)
        {
            var duplicateInfo = group.Select(param =>
                $"- OriginalValue: {param.OriginalValue}, Source: {param.Source}"
            );

            var detail = $"Duplicate Name: {group.Key}\n{string.Join("\n", duplicateInfo)}";
            duplicateDetails.Add(detail);

            _context.LogError($"Duplicate parameter detected: \"{group.Key}\" with details: {detail}");
        }

        // Throw a single exception with all duplicate details
        throw new InvalidOperationException($"Duplicate parameters found:\n{string.Join("\n\n", duplicateDetails)}");
    }


    private string FormatValue(string value, InputType type)
    {
        return type == InputType.String
            ? JsonSerializer.Serialize(value)
            : value;
    }
}