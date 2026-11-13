// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.UI.Dashboard.Components;

/// <summary>
/// Links into the one hierarchy page. The selection is a query string, so a caller holding only a
/// "stack/namespace/module" display name does not have to take it apart itself.
/// </summary>
public static class HierarchyRoute
{
    public const string Page = "/Tree";

    public static string ForStack(string stackName) =>
        $"{Page}?stack={Uri.EscapeDataString(stackName)}";

    public static string ForNamespace(string stackName, string namespaceName) =>
        $"{ForStack(stackName)}&namespace={Uri.EscapeDataString(namespaceName)}";

    public static string ForModule(string stackName, string namespaceName, string moduleName) =>
        $"{ForNamespace(stackName, namespaceName)}&module={Uri.EscapeDataString(moduleName)}";

    /// <summary>
    /// The link for a "stack/namespace/module" display name, with an optional query of its own to
    /// carry onto it. A name with fewer parts links to the deepest level it does name.
    /// </summary>
    public static string ForDisplayName(string displayName, string? extraQuery = null)
    {
        var parts = displayName.Split('/');

        var link = parts.Length switch
        {
            >= 3 => ForModule(parts[0], parts[1], parts[2]),
            2 => ForNamespace(parts[0], parts[1]),
            _ => ForStack(displayName)
        };

        return string.IsNullOrEmpty(extraQuery) ? link : $"{link}&{extraQuery}";
    }
}
