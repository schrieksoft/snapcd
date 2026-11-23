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
using SnapCd.Server.Core.Services.Nonsecured.ParamResolver.Helpers;

namespace SnapCd.Server.Core.Services.Nonsecured.ParamResolver;

public class NamespaceParamResolver
{
    private readonly ServerTaskContext _context;

    // Define the parameters with their corresponding values
    private readonly Dictionary<DefinitionInputType, string> _definitionParams;

    private readonly ILogger<NamespaceParamResolver> _logger;

    private readonly List<NamespaceInputFromLiteralReadDto> _fromLiteralNamespaceParams;
    private readonly List<NamespaceInputFromDefinitionReadDto> _fromDefinitionNamespaceParams;
    private readonly List<SelectedNamespaceSecret> _selectedNamespaceSecrets;

    private readonly SecretParamResolver _secretParamResolver;
    private readonly Guid _organizationId;


    public NamespaceParamResolver(
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

                _context.LogDebug($"Successfully resolved parameter \"{entry.Name}\" from source \"{NamespaceInputSource.Definition.ToString()}\" with value \"{entry.DefinitionName}\"");

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

                _context.LogDebug($"Successfully resolved parameter \"{entry.Name}\" from source \"{NamespaceInputSource.Literal.ToString()}\" with value \"{entry.LiteralValue}\"");

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

    /// <summary>Every Namespace input reaches the Module under its own name. A Module's own input of that name takes precedence later.</summary>
    public Task<List<ModuleResolvedInput>> FilterInputs(List<NamespaceResolvedInput> nsResolvedParams)
    {
        var results = nsResolvedParams
            .Select(p => new ModuleResolvedInput
            {
                Name = p.Name,
                OriginalValue = "<not set>",
                ResolvedValue = p.ResolvedValue,
                IsFromNamespaceDefault = true,
                Source = ModuleInputSource.NamespaceParam
            })
            .ToList();

        return Task.FromResult(results);
    }

    private string FormatValue(string value, InputType type)
    {
        return type == InputType.String
            ? JsonSerializer.Serialize(value)
            : value;
    }
}