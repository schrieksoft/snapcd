// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using System.Runtime.CompilerServices;
using SnapCd.Server.Core.Misc.Helpers;

namespace SnapCd.Server.Core.Tests.Tests.Permissions;

/// <summary>
/// The permission documentation pipeline (PermissionDocExtractor) reads permission
/// maps off uninitialized repository instances — no constructor, no DbContext. That
/// only works if every map getter is literal data: no instance state, no services,
/// no clock. These tests fail on any repo whose maps (or PermissionNotes) throw or
/// return different contents across reads, which would silently break the "docs
/// cannot drift from enforcement" guarantee.
/// </summary>
public class PermissionMapPurityTests
{
    [Fact]
    public void AllPermissionMapsAreReadableAndStableOnUninitializedInstances()
    {
        var failures = new List<string>();

        foreach (var repoType in MapShapedRepositories())
        foreach (var property in repoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.PropertyType == typeof(PermissionMap)))
        {
            try
            {
                var instance = RuntimeHelpers.GetUninitializedObject(repoType);
                var first = Flatten((PermissionMap)property.GetValue(instance)!);
                var second = Flatten((PermissionMap)property.GetValue(instance)!);

                if (!first.SequenceEqual(second))
                    failures.Add($"{repoType.Name}.{property.Name}: contents differ between reads");
            }
            catch (Exception e)
            {
                failures.Add($"{repoType.Name}.{property.Name}: {e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void AllPermissionNotesAreReadableOnUninitializedInstances()
    {
        var failures = new List<string>();

        foreach (var repoType in MapShapedRepositories())
        {
            var property = repoType.GetProperty("PermissionNotes");
            if (property is null) continue;

            try
            {
                var instance = RuntimeHelpers.GetUninitializedObject(repoType);
                var first = property.GetValue(instance) as string;
                var second = property.GetValue(instance) as string;

                if (first != second)
                    failures.Add($"{repoType.Name}.PermissionNotes: contents differ between reads");
            }
            catch (Exception e)
            {
                failures.Add($"{repoType.Name}.PermissionNotes: {e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
            }
        }

        Assert.Empty(failures);
    }

    private static List<string> Flatten(PermissionMap map)
    {
        return typeof(PermissionMap).GetProperties()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .SelectMany(p => ((System.Collections.IEnumerable)p.GetValue(map)!)
                .Cast<object>()
                .Select(role => $"{p.Name}.{role}"))
            .ToList();
    }

    /// <summary>
    /// Every concrete repository type declaring permission maps, with single-parameter
    /// open generics closed over all constraint-satisfying entity types — the same
    /// population the extractor documents from.
    /// </summary>
    private static IEnumerable<Type> MapShapedRepositories()
    {
        var assembly = typeof(PermissionMap).Assembly;

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;
            if (type.GetProperty("ReadPermissionMap")?.PropertyType != typeof(PermissionMap)) continue;

            if (!type.IsGenericTypeDefinition)
            {
                yield return type;
                continue;
            }

            var parameters = type.GetGenericArguments();
            if (parameters.Length != 1) continue;

            var constraints = parameters[0].GetGenericParameterConstraints();
            foreach (var entity in assembly.GetTypes()
                         .Where(t => t.IsClass && !t.IsAbstract && constraints.All(c => c.IsAssignableFrom(t))))
                yield return type.MakeGenericType(entity);
        }
    }
}
