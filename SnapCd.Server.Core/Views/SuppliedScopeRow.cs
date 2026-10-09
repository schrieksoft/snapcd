// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Views;

/// <summary>
/// A supply row as the supplies list shows it: the scope's name together with the names above
/// it, which is what makes one "default" distinguishable from another and what the links need.
/// </summary>
public class SuppliedScopeRow
{
    public Guid SupplyId { get; set; }

    public string OwnerName { get; set; } = null!;

    public string StackName { get; set; } = null!;

    /// <summary>Null on a Stack supply.</summary>
    public string? NamespaceName { get; set; }

    /// <summary>Null unless the supply is to a Module.</summary>
    public string? ModuleName { get; set; }

    /// <summary>The scope's own name, which is the last of the three that is set.</summary>
    public string ScopeName => ModuleName ?? NamespaceName ?? StackName;

    /// <summary>The names above the scope, as a path, or null for a Stack.</summary>
    public string? ParentPath => ModuleName is not null
        ? $"{StackName} / {NamespaceName}"
        : NamespaceName is not null
            ? StackName
            : null;

    /// <summary>Where the scope's own page lives.</summary>
    public string Href => ModuleName is not null
        ? $"/Module/{StackName}/{NamespaceName}/{ModuleName}"
        : NamespaceName is not null
            ? $"/Namespace/{StackName}/{NamespaceName}"
            : $"/Stack/{StackName}";
}
