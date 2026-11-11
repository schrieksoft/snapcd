// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Services.Nonsecured.Plans;

namespace SnapCd.Server.Core.Tests.Tests.Plans;

/// <summary>
/// The walk reads a plan the way the engine's own renderer does not: it descends through a value
/// whatever holds it. Each case here is one thing the engine's printed output cannot show, and
/// each failed before the rule that fixes it existed.
/// </summary>
public class StructuralDiffTests
{
    /// <summary>
    /// The case the work item exists for. The engine diffs inside a string only when the string is
    /// the changed value itself, so the same change reads differently depending on whether a list
    /// holds it. The walk has to report the same thing both times.
    /// </summary>
    [Fact]
    public void A_value_in_a_list_reads_the_same_as_a_bare_value()
    {
        const string before = "a: 1\nb: 2\n";
        const string after = "a: 1\nb: 3\n";

        var inList = Resource(
            "\"before\": {\"values\": [" + Json(before) + "]}, " +
            "\"after\": {\"values\": [" + Json(after) + "]}");

        var bare = Resource(
            "\"before\": {\"values\": " + Json(before) + "}, " +
            "\"after\": {\"values\": " + Json(after) + "}");

        Assert.Equal(1, inList.ChangedLeaves);
        Assert.Equal(1, bare.ChangedLeaves);
    }

    [Fact]
    public void A_change_inside_an_embedded_document_is_found()
    {
        var diff = Resource(
            "\"before\": {\"spec\": " + Json("replicas: 1\n") + "}, " +
            "\"after\": {\"spec\": " + Json("replicas: 2\n") + "}");

        var leaf = Leaves(diff).Single();
        Assert.Equal("spec.replicas", leaf.Path);
        Assert.Equal(NodeAction.Changed, leaf.Action);
    }

    [Fact]
    public void A_change_several_formats_deep_is_found()
    {
        var diff = Resource(
            "\"before\": {\"outer\": " + Json("config: |\n  {\"effect\": \"Allow\"}\n") + "}, " +
            "\"after\": {\"outer\": " + Json("config: |\n  {\"effect\": \"Deny\"}\n") + "}");

        Assert.Equal("outer.config.effect", Leaves(diff).Single().Path);
    }

    [Fact]
    public void Several_documents_in_one_string_are_walked_separately()
    {
        var diff = Resource(
            "\"before\": {\"m\": " + Json("kind: A\nv: 1\n---\nkind: B\nv: 2\n") + "}, " +
            "\"after\": {\"m\": " + Json("kind: A\nv: 1\n---\nkind: B\nv: 3\n") + "}");

        Assert.Equal("m[1].v", Leaves(diff).Single().Path);
    }

    /// <summary>A map is unordered, so writing its keys in another order is not a change.</summary>
    [Fact]
    public void Reordered_keys_are_not_a_change()
    {
        var diff = Resource(
            "\"before\": {\"spec\": " + Json("b: 2\na: 1\n") + "}, " +
            "\"after\": {\"spec\": " + Json("a: 1\nb: 2\n") + "}");

        Assert.Equal(0, diff.ChangedLeaves);
    }

    /// <summary>
    /// Pairing array elements by position makes one insertion read as every later element having
    /// changed, which is the cascade that makes a long list unreadable.
    /// </summary>
    [Fact]
    public void Inserting_into_a_list_of_scalars_is_one_change()
    {
        var diff = Resource(
            "\"before\": {\"zones\": [\"a\", \"b\", \"c\"]}, \"after\": {\"zones\": [\"a\", \"x\", \"b\", \"c\"]}");

        var leaf = Leaves(diff).Single();
        Assert.Equal(NodeAction.Added, leaf.Action);
    }

    [Fact]
    public void Inserting_into_a_list_of_objects_is_one_change()
    {
        var diff = Resource(
            "\"before\": {\"rules\": [{\"port\": 80}, {\"port\": 443}]}, \"after\":  {\"rules\": [{\"port\": 22}, {\"port\": 80}, {\"port\": 443}]}");

        Assert.Equal(1, diff.ChangedLeaves);
    }

    /// <summary>An element that carries an identity is matched by it, so a move is not a change.</summary>
    [Fact]
    public void Elements_are_paired_by_a_nested_identity_rather_than_position()
    {
        var diff = Resource(
            "\"before\": {\"items\": [{\"metadata\": {\"name\": \"a\"}, \"v\": 1}, {\"metadata\": {\"name\": \"b\"}, \"v\": 2}]}, \"after\":  {\"items\": [{\"metadata\": {\"name\": \"b\"}, \"v\": 2}, {\"metadata\": {\"name\": \"a\"}, \"v\": 9}]}");

        var leaf = Leaves(diff).Single();
        Assert.EndsWith(".v", leaf.Path);
    }

    [Fact]
    public void Moving_an_element_is_not_a_change()
    {
        var diff = Resource(
            "\"before\": {\"rules\": [{\"name\": \"a\", \"port\": 80}, {\"name\": \"b\", \"port\": 8080}]}, \"after\":  {\"rules\": [{\"name\": \"b\", \"port\": 8080}, {\"name\": \"a\", \"port\": 80}]}");

        Assert.Equal(0, diff.ChangedLeaves);
    }

    /// <summary>
    /// A value the plan cannot predict has no after to compare, so the walk says so rather than
    /// reporting a change to null.
    /// </summary>
    [Fact]
    public void An_unpredictable_value_is_reported_as_unknown()
    {
        var diff = Resource(
            "\"before\": {\"ip\": \"10.0.0.1\"}, \"after\": {}, \"after_unknown\": {\"ip\": true}");

        var leaf = Leaves(diff).Single(l => l.Name == "ip");
        Assert.True(leaf.UnknownAfterApply);
    }

    /// <summary>
    /// The node says whether a secret changed and carries nothing else. A node that held the value
    /// would put it one forgotten condition away from the screen.
    /// </summary>
    [Fact]
    public void A_sensitive_value_never_reaches_the_tree()
    {
        var diff = Resource(
            "\"before\": {\"password\": \"old-secret\"}, \"after\": {\"password\": \"new-secret\"}, \"after_sensitive\": {\"password\": true}");

        var leaf = Leaves(diff).Single();
        Assert.True(leaf.Sensitive);
        Assert.Equal(NodeAction.Changed, leaf.Action);
        Assert.Null(leaf.Before);
        Assert.Null(leaf.After);
    }

    [Fact]
    public void A_sensitive_subtree_never_reaches_the_tree()
    {
        var diff = Resource(
            "\"before\": {\"auth\": {\"user\": \"karl\", \"pass\": \"old\"}}, \"after\":  {\"auth\": {\"user\": \"karl\", \"pass\": \"new\"}}, \"after_sensitive\": {\"auth\": true}");

        var leaf = Leaves(diff).Single();
        Assert.True(leaf.Sensitive);
        Assert.Empty(leaf.Children);
        Assert.Null(leaf.Before);
    }

    /// <summary>A multi-line value holding no structure still reads better as a diff.</summary>
    [Fact]
    public void A_multiline_value_without_structure_is_marked_for_a_line_diff()
    {
        var diff = Resource(
            "\"before\": {\"cert\": " + Json("AAAA\nBBBB\nCCCC") + "}, " +
            "\"after\": {\"cert\": " + Json("AAAA\nZZZZ\nCCCC") + "}");

        Assert.True(Leaves(diff).Single().Multiline);
    }

    [Fact]
    public void A_value_that_changes_kind_is_a_replacement()
    {
        var diff = Resource(
            "\"before\": {\"port\": \"8080\"}, \"after\": {\"port\": 8080}");

        Assert.Equal(NodeAction.Replaced, Leaves(diff).Single().Action);
    }

    [Fact]
    public void A_plan_with_no_changes_reports_none()
    {
        var diff = Resource(
            "\"before\": {\"a\": 1}, \"after\": {\"a\": 1}");

        Assert.Equal(0, diff.ChangedLeaves);
        Assert.False(diff.Root.HasChange);
    }

    private static ResourceDiff Resource(string change)
    {
        var json =
            "{\"format_version\": \"1.2\", \"terraform_version\": \"test\", " +
            "\"resource_changes\": [{\"address\": \"test.one\", \"type\": \"test\", " +
            "\"provider_name\": \"test\", \"change\": {\"actions\": [\"update\"], " +
            change + "}}]}";

        return PlanReader.Parse(json).Resources.Single();
    }

    /// <summary>Every leaf that changed, which is what a reader of the plan is looking for.</summary>
    private static List<DiffNode> Leaves(ResourceDiff diff)
    {
        var found = new List<DiffNode>();
        Walk(diff.Root);
        return found;

        void Walk(DiffNode node)
        {
            foreach (var child in node.Children)
            {
                if (child.Children.Count == 0)
                {
                    if (child.Action != NodeAction.Unchanged) found.Add(child);
                }
                else
                {
                    Walk(child);
                }
            }
        }
    }

    private static string Json(string value) => System.Text.Json.JsonSerializer.Serialize(value);
}
