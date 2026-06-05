// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Mcp;

/// <summary>
/// Declares the entity name a controller manages, used by the MCP codegen for two purposes:
/// (1) template substitution in inherited generic-CRUD XML doc summaries — <c>{Entity}</c> and
///     <c>{entities}</c> placeholders on the base controller's actions are replaced with
///     <see cref="Singular"/> / <see cref="Plural"/> when emitting wrappers for the derived class;
/// (2) tool name derivation — collection-returning operations and CRUD verbs use
///     <see cref="Plural"/> (snake-cased) as the noun stem (e.g. <c>agents_list</c>, <c>agents_create</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class McpEntityAttribute : Attribute
{
    /// <summary>Capitalised singular form. Substituted for <c>{Entity}</c> placeholders.</summary>
    public required string Singular { get; init; }

    /// <summary>Capitalised plural form. Substituted for <c>{entities}</c> (lowercased) and
    /// used as the noun stem in derived tool names.</summary>
    public required string Plural { get; init; }
}
