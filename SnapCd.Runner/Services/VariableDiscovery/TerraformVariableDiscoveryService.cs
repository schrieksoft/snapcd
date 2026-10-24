// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SnapCd.Contracts.Dto.Variables;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Runner.Exceptions;
using SnapCd.Runner.Services.ModuleSourceRefresher;

namespace SnapCd.Runner.Services;

/// <summary>
/// Discovers a Terraform/OpenTofu module's variable and output declarations by parsing its configuration with snapcd-inspect.
/// </summary>
public class TerraformVariableDiscoveryService : IVariableDiscoveryService
{
    private readonly ILogger<TerraformVariableDiscoveryService> _logger;
    private readonly SnapCdInspect _inspect;

    public TerraformVariableDiscoveryService(ILogger<TerraformVariableDiscoveryService> logger, SnapCdInspect inspect)
    {
        _logger = logger;
        _inspect = inspect;
    }

    /// <summary>Discovers the module's variables; invalid configuration, including a duplicate declaration, throws.</summary>
    public Task<List<VariableCreateDto>> DiscoverVariablesAsync(string directoryPath, ISet<string>? extraFileNames = null)
    {
        if (!Directory.Exists(directoryPath)) throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        SnapCdInspect.InspectedModule module;
        try
        {
            module = _inspect.InspectVariables(directoryPath);
        }
        catch (Exception ex)
        {
            throw new VariableParseException($"Could not read the module's variable declarations: {ex.Message}", ex);
        }

        return Task.FromResult(module.Variables
            .Select(v => new VariableCreateDto
            {
                Name = v.Name,
                Type = v.Type,
                Description = v.Description,
                Sensitive = v.Sensitive,
                Nullable = v.Nullable,
                FromExtraFile = extraFileNames?.Contains(v.File) == true
            })
            .ToList());
    }

    /// <summary>Maps each declared output to whether it comes from an extra file; unreadable configuration yields none.</summary>
    public Task<Dictionary<string, bool>> DiscoverOutputSourcesAsync(string directoryPath, ISet<string>? extraFileNames = null)
    {
        if (!Directory.Exists(directoryPath)) return Task.FromResult(new Dictionary<string, bool>());

        try
        {
            return Task.FromResult(_inspect.InspectVariables(directoryPath).Outputs
                .ToDictionary(o => o.Name, o => extraFileNames?.Contains(o.File) == true));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read output declarations in {Directory}", directoryPath);
            return Task.FromResult(new Dictionary<string, bool>());
        }
    }

    /// <summary>
    /// Creates an VariableSetDto by discovering all Terraform variables in a directory.
    /// </summary>
    /// <param name="directoryPath">Path to the directory containing Terraform files</param>
    /// <param name="moduleId">The module ID for the VariableSet</param>
    /// <param name="extraFileNames">Set of filenames that are extra files (to set FromExtraFile flag)</param>
    /// <returns>VariableSetDto containing discovered variables, or null if no variables found</returns>
    public async Task<VariableSetCreateDto?> CreateVariableSet(
        string directoryPath,
        Guid moduleId,
        ISet<string>? extraFileNames = null)
    {
        // Discover all variables in the directory
        var discoveredInputs = await DiscoverVariablesAsync(directoryPath, extraFileNames);

        // If no variables found, return null
        if (discoveredInputs.Count == 0) return null;

        // Serialize the inputs to JSON for checksum calculation
        var inputsJson = JsonConvert.SerializeObject(discoveredInputs, Formatting.None);

        // Create the VariableSetDto
        var variableSet = new VariableSetCreateDto
        {
            ModuleId = moduleId,
            Checksum = CalculateChecksum(inputsJson),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Variables = discoveredInputs
        };

        return variableSet;
    }

    private string CalculateChecksum(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(inputBytes);

            // Convert the byte array to a hexadecimal string
            var sb = new StringBuilder();
            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }
}