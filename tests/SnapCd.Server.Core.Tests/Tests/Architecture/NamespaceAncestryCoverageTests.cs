// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using System.Text.RegularExpressions;

namespace SnapCd.Server.Core.Tests.Tests.Architecture;

/// <summary>
/// A role granted on a Namespace reaches every Namespace below it, so a permission query must
/// match a role assignment against the ancestor closure rather than against one Namespace id.
///
/// A query that compares the id directly still compiles and still runs. It simply fails to grant
/// a permission that was granted higher up, which no other test notices.
/// </summary>
public class NamespaceAncestryCoverageTests
{
    private static readonly string SecuredRepositories =
        Path.Combine(RepositoryRoot(), "SnapCd.Server.Core", "Repositories", "Organizations", "Secured");

    /// <summary>
    /// Joins onto a namespace role assignment table. The left side has to come from the closure,
    /// so the capture is whatever the join keys off.
    /// </summary>
    private static readonly Regex AssignmentJoin = new(
        @"join\s+\w+\s+in\s+[^\n]*?(?:Set<TNamespaceRoleAssignment>\(\)|GroupNamespaceRoleAssignments)\s*\n\s*on\s+new\s*\{([^}]*)\}(?<tail>(?:\s*\n[^\n]*){0,6})",
        RegexOptions.Compiled);

    [Fact]
    public void Every_Namespace_Role_Join_Goes_Through_The_Ancestor_Closure()
    {
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(SecuredRepositories, "*.cs", SearchOption.AllDirectories))
        {
            // Stack-scoped repositories keep their own shape until Stack is removed.
            if (file.Contains("Stack")) continue;

            var source = File.ReadAllText(file);

            foreach (Match match in AssignmentJoin.Matches(source))
            {
                var keys = match.Groups[1].Value;
                if (keys.Contains("AncestorNamespaceId")) continue;

                // The id set may be narrowed just after the join instead of inside its keys.
                if (match.Groups["tail"].Value.Contains("ancestorIds.Contains")) continue;

                var line = source.Take(match.Index).Count(c => c == '\n') + 1;
                offenders.Add($"{Path.GetFileName(file)}:{line} joins a namespace role assignment on {keys.Trim()}");
            }
        }

        Assert.Empty(offenders.OrderBy(line => line));
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SnapCd.sln"))) dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("SnapCd.sln not found above the test output.");
    }
}
