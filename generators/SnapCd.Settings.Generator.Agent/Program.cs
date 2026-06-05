// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Nodes;
using SnapCd.Agent.Configuration;
using SnapCd.Utils.Settings;

// Section-name → POCO type map. Keys must match the section names the Agent binds against in
// SnapCd.Agent/Program.cs (currently only the "Agent" section via AgentOptions.SectionName).
var sectionTypes = new Dictionary<string, Type>
{
    [ServerSettings.SectionName] = typeof(ServerSettings),
    [AgentOptions.SectionName] = typeof(AgentOptions),
};

// Section-name → pre-baked schema fragment. Standard .NET Logging shape lives in SnapCd.Utils
// and is shared with the Runner (and eventually the Server) generator.
var sectionFragments = new Dictionary<string, JsonNode>
{
    ["Logging"] = StandardSchemaFragments.Logging,
};

// Schemas live under applications/snapcd/schemas/<component>.schema.json. AppContext.BaseDirectory
// is the build output:
//   applications/snapcd/generators/SnapCd.Settings.Generator.Agent/bin/<Config>/net10.0/
// Walk up 5 levels (net10.0 → bin/<Config> → bin → project dir → generators → snapcd) to reach
// applications/snapcd/.
var binDir = AppContext.BaseDirectory;
var snapcdRoot = Path.GetFullPath(Path.Combine(binDir, "..", "..", "..", "..", ".."));
var outputPath = Path.Combine(snapcdRoot, "schemas", "agent.schema.json");

return SettingsSchemaCliRunner.Run(
    component: "agent",
    sectionTypes: sectionTypes,
    outputPath: outputPath,
    args: args,
    sectionFragments: sectionFragments);
