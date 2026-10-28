// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;

namespace SnapCd.Server.Core.Services.ParamResolver.Helpers;

/// <summary>
/// Resolves inputs from specific Outputs (FromOutput).
/// Generic over the input type to ensure type-safe resolution:
/// - ModuleParamFromOutput for Params (used in Plan)
/// - ModuleEnvVarFromOutput for EnvVars (used in Init)
/// </summary>
public class OutputParamResolver<TModuleInputFromOutput>
    where TModuleInputFromOutput : ModuleInputFromOutput
{
    private readonly OutputRepository _repository;
    private readonly CustomOutputMapper _outputMapper;
    private readonly SnapCdDbContext _dbContext;

    public OutputParamResolver(
        OutputRepository repository,
        CustomOutputMapper outputMapper,
        SnapCdDbContext dbContext)
    {
        _repository = repository;
        _outputMapper = outputMapper;
        _dbContext = dbContext;
    }

    public async Task<List<ModuleResolvedInput>> ListByModuleInputFromOutputs(Guid moduleId, Guid organizationId, bool formatStrings = true)
    {
        // 1. Validate stack scope - get requesting module's stack
        var requestingStackId = await ValidateAndGetModuleStackId(moduleId);

        // 2. Get all ModuleInputFromOutput entities of the specific type for this module
        var moduleInputFromOutputs = await GetModuleInputFromOutputs(moduleId);

        // 3. Validate that all referenced output modules are in the same stack
        await ValidateOutputModuleStackScope(moduleInputFromOutputs, requestingStackId);

        // 4. Get specific outputs based on output names and create resolved inputs
        return await GetSpecificOutputsResolved(moduleInputFromOutputs, organizationId, formatStrings);
    }

    private async Task<Guid> ValidateAndGetModuleStackId(Guid moduleId)
    {
        var stackId = await _dbContext.Modules
            .Where(m => m.Id == moduleId)
            .Select(m => m.Namespace.StackId)
            .FirstOrDefaultAsync();

        if (stackId == Guid.Empty) throw new InvalidStackReferenceException($"Module with ID {moduleId} not found");

        return stackId;
    }

    private async Task ValidateOutputModuleStackScope(
        IEnumerable<TModuleInputFromOutput> moduleInputs,
        Guid requestingStackId)
    {
        var outputModuleIds = moduleInputs.Select(mi => mi.OutputModuleId).Distinct().ToList();
        if (!outputModuleIds.Any()) return;

        var outputModuleStacks = await _dbContext.Modules
            .Where(m => outputModuleIds.Contains(m.Id))
            .Select(m => new { ModuleId = m.Id, m.Namespace.StackId })
            .ToListAsync();

        var crossStackReferences = outputModuleStacks
            .Where(oms => oms.StackId != requestingStackId)
            .ToList();

        if (crossStackReferences.Any())
        {
            var violatingModules = string.Join(", ", crossStackReferences.Select(r => r.ModuleId));
            throw new InvalidStackReferenceException(
                $"Cannot reference outputs from modules in different stacks. " +
                $"Output modules {violatingModules} are in different stacks.");
        }
    }

    private async Task<List<TModuleInputFromOutput>> GetModuleInputFromOutputs(Guid moduleId)
    {
        // Query only the specific entity type (Param or EnvVar)
        return await _dbContext.Set<TModuleInputFromOutput>()
            .Where(p => p.ModuleId == moduleId)
            .ToListAsync();
    }

    private async Task<List<ModuleResolvedInput>> GetSpecificOutputsResolved(
        List<TModuleInputFromOutput> moduleInputFromOutputs,
        Guid organizationId,
        bool formatStrings = true)
    {
        var result = new List<ModuleResolvedInput>();

        // Group module inputs by output module to optimize fetching
        var inputsByModule = moduleInputFromOutputs.GroupBy(mi => mi.OutputModuleId);

        foreach (var moduleGroup in inputsByModule)
        {
            var outputModuleId = moduleGroup.Key;

            // Get the latest OutputSet for the module
            var latestOutputSet = await _dbContext.OutputSets
                .Where(os => os.ModuleId == outputModuleId)
                .OrderByDescending(os => os.Timestamp)
                .FirstOrDefaultAsync();

            if (latestOutputSet == null) continue;

            // Get all outputs from this output set via repository
            var outputSetOutputs = await _repository.ListByOutputSetIds(new List<Guid> { latestOutputSet.Id }, organizationId);

            // Apply stack-aware secret store validation and mapping
            var mappedOutputs = await _outputMapper.MapOutputs(outputSetOutputs, organizationId);

            // Create a lookup for outputs by name
            var outputsByName = mappedOutputs.ToDictionary(o => o.Name, o => o);

            // Process each module input for this output module
            foreach (var moduleInput in moduleGroup)
                if (outputsByName.TryGetValue(moduleInput.OutputName, out var output))
                    result.Add(new ModuleResolvedInput
                    {
                        Name = moduleInput.Name,
                        OriginalValue = moduleInput.OutputModuleId.ToString(),
                        ResolvedValue = FormatValueBasedOnOutput(output, formatStrings),
                        Source = ModuleInputSource.ModuleOutput
                    });
        }

        return result;
    }

    private static string FormatValueBasedOnOutput(OutputReadDto output, bool formatStrings)
    {
        if (!formatStrings) return output.Value;

        switch (output.Type)
        {
            case "string":
                return JsonSerializer.Serialize(output.Value);
            case "bool":
                return output.Value.ToLower();
            default:
                return output.Value;
        }
    }
}
