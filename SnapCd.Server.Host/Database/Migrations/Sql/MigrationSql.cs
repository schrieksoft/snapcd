// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using System.Text.RegularExpressions;

namespace SnapCd.Server.Host.Database.Migrations.Sql;

/// <summary>
/// Reads the SQL a migration deploys from the file of the same name beside it, embedded as a
/// resource under Database/Migrations/Sql.
/// </summary>
internal static class MigrationSql
{
    /// <summary>
    /// <c>MigrationBuilder.Sql</c> takes one batch at a time. The GO split matches
    /// IdempotentSqlManager's, so a batch valid in one is valid in the other.
    /// </summary>
    public static IEnumerable<string> ReadBatches(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded SQL resource '{resourceName}' not found. Migration SQL must sit in "
                + "Database/Migrations/Sql and be included as an EmbeddedResource in "
                + "SnapCd.Server.Host.csproj.");

        using var reader = new StreamReader(stream);
        var sql = reader.ReadToEnd();

        return Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToList();
    }
}
