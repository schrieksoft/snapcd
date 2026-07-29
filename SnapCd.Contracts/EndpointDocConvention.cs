// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts;

/// <summary>
/// Convention-derived endpoint documentation, shared by the OpenAPI summary transformer and the
/// MCP surface codegen. An <c>[EndpointSummary]</c> attribute on the action overrides the
/// convention in both consumers.
/// </summary>
public static class EndpointDocConvention
{
    public static string? Summary(string actionName, string singular, string plural)
    {
        if (actionName.StartsWith("List", StringComparison.Ordinal))
            return $"List all {plural}";
        if (actionName.StartsWith("Get", StringComparison.Ordinal))
            return $"Get a single {singular}";
        if (actionName.StartsWith("Create", StringComparison.Ordinal))
            return $"Create a new {singular}";
        if (actionName.StartsWith("Update", StringComparison.Ordinal))
            return $"Update an existing {singular}";
        if (actionName.StartsWith("Delete", StringComparison.Ordinal))
            return $"Delete {Article(singular)} {singular}";
        return null;
    }

    private static string Article(string noun) =>
        "aeiou".Contains(char.ToLowerInvariant(noun[0])) ? "an" : "a";

    /// <summary>
    /// Convention description for an action parameter; null when no convention applies.
    /// A [Description] attribute on the parameter overrides this in both consumers.
    /// </summary>
    public static string? ParamDescription(string paramName, string actionName, string singular)
    {
        if (paramName == "organizationId") return "Organization ID";
        if (paramName == "id") return $"{singular} ID";
        if (paramName == "dto")
        {
            if (actionName.StartsWith("Create", StringComparison.Ordinal))
                return $"The {singular} to create";
            if (actionName.StartsWith("Update", StringComparison.Ordinal))
                return $"The new {singular} values";
            return "Request payload";
        }
        if (paramName.EndsWith("Id", StringComparison.Ordinal) && paramName.Length > 2)
        {
            var noun = char.ToUpperInvariant(paramName[0]) + paramName[1..^2];
            return $"{Singular(noun)} ID";
        }
        return null;
    }

    /// <summary>"AgentRoleAssignmentController" → "AgentRoleAssignment".</summary>
    public static string Singular(string controllerName)
    {
        return controllerName.EndsWith("Controller", StringComparison.Ordinal)
            ? controllerName[..^"Controller".Length]
            : controllerName;
    }

    /// <summary>"AgentModuleSupply" → "AgentModuleSupplies".</summary>
    public static string Plural(string singular)
    {
        if (singular.Length > 1 && singular[^1] == 'y' && !"aeiou".Contains(char.ToLowerInvariant(singular[^2])))
            return singular[..^1] + "ies";
        return singular + "s";
    }
}
