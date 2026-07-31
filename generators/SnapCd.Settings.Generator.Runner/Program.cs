// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Nodes;
using SnapCd.Runner.Settings;
using SnapCd.Utils.Settings;

// Section-name → POCO type map. Keys must match the section names passed to Configure<T> in
// SnapCd.Runner/Program.cs — those are the strings operators write into appsettings.json.
var sectionTypes = new Dictionary<string, Type>
{
    ["Server"] = typeof(ServerSettings),
    ["WorkingDirectory"] = typeof(WorkingDirectorySettings),
    ["Runner"] = typeof(RunnerSettings),
    ["HooksPreapproval"] = typeof(HooksPreapprovalSettings),
    ["Engine"] = typeof(EngineSettings),
    ["JobLogStream"] = typeof(JobLogStreamSettings),
    ["SourceCache"] = typeof(SourceCacheSettings),
};

// Section-name → pre-baked schema fragment. Used for sections whose shape isn't owned by a
// snapcd-defined POCO — currently the standard .NET Logging shape.
var sectionFragments = new Dictionary<string, JsonNode>
{
    ["Logging"] = StandardSchemaFragments.Logging,
};

// Schemas live under applications/snapcd/schemas/<component>.schema.json. We resolve the path
// relative to the running binary so the generator works from any working directory (CI, IDE
// run-button, pre-commit hook, etc.).
//
// AppContext.BaseDirectory is the build output:
//   applications/snapcd/generators/SnapCd.Settings.Generator.Runner/bin/<Config>/net10.0/
// We walk up 5 levels (net10.0 → bin/<Config> → bin → project dir → generators → snapcd)
// to reach applications/snapcd/.
var binDir = AppContext.BaseDirectory;
var snapcdRoot = Path.GetFullPath(Path.Combine(binDir, "..", "..", "..", "..", ".."));
var outputPath = Path.Combine(snapcdRoot, "schemas", "runner.schema.json");

return SettingsSchemaCliRunner.Run(
    component: "runner",
    sectionTypes: sectionTypes,
    outputPath: outputPath,
    args: args,
    sectionFragments: sectionFragments);
