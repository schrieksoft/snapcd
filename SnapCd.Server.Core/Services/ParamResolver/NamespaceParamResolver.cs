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
using SnapCd.Server.Core.Services.ParamResolver.Helpers;

namespace SnapCd.Server.Core.Services.ParamResolver;

public class NamespaceParamResolver
{
    private readonly ServerTaskContext _context;

    // Define the parameters with their corresponding values
    private readonly Dictionary<DefinitionInputType, string> _definitionParams;

    private readonly ILogger<NamespaceParamResolver> _logger;

    private readonly List<NamespaceInputFromLiteralReadDto> _fromLiteralNamespaceParams;
    private readonly List<NamespaceInputFromDefinitionReadDto> _fromDefinitionNamespaceParams;
    private readonly List<ModuleInputFromNamespaceReadDto> _fromNamespaceParams; // use this for filtering
    private readonly List<SelectedNamespaceSecret> _selectedNamespaceSecrets;

    private readonly SecretParamResolver _secretParamResolver;
    private readonly Guid _organizationId;


    public NamespaceParamResolver(
        List<ModuleInputFromNamespaceReadDto> fromNamespaceParams,
        List<NamespaceInputFromLiteralReadDto> fromLiteralNamespaceParams,
        List<NamespaceInputFromDefinitionReadDto> fromDefinitionNamespaceParams,
        SecretParamResolver secretParamResolver,
        ILogger<NamespaceParamResolver> logger,
        ServerTaskContext context,
        Dictionary<DefinitionInputType, string> definitionParams,
        List<SelectedNamespaceSecret> selectedNamespaceSecrets,
        Guid organizationId
    )
    {
        _fromLiteralNamespaceParams = fromLiteralNamespaceParams;
        _fromDefinitionNamespaceParams = fromDefinitionNamespaceParams;
        _fromNamespaceParams = fromNamespaceParams;
        _logger = logger;
        _context = context;
        _definitionParams = definitionParams;
        _secretParamResolver = secretParamResolver;
        _selectedNamespaceSecrets = selectedNamespaceSecrets;
        _organizationId = organizationId;
    }

    private async Task<List<NamespaceResolvedInput>> GetNamespaceSecretInputs(bool formatStrings = true)
    {
        var result = new List<NamespaceResolvedInput>();

        // Call service directly instead of HTTP client
        foreach (var discriminator in SecretDiscriminatorConstants.NamespaceSecretDiscriminators)
        {
            var secrets = _selectedNamespaceSecrets
                .Where(x => x.Discriminator == discriminator)
                .ToList();

            if (secrets == null || secrets.Count == 0)
                continue;

            var mappedSecrets = await _secretParamResolver.ListRemoteByIds(
                secrets
                    .Where(x => x.SecretId.HasValue)
                    .Select(x => x.SecretId!.Value)
                    .ToList(),
                _organizationId);

            foreach (var secret in secrets)
            {
                var value = mappedSecrets.FirstOrDefault(x => x.Id == secret.SecretId)?.Value ?? string.Empty;
                if (formatStrings)
                    value = FormatValue(value, secret.Type);

                var resolvedInput = new NamespaceResolvedInput
                {
                    Id = secret.InputId,
                    Name = secret.InputName,
                    ResolvedValue = value,
                    UsageMode = secret.UsageMode,
                    Source = NamespaceInputSource.NamespaceSecret,
                    OriginalValue = secret.SecretName ?? string.Empty
                };

                result.Add(resolvedInput);
            }
        }

        return result.ToList();
    }


    private async Task<List<NamespaceResolvedInput>> GetDefinitionInputs(bool formatStrings = true)
    {
        var exceptions = new List<Exception>();

        var tasks = _fromDefinitionNamespaceParams.Select(entry =>
        {
            try
            {
                string value;

                try
                {
                    value = _definitionParams[entry.DefinitionName];
                }
                catch (KeyNotFoundException)
                {
                    var validSources = string.Join(", ", _definitionParams.Keys);
                    throw new KeyNotFoundException(
                        $"The key '{entry.DefinitionName}' is not a valid \"Definition\" parameter source. Valid sources are: {validSources}");
                }

                if (formatStrings)
                    value = FormatValue(value, InputType.String);

                _context.LogInformation($"Successfully resolved parameter \"{entry.Name}\" from source \"{NamespaceInputSource.Definition.ToString()}\" with value \"{entry.DefinitionName}\"");

                return Task.FromResult(new NamespaceResolvedInput
                {
                    Id = entry.Id,
                    Name = entry.Name,
                    OriginalValue = entry.DefinitionName.ToString(),
                    ResolvedValue = value,
                    UsageMode = entry.UsageMode,
                    Source = NamespaceInputSource.Definition
                });
            }
            catch (Exception ex)
            {
                exceptions.Add(new Exception($"Error resolving definition parameter {entry.Id}", ex));
                _context.LogError($"Failed to resolve parameter \"{entry.Name}\" from source \"{NamespaceInputSource.Definition.ToString()}\" with value \"{entry.DefinitionName}\"");
                return Task.FromResult(new NamespaceResolvedInput { Id = entry.Id });
            }
        });

        var results = (await Task.WhenAll(tasks)).ToList();

        if (exceptions.Any())
            throw new AggregateException("Errors occurred while resolving definition parameters.", exceptions);

        return results;
    }

    private async Task<List<NamespaceResolvedInput>> GetLiteralInputs(bool formatStrings = true)
    {
        var exceptions = new List<Exception>();

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        var tasks = _fromLiteralNamespaceParams.Select(async entry =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            try
            {
                var value = entry.LiteralValue;
                if (formatStrings)
                    value = FormatValue(entry.LiteralValue, entry.Type);

                _context.LogInformation($"Successfully resolved parameter \"{entry.Name}\" from source \"{NamespaceInputSource.Literal.ToString()}\" with value \"{entry.LiteralValue}\"");

                return new NamespaceResolvedInput
                {
                    Id = entry.Id,
                    Name = entry.Name,
                    OriginalValue = entry.LiteralValue,
                    ResolvedValue = value,
                    UsageMode = entry.UsageMode,
                    Source = NamespaceInputSource.Literal
                };
            }
            catch (Exception ex)
            {
                exceptions.Add(new Exception($"Error resolving literal parameter {entry.Name}", ex));
                _context.LogError($"Failed to resolve parameter \"{entry.Name}\" from source \"{NamespaceInputSource.Literal.ToString()}\" with value \"{entry.LiteralValue}\"");
                return null;
            }
        });

        var results = (await Task.WhenAll(tasks)).Where(p => p != null).Cast<NamespaceResolvedInput>().ToList();

        if (exceptions.Any())
            throw new AggregateException("Errors occurred while resolving literal parameters.", exceptions);

        return results;
    }


    public async Task<List<ModuleResolvedInput>> GetAllInputs(bool formatStrings = true)
    {
        var exceptions = new List<Exception>();
        var nsParams = new List<NamespaceResolvedInput>();
        var result = new List<ModuleResolvedInput>();

        try
        {
            nsParams.AddRange(await GetNamespaceSecretInputs(formatStrings));
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.InnerExceptions);
        }

        try
        {
            nsParams.AddRange(await GetLiteralInputs(formatStrings));
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.InnerExceptions);
        }

        try
        {
            nsParams.AddRange(await GetDefinitionInputs(formatStrings));
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.InnerExceptions);
        }

        try
        {
            result = await FilterInputs(nsParams);
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.InnerExceptions);
        }


        if (exceptions.Any()) throw new AggregateException("Errors occurred while getting all parameters.", exceptions);

        return result;
    }

    public Task<List<ModuleResolvedInput>> FilterInputs(List<NamespaceResolvedInput> nsResolvedParams)
    {
        var results = new List<ModuleResolvedInput>();

        var defaults = nsResolvedParams
            .Where(p => p.UsageMode == NamespaceInputUsageMode.UseByDefault);

        // Add all defaults. Here we have to create a ResolvedParam() (i.e. a module-level resolved param) based on default namespace params
        foreach (var defaultParam in defaults)
            results.Add(new ModuleResolvedInput
            {
                Name = defaultParam.Name,
                OriginalValue = "<not set>",
                ResolvedValue = defaultParam.ResolvedValue,
                IsFromNamespaceDefault = true,
                Source = ModuleInputSource.NamespaceParam
            });


        // Filter the rest based on whether they were included in list of params
        // Here we have to create a ResolvedParam based on the module level param. We need to use its name for example, but the
        // value we get from the NamespaceResolvedParam. The source is implied and will always be ModuleParamSource.NamespaceParam.
        var exceptions = new List<Exception>();

        foreach (var p in _fromNamespaceParams)
            try
            {
                var nsParam = nsResolvedParams
                    .FirstOrDefault(nsp =>
                        nsp.Id == p.NamespaceInputId); // The "Value" field of a ModuleParam of source type "ModuleParamSource.NamespaceParam" indicates the name of the NamespaceParam to fetch

                if (nsParam == null)
                    throw new InvalidOperationException(
                        $"NamespaceParam with Id '{p.NamespaceInputId}' not found.");

                results.Add(new ModuleResolvedInput
                {
                    Name = p.Name,
                    OriginalValue = p.NamespaceInputId.ToString(),
                    ResolvedValue = nsParam.ResolvedValue,
                    IsFromNamespaceDefault = false,
                    Source = ModuleInputSource.NamespaceParam
                });
            }
            catch (Exception ex)
            {
                exceptions.Add(new Exception($"Error processing param '{p.Name}': {ex.Message}", ex));
            }

        // Throw all collected exceptions at the end
        if (exceptions.Any()) throw new AggregateException("Errors occurred while resolving parameters.", exceptions);

        return Task.FromResult(results);
    }

    private string FormatValue(string value, InputType type)
    {
        return type == InputType.String
            ? JsonSerializer.Serialize(value)
            : value;
    }
}