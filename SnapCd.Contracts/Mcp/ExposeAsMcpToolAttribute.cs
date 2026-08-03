// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Mcp;

/// <summary>
/// Marks a controller action as visible to MCP-connected agents. The codegen (see
/// <c>generators/SnapCd.Generators</c>, <c>mcp</c> command) emits a matching
/// <c>[McpServerTool]</c> wrapper into the committed MCP surface.
/// The tool's <c>[Description]</c> is the action's summary ([EndpointSummary] override, else
/// the <c>EndpointDocConvention</c> text) with <see cref="Instructions"/> appended when set.
/// Opt-in by design — unannotated actions are invisible to the agent.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ExposeAsMcpToolAttribute : Attribute
{
    /// <summary>
    /// Override the snake_case-derived tool name. Default convention:
    /// <c>{McpEntity.Plural ?? ControllerNameWithoutSuffix}_{ActionName}</c> in snake_case.
    /// Example: <c>AgentController.List</c> with <c>[McpEntity(Plural = "Agents")]</c> → <c>agents_list</c>.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Agent-facing usage guidance appended to the tool's <c>[Description]</c> after the
    /// endpoint summary. MCP-only — never surfaces in the OpenAPI document.
    /// </summary>
    public string? Instructions { get; init; }
}
