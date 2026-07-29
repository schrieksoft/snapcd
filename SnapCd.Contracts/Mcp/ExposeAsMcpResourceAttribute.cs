// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Mcp;

/// <summary>
/// Marks a controller action as an MCP Resource. The codegen emits a matching
/// <c>[McpServerResource]</c> wrapper that JSON-serialises the controller's return DTO.
/// The resource's <c>[Description]</c> is the action's summary ([EndpointSummary] override,
/// else the <c>EndpointDocConvention</c> text) with <see cref="Instructions"/> appended when set.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ExposeAsMcpResourceAttribute : Attribute
{
    /// <summary>
    /// The URI template the agent uses to read this resource (RFC 6570). Required — typed as a
    /// literal string on every annotation; the codegen does not derive it from the HTTP route.
    /// Convention: <c>snapcd://orgs/{organizationId}/&lt;entity-plural&gt;/{entityId}/&lt;sub-path&gt;</c>
    /// </summary>
    public string? UriTemplate { get; init; }

    /// <summary>
    /// Override the default <c>{Entity.Singular}_{ActionMethodName}</c> snake_case name. Useful
    /// when the convention reads awkwardly (e.g. <c>module_job_logs</c> vs <c>job_logs</c>).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Override the default <c>application/json</c> mime type. Set for non-JSON resources
    /// (e.g. <c>text/plain</c> for raw log dumps, <c>application/yaml</c> for manifests).
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// Agent-facing usage guidance appended to the resource's <c>[Description]</c> after the
    /// endpoint summary. MCP-only — never surfaces in the OpenAPI document.
    /// </summary>
    public string? Instructions { get; init; }
}
