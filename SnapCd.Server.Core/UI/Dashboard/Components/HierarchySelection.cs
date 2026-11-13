// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.UI.Dashboard.Components;

/// <summary>
/// What the hierarchy tree has selected, which decides what the panels beside it show. A Stack
/// carries no namespace; a Namespace carries both names because that is how it is fetched.
/// </summary>
/// <param name="Scope">Which level the selection is at.</param>
/// <param name="StackName">The selected Stack, or the Stack a selected Namespace belongs to.</param>
/// <param name="NamespaceName">The selected Namespace, or the one a selected Module belongs to.</param>
/// <param name="ModuleName">The selected Module, where one is selected.</param>
public record HierarchySelection(
    SearchScope Scope,
    string StackName,
    string? NamespaceName = null,
    string? ModuleName = null)
{
    public static HierarchySelection OfStack(string stackName) =>
        new(SearchScope.Stack, stackName);

    public static HierarchySelection OfNamespace(string stackName, string namespaceName) =>
        new(SearchScope.Namespace, stackName, namespaceName);

    public static HierarchySelection OfModule(string stackName, string namespaceName, string moduleName) =>
        new(SearchScope.Module, stackName, namespaceName, moduleName);
}
