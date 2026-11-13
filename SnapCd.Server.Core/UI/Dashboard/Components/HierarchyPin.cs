// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.UI.Dashboard.Components;

/// <summary>
/// The one stack, or the one namespace, the tree is narrowed to. Named rather than keyed by id so
/// it survives in the browser across a reload, where ids of a deleted node would not resolve.
/// </summary>
public record HierarchyPin(string StackName, string? NamespaceName = null)
{
    public static HierarchyPin OfStack(string stackName) => new(stackName);

    public static HierarchyPin OfNamespace(string stackName, string namespaceName) =>
        new(stackName, namespaceName);

    /// <summary>What the pin is called where it is shown, such as "default" or "default / prod".</summary>
    public string Label => NamespaceName == null ? StackName : $"{StackName} / {NamespaceName}";

    public string ToStorageValue() =>
        NamespaceName == null ? $"stack:{StackName}" : $"namespace:{StackName}/{NamespaceName}";

    /// <summary>Reads back what <see cref="ToStorageValue"/> wrote, or null for anything else.</summary>
    public static HierarchyPin? FromStorageValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var separator = value.IndexOf(':');

        if (separator < 0) return null;

        var kind = value[..separator];
        var rest = value[(separator + 1)..];

        if (rest.Length == 0) return null;

        if (kind == "stack") return OfStack(rest);

        if (kind != "namespace") return null;

        var slash = rest.IndexOf('/');

        return slash <= 0 || slash == rest.Length - 1
            ? null
            : OfNamespace(rest[..slash], rest[(slash + 1)..]);
    }
}
