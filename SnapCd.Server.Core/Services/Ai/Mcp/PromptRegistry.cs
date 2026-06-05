// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Protocol;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SnapCd.Server.Core.Services.Ai.Mcp;

public sealed class PromptRegistry
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\}\}", RegexOptions.Compiled);

    private readonly IReadOnlyDictionary<string, SkillEntry> _skills;

    public PromptRegistry(ILogger<PromptRegistry> logger)
    {
        _skills = LoadSkills(logger);
    }

    public IReadOnlyList<Prompt> ListPrompts() =>
        _skills.Values.Select(s => s.Prompt).ToList();

    public bool TryGet(string name, IDictionary<string, JsonElement>? arguments, out GetPromptResult? result, out string? error)
    {
        if (!_skills.TryGetValue(name, out var skill))
        {
            result = null;
            error = $"Unknown prompt: {name}";
            return false;
        }

        var args = (arguments ?? new Dictionary<string, JsonElement>())
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ValueKind == JsonValueKind.String ? kv.Value.GetString() : kv.Value.GetRawText(),
                StringComparer.Ordinal);

        var missing = skill.Manifest.Arguments
            .Where(a => a.Required && (!args.TryGetValue(a.Name, out var v) || string.IsNullOrEmpty(v)))
            .Select(a => a.Name)
            .ToList();

        if (missing.Count > 0)
        {
            result = null;
            error = $"Missing required argument(s): {string.Join(", ", missing)}";
            return false;
        }

        var rendered = PlaceholderRegex.Replace(skill.Body, m =>
        {
            var key = m.Groups["name"].Value;
            return args.TryGetValue(key, out var v) && v is not null ? v : string.Empty;
        });

        result = new GetPromptResult
        {
            Description = skill.Manifest.Description,
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = rendered }
                }
            ]
        };
        error = null;
        return true;
    }

    private static Dictionary<string, SkillEntry> LoadSkills(ILogger logger)
    {
        var assembly = typeof(PromptRegistry).Assembly;
        const string manifestResource = "SnapCd.Server.Core.AI.Skills.manifest.yaml";

        using var manifestStream = assembly.GetManifestResourceStream(manifestResource)
            ?? throw new InvalidOperationException(
                $"Embedded skills manifest '{manifestResource}' not found. " +
                "Ensure AI/Skills/manifest.yaml is included as EmbeddedResource in the project.");

        using var manifestReader = new StreamReader(manifestStream);
        var manifestYaml = manifestReader.ReadToEnd();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var manifest = deserializer.Deserialize<SkillsManifest>(manifestYaml)
            ?? throw new InvalidOperationException("Skills manifest deserialised to null.");

        var skills = new Dictionary<string, SkillEntry>(StringComparer.Ordinal);

        foreach (var skill in manifest.Skills)
        {
            var bodyResource = $"SnapCd.Server.Core.AI.Skills.{skill.File}";
            using var bodyStream = assembly.GetManifestResourceStream(bodyResource);
            if (bodyStream is null)
            {
                logger.LogWarning(
                    "Skill '{Name}' references body file '{File}' but embedded resource '{Resource}' was not found; skipping.",
                    skill.Name, skill.File, bodyResource);
                continue;
            }

            using var bodyReader = new StreamReader(bodyStream);
            var body = bodyReader.ReadToEnd();

            var prompt = new Prompt
            {
                Name = skill.Name,
                Description = skill.Description,
                Arguments = skill.Arguments
                    .Select(a => new PromptArgument
                    {
                        Name = a.Name,
                        Description = a.Description,
                        Required = a.Required
                    })
                    .ToList()
            };

            skills[skill.Name] = new SkillEntry(skill, body, prompt);
        }

        logger.LogInformation("Loaded {Count} MCP prompts (skills) from embedded manifest.", skills.Count);
        return skills;
    }

    private sealed record SkillEntry(SkillManifest Manifest, string Body, Prompt Prompt);

    private sealed class SkillsManifest
    {
        public List<SkillManifest> Skills { get; set; } = [];
    }

    private sealed class SkillManifest
    {
        public string Name { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<SkillArgumentManifest> Arguments { get; set; } = [];
    }

    private sealed class SkillArgumentManifest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Required { get; set; }
    }
}
