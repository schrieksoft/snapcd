// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Generators;

// The one generator: emits every generated artifact except the OpenAPI document (which is a
// build-time product of SnapCd.OpenApi.Generator, not a program).
//
// CLI: dotnet run --project SnapCd.Generators -- [settings|mcp|all] [mcp args...]
//
//   settings   emit schemas/{runner,agent,server}.schema.json
//   mcp        regenerate the MCP surface (accepts the mcp args: --project, --out, --check)
//   all        both (default); trailing args go to mcp
//
// AppContext.BaseDirectory is the build output:
//   applications/snapcd/generators/SnapCd.Generators/bin/<Config>/net10.0/
// Walk up 5 levels (net10.0 → bin/<Config> → bin → project dir → generators → snapcd) to reach
// applications/snapcd/.
var snapcdRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

var command = args.Length > 0 && !args[0].StartsWith('-') ? args[0] : "all";
var commandArgs = args.Length > 0 && !args[0].StartsWith('-') ? args[1..] : args;

switch (command)
{
    case "settings":
        return SettingsSchemasCommand.Run(snapcdRoot);
    case "mcp":
        return await McpCommand.Run(commandArgs, snapcdRoot);
    case "all":
        var rc = SettingsSchemasCommand.Run(snapcdRoot);
        return Math.Max(rc, await McpCommand.Run(commandArgs, snapcdRoot));
    default:
        Console.Error.WriteLine($"unknown command: {command} (expected settings, mcp or all)");
        return 2;
}
