// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Reports configuration keys that reach no property, which the binder otherwise drops in silence.
///
/// A key that does not bind leaves its property at the default and says nothing, so a rename or a
/// typo looks exactly like a setting that works. The two cases are a key under a section that is
/// still bound, which <see cref="Check"/> finds by walking the section against the type, and a
/// section the server no longer reads at all, which nothing can find by binding and which
/// <see cref="Retired"/> names.
/// </summary>
public static class SettingsBindingCheck
{
    /// <summary>
    /// Sections the server once read and no longer does, with what replaced each. A binder check
    /// cannot find these: nothing binds them, so there is no type to compare a key against.
    /// </summary>
    private static readonly (string Section, string Replacement)[] Retired =
    [
        ("SourceRefresh:RefreshIntervalCronExpression", "BackgroundJobs:SourceRefresh:Cron"),
        ("SourceRefresh:TimeoutSeconds", "nothing; the source refresh no longer takes a timeout"),
        ("OrphanedJobCleanup:CronExpression", "BackgroundJobs:OrphanedJobCleanup:Cron"),
        ("StuckJobDetection:CronExpression", "BackgroundJobs:StuckJobDetection:Cron"),
        ("SecretStore:EnableMigrator", "nothing; it was never read")
    ];

    /// <summary>
    /// Warns for every configured key that binds to nothing. Call once at startup, after the
    /// sections have been registered and before the first of them is read.
    /// </summary>
    public static void Report(IConfiguration configuration, ILogger logger, params (string Section, Type Type)[] bound)
    {
        var retired = Retired.ToDictionary(r => r.Section, r => r.Replacement, StringComparer.OrdinalIgnoreCase);

        foreach (var (section, replacement) in Retired)
            if (configuration.GetSection(section).Exists())
                logger.LogWarning(
                    "Configuration key {Key} is no longer read. Use {Replacement}", section, replacement);

        // A retired key reaches no property either, so it would be reported twice. The message
        // naming its replacement is the useful one.
        foreach (var (section, type) in bound)
            foreach (var key in Unmatched(configuration.GetSection(section), type))
                if (!retired.ContainsKey(key))
                    logger.LogWarning(
                        "Configuration key {Key} matches no setting and is ignored", key);
    }

    /// <summary>
    /// The configured keys under this section that the type has no property for, as full paths.
    /// Recurses into a property's own type so a misspelling deep in a section is found too.
    /// </summary>
    private static IEnumerable<string> Unmatched(IConfigurationSection section, Type type)
    {
        foreach (var child in section.GetChildren())
        {
            var property = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => string.Equals(p.Name, child.Key, StringComparison.OrdinalIgnoreCase));

            if (property == null)
            {
                // A dictionary or array takes keys of its own, which are values rather than
                // property names, so the section's own children are not checked against the type.
                if (!TakesArbitraryKeys(type)) yield return child.Path;
                continue;
            }

            if (!HasOwnProperties(property.PropertyType)) continue;

            foreach (var path in Unmatched(child, property.PropertyType)) yield return path;
        }
    }

    /// <summary>Whether the type is one a reader walks into rather than parses as a value.</summary>
    private static bool HasOwnProperties(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)) return false;
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)) return false;
        if (type == typeof(Guid) || type == typeof(Uri)) return false;
        if (Nullable.GetUnderlyingType(type) != null) return false;
        if (TakesArbitraryKeys(type)) return false;

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Length > 0;
    }

    /// <summary>A dictionary or collection, whose keys are data rather than property names.</summary>
    private static bool TakesArbitraryKeys(Type type)
    {
        if (type == typeof(string)) return false;

        return type.IsArray
               || type.GetInterfaces().Any(i => i.IsGenericType &&
                                                (i.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                                                 i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
    }
}
