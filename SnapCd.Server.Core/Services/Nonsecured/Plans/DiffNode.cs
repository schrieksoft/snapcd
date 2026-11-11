// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;

namespace SnapCd.Server.Core.Services.Nonsecured.Plans;

public enum NodeKind
{
    Object,
    Array,
    String,
    Number,
    Bool,
    Null
}

public enum NodeAction
{
    Unchanged,
    Added,
    Removed,
    Changed,

    /// <summary>The two sides are different kinds, so there is nothing to descend into.</summary>
    Replaced
}

public enum ResourceAction
{
    NoOp,
    Create,
    Read,
    Update,
    Replace,
    Delete,
    Forget
}

/// <summary>
/// One position in a value, with what it was and what it becomes. The tree is built by walking
/// before and after together, so a node exists wherever either side has something.
/// </summary>
public sealed class DiffNode
{
    /// <summary>The key or index this node sits at, as its parent names it.</summary>
    public string Name { get; init; } = "";

    /// <summary>The full path from the resource root, for display and for filtering.</summary>
    public string Path { get; init; } = "";

    public NodeKind Kind { get; init; }

    public NodeAction Action { get; set; }

    public JsonElement? Before { get; init; }

    public JsonElement? After { get; init; }

    public List<DiffNode> Children { get; } = [];

    /// <summary>The engine cannot predict this value, so the plan carries no after.</summary>
    public bool UnknownAfterApply { get; init; }

    /// <summary>Marked sensitive by the provider or the configuration, so it is not displayed.</summary>
    public bool Sensitive { get; init; }

    /// <summary>
    /// Set where a string held a YAML or JSON document and was parsed so the walk could continue
    /// into it. The node's own Before and After stay the raw strings.
    /// </summary>
    public bool Embedded { get; init; }

    /// <summary>The format that was recognised, for the reader's benefit.</summary>
    public string? EmbeddedFormat { get; init; }

    /// <summary>
    /// A changed string holding more than one line but no structure. There is nothing to descend
    /// into, so the renderer compares it line by line instead of showing both values whole.
    /// </summary>
    public bool Multiline { get; init; }

    /// <summary>Every changed leaf at or below this node.</summary>
    public int ChangedLeaves =>
        Children.Count == 0
            ? Action == NodeAction.Unchanged ? 0 : 1
            : Children.Sum(c => c.ChangedLeaves);

    public bool HasChange => Action != NodeAction.Unchanged || Children.Any(c => c.HasChange);
}

/// <summary>One resource's change, as the plan file describes it.</summary>
public sealed class ResourceDiff
{
    public string Address { get; init; } = "";
    public string Type { get; init; } = "";
    public string ProviderName { get; init; } = "";
    public ResourceAction Action { get; init; }

    /// <summary>Why the engine chose this action, where it said.</summary>
    public string? ActionReason { get; init; }

    public DiffNode Root { get; init; } = new();

    public int ChangedLeaves => Root.ChangedLeaves;
}

/// <summary>One output's change.</summary>
public sealed class OutputDiff
{
    public string Name { get; init; } = "";
    public ResourceAction Action { get; init; }
    public bool Sensitive { get; init; }
    public DiffNode Root { get; init; } = new();
}

/// <summary>A whole plan, parsed.</summary>
public sealed class ParsedPlan
{
    public string FormatVersion { get; init; } = "";
    public string EngineVersion { get; init; } = "";
    public bool Errored { get; init; }
    public List<ResourceDiff> Resources { get; } = [];
    public List<OutputDiff> Outputs { get; } = [];

    public int Count(ResourceAction action) => Resources.Count(r => r.Action == action);
}
