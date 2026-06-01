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
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;

namespace SnapCd.Server.Core.Services.ParamResolver.Helpers;

/// <summary>
/// Resolves inputs from OutputSets. This is only used for Params (not EnvVars)
/// since ModuleEnvVarFromOutputSet was removed - FromOutputSet only makes sense
/// for parameters that need to match Variables in VariableSet.
/// </summary>
public class OutputSetParamResolver
{
    private readonly OutputRepository _repository;
    private readonly CustomOutputMapper _outputMapper;
    private readonly SnapCdDbContext _dbContext;

    public OutputSetParamResolver(
        OutputRepository repository,
        CustomOutputMapper outputMapper,
        SnapCdDbContext dbContext)
    {
        _repository = repository;
        _outputMapper = outputMapper;
        _dbContext = dbContext;
    }

    public async Task<List<ModuleResolvedInput>> ListByModuleInputFromOutputSets(Guid moduleId, Guid organizationId, string engine, bool formatStrings = true)
    {
        // 1. Validate stack scope - get requesting module's stack
        var requestingStackId = await ValidateAndGetModuleStackId(moduleId);

        // 2. Get all ModuleParamFromOutputSet entities for this module
        var moduleInputFromOutputSets = await GetModuleInputFromOutputSets(moduleId);

        // 3. Validate that all referenced output modules are in the same stack
        await ValidateOutputModuleStackScope(moduleInputFromOutputSets, requestingStackId);

        // 4. Get all outputs from the referenced modules' output sets and create resolved inputs
        return await GetOutputsFromOutputSetsResolved(moduleInputFromOutputSets, moduleId, organizationId, engine, formatStrings);
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
        IEnumerable<IModuleInputFromOutputSet> moduleInputs,
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
                $"Cannot reference output sets from modules in different stacks. " +
                $"Output modules {violatingModules} are in different stacks.");
        }
    }

    private async Task<List<IModuleInputFromOutputSet>> GetModuleInputFromOutputSets(Guid moduleId)
    {
        // Only query ModuleParamFromOutputSets - EnvVar variant was removed
        var paramEntities = await _dbContext.ModuleParamFromOutputSets
            .Where(p => p.ModuleId == moduleId)
            .ToListAsync();

        return paramEntities.Cast<IModuleInputFromOutputSet>().ToList();
    }

    private async Task<List<ModuleResolvedInput>> GetOutputsFromOutputSetsResolved(
        List<IModuleInputFromOutputSet> moduleInputFromOutputSets,
        Guid moduleId,
        Guid organizationId,
        string engine,
        bool formatStrings = true)
    {
        var result = new List<ModuleResolvedInput>();

        var outputModuleIds = moduleInputFromOutputSets.Select(m => m.OutputModuleId).Distinct().ToList();
        if (!outputModuleIds.Any()) return result;

        HashSet<string>? variableNames = null;

        // Pulumi cannot produce VariableSets (variables are defined in code, not declarative files).
        // Skip VariableSet filtering entirely for Pulumi to avoid stale Terraform VariableSets
        // incorrectly filtering outputs after an engine change.
        if (engine != "pulumi")
        {
            var latestVariableSet = await _dbContext.VariableSets
                .Where(vs => vs.ModuleId == moduleId && vs.OrganizationId == organizationId)
                .OrderByDescending(vs => vs.Timestamp)
                .Include(vs => vs.Variables)
                .FirstOrDefaultAsync();

            variableNames = latestVariableSet?.Variables.Select(v => v.Name).ToHashSet();
        }

        // Get the latest OutputSet for each module
        var latestOutputSets = await _dbContext.OutputSets
            .Where(os => outputModuleIds.Contains(os.ModuleId))
            .GroupBy(os => os.ModuleId)
            .Select(g => g.OrderByDescending(os => os.Timestamp).First())
            .ToListAsync();

        var outputSetIds = latestOutputSets.Select(os => os.Id).ToList();

        // Get all outputs from these output sets
        var outputs = await _repository.ListByOutputSetIds(outputSetIds, organizationId);

        // Filter outputs by variable names if VariableSet exists
        if (variableNames != null)
            outputs = outputs.Where(o => variableNames.Contains(o.Name)).ToList();

        // Apply stack-aware secret store validation and mapping
        var mappedOutputs = await _outputMapper.MapOutputs(outputs, organizationId);

        // Create a lookup for outputs by OutputSetId
        var outputsBySetId = mappedOutputs.GroupBy(o => o.OutputSetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Create ModuleResolvedInput for each output
        foreach (var moduleInput in moduleInputFromOutputSets)
        {
            var outputSet = latestOutputSets.FirstOrDefault(os => os.ModuleId == moduleInput.OutputModuleId);
            if (outputSet == null) continue;

            if (outputsBySetId.TryGetValue(outputSet.Id, out var outputsForSet))
                foreach (var output in outputsForSet)
                    result.Add(new ModuleResolvedInput
                    {
                        Name = output.Name,
                        OriginalValue = moduleInput.OutputModuleId.ToString(),
                        ResolvedValue = FormatValueBasedOnOutput(output, formatStrings),
                        Source = ModuleInputSource.ModuleOutputSet
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
