// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.Nonsecured.Plans;

/// <summary>
/// One line of the rendered tree. The view is computed from the diff rather than rendered straight
/// from it, so the decisions about what to open and what to join happen in one place and can be
/// tested without a browser.
/// </summary>
public sealed class ViewRow
{
    /// <summary>
    /// The label, which is a dotted path where a chain of single-child nodes was joined. Eight
    /// nested lines each holding one child say no more than one line holding the whole path.
    /// </summary>
    public string Label { get; init; } = "";

    public int Depth { get; init; }

    /// <summary>The node this row stands for, which is the deepest of a joined chain.</summary>
    public DiffNode Node { get; init; } = null!;

    /// <summary>The path of the deepest node, which is what a toggle is keyed on.</summary>
    public string Key => Node.Path;

    public bool IsLeaf => Node.Children.Count == 0;

    /// <summary>Whether this row's children are shown below it.</summary>
    public bool IsOpen { get; init; }
}

public sealed class TreeViewOptions
{
    /// <summary>
    /// Open the path to every changed leaf. A reader opening a resource wants to see what changed,
    /// not to click down to it through levels they did not choose.
    /// </summary>
    public bool AutoExpandChanged { get; init; } = true;

    /// <summary>Show subtrees that hold no change, which are summarised rather than listed.</summary>
    public bool ShowUnchanged { get; init; }

    /// <summary>
    /// A resource being created or destroyed has every leaf changed, so opening it to the leaves
    /// lists the whole object. The summary says as much as the listing would.
    /// </summary>
    public bool SummariseWholesale { get; init; } = true;

    /// <summary>Paths the reader opened or closed by hand, which outrank the automatic choice.</summary>
    public IReadOnlySet<string> ForcedOpen { get; init; } = new HashSet<string>();

    public IReadOnlySet<string> ForcedClosed { get; init; } = new HashSet<string>();
}

public static class TreeView
{
    /// <summary>
    /// Flattens a resource's tree into the rows to draw. Rows are produced only for what is open,
    /// so the component renders a list rather than deciding what to recurse into.
    /// </summary>
    public static List<ViewRow> Rows(ResourceDiff resource, TreeViewOptions options)
    {
        var rows = new List<ViewRow>();

        var wholesale = options.SummariseWholesale
                        && resource.Action is ResourceAction.Create or ResourceAction.Delete;

        Walk(resource.Root, 0, rows, options, wholesale);
        return rows;
    }

    private static void Walk(DiffNode node, int depth, List<ViewRow> rows, TreeViewOptions options, bool wholesale)
    {
        foreach (var child in node.Children)
        {
            if (!options.ShowUnchanged && !child.HasChange) continue;

            // A chain of single-child containers carries no information at each step, so it is
            // joined into one row whose label is the whole path.
            var (deepest, label) = Join(child, options);

            var open = ShouldOpen(deepest, options, wholesale);

            rows.Add(new ViewRow
            {
                Label = label,
                Depth = depth,
                Node = deepest,
                IsOpen = open
            });

            if (open) Walk(deepest, depth + 1, rows, options, wholesale);
        }
    }

    /// <summary>
    /// Follows a run of containers that each have exactly one child worth showing, returning the
    /// deepest and the dotted label that reaches it.
    /// </summary>
    private static (DiffNode Node, string Label) Join(DiffNode node, TreeViewOptions options)
    {
        var label = node.Name;
        var current = node;

        while (true)
        {
            // A node the reader has acted on is a row of its own, so the chain stops there.
            if (options.ForcedOpen.Contains(current.Path) || options.ForcedClosed.Contains(current.Path))
                return (current, label);

            var visible = current.Children
                .Where(c => options.ShowUnchanged || c.HasChange)
                .ToList();

            if (visible.Count != 1 || visible[0].Children.Count == 0) return (current, label);

            // An embedded document is a format boundary and is worth seeing, so it ends the chain.
            if (visible[0].Embedded) return (current, label);

            current = visible[0];
            label = current.Name.StartsWith('[') ? label + current.Name : $"{label}.{current.Name}";
        }
    }

    private static bool ShouldOpen(DiffNode node, TreeViewOptions options, bool wholesale)
    {
        if (node.Children.Count == 0) return false;
        if (options.ForcedOpen.Contains(node.Path)) return true;
        if (options.ForcedClosed.Contains(node.Path)) return false;

        // Everything under a created or destroyed resource carries the same action, so listing it
        // repeats one fact per line.
        if (wholesale) return false;

        // A subtree that arrived or went whole is described rather than listed, for the same reason.
        if (node.Action is NodeAction.Added or NodeAction.Removed) return false;

        return options.AutoExpandChanged && node.HasChange;
    }

    /// <summary>
    /// What a container row says about its own shape. The row's marker already says whether it is
    /// added, removed or changed, so repeating that here made a scalar without a bracket look as
    /// though it was not removed.
    /// </summary>
    public static string Shape(DiffNode node)
        => node.Kind == NodeKind.Array
            ? $"{node.Children.Count} item{(node.Children.Count == 1 ? "" : "s")}"
            : $"{node.Children.Count} key{(node.Children.Count == 1 ? "" : "s")}";
}
