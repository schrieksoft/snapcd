// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SnapCd.Contracts.Dto;
using SnapCd.Contracts.Dto.Variables;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Runner.Exceptions;

namespace SnapCd.Runner.Services;

/// <summary>
/// Service for discovering Terraform variable definitions from .tf and .tf.json files.
/// Supports native JSON parsing for .tf.json and regex-based parsing for .tf files.
/// </summary>
public class TerraformVariableDiscoveryService : IVariableDiscoveryService
{
    /// <summary>
    /// Discovers all Terraform variables in a directory by scanning .tf and .tf.json files.
    /// </summary>
    /// <param name="directoryPath">Path to the directory containing Terraform files</param>
    /// <param name="extraFileNames">Set of filenames that are extra files (to set FromExtraFile flag)</param>
    /// <param name="throwOnError">If false, logs warnings and continues on parse errors</param>
    /// <returns>List of discovered variables as InputDto</returns>
    public async Task<List<VariableCreateDto>> DiscoverVariablesAsync(
        string directoryPath,
        ISet<string>? extraFileNames = null,
        bool throwOnError = false)
    {
        if (!Directory.Exists(directoryPath)) throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var allVariables = new List<VariableCreateDto>();

        // Scan *.tf files
        var tfFiles = Directory.GetFiles(directoryPath, "*.tf", SearchOption.TopDirectoryOnly);
        foreach (var file in tfFiles)
            try
            {
                var vars = await ParseFileAsync(file);
                var isExtraFile = IsExtraFile(file, extraFileNames);
                foreach (var v in vars) v.FromExtraFile = isExtraFile;
                allVariables.AddRange(vars);
            }
            catch (VariableParseException ex)
            {
                if (throwOnError) throw;

                // Log warning but continue (in production, use ILogger)
                Console.WriteLine($"Warning: {ex.Message}");
            }

        // Scan *.tf.json files
        var tfJsonFiles = Directory.GetFiles(directoryPath, "*.tf.json", SearchOption.TopDirectoryOnly);
        foreach (var file in tfJsonFiles)
            try
            {
                var vars = await ParseFileAsync(file);
                var isExtraFile = IsExtraFile(file, extraFileNames);
                foreach (var v in vars) v.FromExtraFile = isExtraFile;
                allVariables.AddRange(vars);
            }
            catch (Exception ex)
            {
                if (throwOnError) throw new VariableParseException($"Failed to parse {file}", ex, file);
                Console.WriteLine($"Warning: Failed to parse {file}: {ex.Message}");
            }

        return allVariables;
    }

    private static bool IsExtraFile(string filePath, ISet<string>? extraFileNames)
    {
        if (extraFileNames == null || extraFileNames.Count == 0) return false;
        var fileName = Path.GetFileName(filePath);
        var isExtraFile = extraFileNames.Contains(fileName);
        return isExtraFile;
    }

    /// <summary>
    /// Discovers output definitions from .tf files and returns a mapping of output name to FromExtraFile flag.
    /// </summary>
    /// <param name="directoryPath">Path to the directory containing Terraform files</param>
    /// <param name="extraFileNames">Set of filenames that are extra files</param>
    /// <returns>Dictionary mapping output name to whether it's from an extra file</returns>
    public async Task<Dictionary<string, bool>> DiscoverOutputSourcesAsync(
        string directoryPath,
        ISet<string>? extraFileNames = null)
    {
        var outputSources = new Dictionary<string, bool>();

        if (!Directory.Exists(directoryPath)) return outputSources;

        // Scan *.tf files for output blocks
        var tfFiles = Directory.GetFiles(directoryPath, "*.tf", SearchOption.TopDirectoryOnly);
        foreach (var file in tfFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var isExtraFile = IsExtraFile(file, extraFileNames);
                var outputNames = ParseOutputNames(content);
                foreach (var name in outputNames)
                {
                    outputSources[name] = isExtraFile;
                }
            }
            catch
            {
                // Silently continue on parse errors
            }
        }

        // Scan *.tf.json files for output blocks
        var tfJsonFiles = Directory.GetFiles(directoryPath, "*.tf.json", SearchOption.TopDirectoryOnly);
        foreach (var file in tfJsonFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var isExtraFile = IsExtraFile(file, extraFileNames);
                var outputNames = ParseOutputNamesFromJson(content);
                foreach (var name in outputNames)
                {
                    outputSources[name] = isExtraFile;
                }
            }
            catch
            {
                // Silently continue on parse errors
            }
        }

        return outputSources;
    }

    private static List<string> ParseOutputNames(string content)
    {
        var names = new List<string>();

        // Remove comments
        content = Regex.Replace(content, @"(#|//).*$", "", RegexOptions.Multiline);
        content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Match output blocks: output "name" { or output name {
        var pattern = @"output\s+(?:""([^""]+)""|(\w+))\s*\{";
        var regex = new Regex(pattern, RegexOptions.Singleline);
        var matches = regex.Matches(content);

        foreach (Match match in matches)
        {
            var name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            names.Add(name);
        }

        return names;
    }

    private static List<string> ParseOutputNamesFromJson(string content)
    {
        var names = new List<string>();

        try
        {
            var json = JObject.Parse(content);
            if (json["output"] is JObject outputObj)
            {
                foreach (var prop in outputObj.Properties())
                {
                    names.Add(prop.Name);
                }
            }
        }
        catch
        {
            // Invalid JSON, return empty list
        }

        return names;
    }

    /// <summary>
    /// Parses a single Terraform file and extracts variable definitions.
    /// </summary>
    /// <param name="filePath">Path to .tf or .tf.json file</param>
    /// <returns>List of variables found in the file as InputDto</returns>
    public async Task<List<VariableReadDto>> ParseFileAsync(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"File not found: {filePath}", filePath);

        if (filePath.EndsWith(".tf.json", StringComparison.OrdinalIgnoreCase))
            return await ParseTfJsonFileAsync(filePath);
        else if (filePath.EndsWith(".tf", StringComparison.OrdinalIgnoreCase)) return await ParseTfFileAsync(filePath);

        throw new NotSupportedException($"Unsupported file type: {filePath}. Only .tf and .tf.json files are supported.");
    }

    #region .tf.json Parsing (Native JSON)

    /// <summary>
    /// Parses a .tf.json file (native JSON format).
    /// Structure: { "variable": { "var_name": { "type": "string", "description": "...", ... } } }
    /// </summary>
    private async Task<List<VariableReadDto>> ParseTfJsonFileAsync(string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var json = JObject.Parse(content);

            var variables = new List<VariableReadDto>();

            // .tf.json structure: variable is an object, not array
            if (json["variable"] is JObject variableObj)
                foreach (var prop in variableObj.Properties())
                {
                    var varName = prop.Name;
                    var varDef = prop.Value as JObject;

                    // Parse default to check if it exists (but don't store it)
                    var defaultValue = ParseDefaultValue(varDef?["default"]);
                    var hasDefault = defaultValue != null;

                    // Calculate Nullable: explicit nullable > has default > false
                    var explicitNullable = varDef?["nullable"]?.Value<bool>();
                    var nullable = explicitNullable ?? (hasDefault ? true : false);

                    variables.Add(new VariableReadDto
                    {
                        Name = varName,
                        Type = varDef?["type"]?.ToString(),
                        Description = varDef?["description"]?.ToString(),
                        Sensitive = varDef?["sensitive"]?.Value<bool>() ?? false,
                        Nullable = nullable
                    });
                }

            return variables;
        }
        catch (Exception ex)
        {
            throw new VariableParseException($"Failed to parse .tf.json file: {filePath}", ex, filePath);
        }
    }

    private object? ParseDefaultValue(JToken? token)
    {
        if (token == null) return null;

        return token.Type switch
        {
            JTokenType.String => token.Value<string>(),
            JTokenType.Integer => token.Value<long>(),
            JTokenType.Float => token.Value<double>(),
            JTokenType.Boolean => token.Value<bool>(),
            JTokenType.Array => token.ToObject<List<object>>(),
            JTokenType.Object => token.ToObject<Dictionary<string, object>>(),
            JTokenType.Null => null,
            _ => token.ToString()
        };
    }

    #endregion

    #region .tf File Parsing (HCL - Regex Based)

    /// <summary>
    /// Parses a .tf file (HCL format) using regex-based extraction.
    /// Handles most common variable block patterns.
    /// </summary>
    private async Task<List<VariableReadDto>> ParseTfFileAsync(string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            return ParseHclVariableBlocks(content, filePath);
        }
        catch (Exception ex)
        {
            throw new VariableParseException($"Failed to parse .tf file: {filePath}", ex, filePath);
        }
    }

    private List<VariableReadDto> ParseHclVariableBlocks(string content, string filePath)
    {
        var variables = new List<VariableReadDto>();

        // Remove single-line comments (// and #)
        content = Regex.Replace(content, @"(#|//).*$", "", RegexOptions.Multiline);

        // Remove multi-line comments (/* ... */)
        content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Regex pattern to match: variable "name" { ... } or variable name { ... }
        // Variable names can be quoted or unquoted in HCL
        // Handles nested braces in the block content
        var pattern = @"variable\s+(?:""([^""]+)""|(\w+))\s*\{((?:[^{}]|\{(?:[^{}]|\{[^{}]*\})*\})*)\}";
        var regex = new Regex(pattern, RegexOptions.Singleline);

        var matches = regex.Matches(content);

        foreach (Match match in matches)
        {
            // Group 1 = quoted name, Group 2 = unquoted name, Group 3 = block content
            var variableName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            var blockContent = match.Groups[3].Value;

            try
            {
                var variable = ParseVariableBlock(blockContent, variableName, filePath);
                variables.Add(variable);
            }
            catch (Exception ex)
            {
                throw new VariableParseException(
                    $"Failed to parse variable '{variableName}' in {filePath}: {ex.Message}",
                    ex,
                    filePath);
            }
        }

        return variables;
    }

    private VariableReadDto ParseVariableBlock(string blockContent, string variableName, string filePath)
    {
        var variable = new VariableReadDto
        {
            Name = variableName
        };

        // Parse type
        var typeMatch = Regex.Match(blockContent, @"type\s*=\s*([^\n]+)");
        if (typeMatch.Success) variable.Type = CleanValue(typeMatch.Groups[1].Value);

        // Parse description
        var descMatch = Regex.Match(blockContent, @"description\s*=\s*""([^""]*)""");
        if (descMatch.Success) variable.Description = descMatch.Groups[1].Value;

        // Parse sensitive
        var sensitiveMatch = Regex.Match(blockContent, @"sensitive\s*=\s*(true|false)");
        if (sensitiveMatch.Success) variable.Sensitive = bool.Parse(sensitiveMatch.Groups[1].Value);

        // Parse default to check if it exists (but don't store it)
        var defaultValue = ParseDefaultFromHcl(blockContent);
        var hasDefault = defaultValue != null;

        // Parse nullable attribute
        var nullableMatch = Regex.Match(blockContent, @"nullable\s*=\s*(true|false)");
        bool? explicitNullable = nullableMatch.Success
            ? bool.Parse(nullableMatch.Groups[1].Value)
            : null;

        // Calculate Nullable: explicit nullable > has default > false
        variable.Nullable = explicitNullable ?? (hasDefault ? true : false);

        return variable;
    }

    private string CleanValue(string value)
    {
        // Remove trailing comments and whitespace
        value = Regex.Replace(value, @"#.*$", "").Trim();
        value = Regex.Replace(value, @"//.*$", "").Trim();
        return value;
    }

    private object? ParseDefaultFromHcl(string blockContent)
    {
        // Match default = ... up to the next attribute or closing brace
        var defaultMatch = Regex.Match(blockContent, @"default\s*=\s*(.+?)(?=\n\s*\w+\s*=|\n\s*\}|$)", RegexOptions.Singleline);
        if (!defaultMatch.Success) return null;

        var defaultValue = defaultMatch.Groups[1].Value.Trim();

        // Handle different default value types

        // Null
        if (defaultValue == "null") return null;

        // Boolean
        if (defaultValue == "true") return true;
        if (defaultValue == "false") return false;

        // String with quotes
        var stringMatch = Regex.Match(defaultValue, @"^""(.*)""$", RegexOptions.Singleline);
        if (stringMatch.Success) return stringMatch.Groups[1].Value;

        // Number
        if (double.TryParse(defaultValue, out var number)) return number;

        // List: [...]
        if (defaultValue.StartsWith("[") && defaultValue.Contains("]"))
        {
            var listContent = Regex.Match(defaultValue, @"\[(.*?)\]", RegexOptions.Singleline);
            if (listContent.Success) return ParseHclList(listContent.Groups[1].Value);
        }

        // Map/Object: {...}
        if (defaultValue.StartsWith("{") && defaultValue.Contains("}"))
        {
            var mapContent = Regex.Match(defaultValue, @"\{(.*?)\}", RegexOptions.Singleline);
            if (mapContent.Success) return ParseHclMap(mapContent.Groups[1].Value);
        }

        // If we can't parse it, return as string (for complex types like object({...}))
        return defaultValue;
    }

    private List<object> ParseHclList(string listContent)
    {
        var items = new List<object>();
        if (string.IsNullOrWhiteSpace(listContent)) return items;

        // Simple comma-separated list parser
        // Handle quoted strings and numbers
        var parts = SplitRespectingQuotes(listContent, ',');

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // String
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
                items.Add(trimmed.Trim('"'));
            // Boolean
            else if (trimmed == "true") items.Add(true);
            else if (trimmed == "false") items.Add(false);
            // Number
            else if (double.TryParse(trimmed, out var num))
                items.Add(num);
            // Default: string
            else
                items.Add(trimmed);
        }

        return items;
    }

    private Dictionary<string, object> ParseHclMap(string mapContent)
    {
        var map = new Dictionary<string, object>();
        if (string.IsNullOrWhiteSpace(mapContent)) return map;

        // Match key = value pairs
        // Key can be quoted or unquoted
        var kvPattern = @"(?:""([^""]+)""|(\w+))\s*=\s*([^,\n}]+)";
        var matches = Regex.Matches(mapContent, kvPattern);

        foreach (Match match in matches)
        {
            var key = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            var value = match.Groups[3].Value.Trim();

            // Parse value
            if (value.StartsWith("\"") && value.EndsWith("\""))
                map[key] = value.Trim('"');
            else if (value == "true") map[key] = true;
            else if (value == "false") map[key] = false;
            else if (double.TryParse(value, out var num))
                map[key] = num;
            else
                map[key] = value;
        }

        return map;
    }

    private List<string> SplitRespectingQuotes(string input, char delimiter)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in input)
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                current.Append(ch);
            }
            else if (ch == delimiter && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }

        if (current.Length > 0) result.Add(current.ToString());

        return result;
    }

    #endregion

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