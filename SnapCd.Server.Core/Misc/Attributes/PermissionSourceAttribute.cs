// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Misc.Attributes;

public enum PermissionVerb
{
    Create,
    Read,
    Update,
    Delete,
    RunJob
}

/// <summary>
/// Escape hatch for the permission documentation extractor. Permissions are normally
/// resolved by convention — the secured repository from the controller's
/// GenericCrudController type argument or the {X}Controller → {X}SecuredRepository
/// name match, the verb from the action name — and this attribute overrides that
/// resolution where the conventions don't hold. Contradicting the type-system
/// resolution requires <see cref="OverridesInheritance"/> so a stray attribute can't
/// silently misdocument an endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PermissionSourceAttribute : Attribute
{
    /// <summary>The secured repository whose permission maps document this endpoint.</summary>
    public Type? Repository { get; init; }

    /// <summary>The map to read; resolved from the action name when unset.</summary>
    public PermissionVerb Verb { get; init; } = (PermissionVerb)(-1);

    /// <summary>Required when <see cref="Repository"/> contradicts the controller's generic argument.</summary>
    public bool OverridesInheritance { get; init; }

    /// <summary>Marks the endpoint as deliberately undocumented (counts as covered).</summary>
    public bool Skip { get; init; }

    /// <summary>
    /// Per-endpoint prose. On a documented endpoint it becomes the notes line; combined
    /// with <see cref="Skip"/> it emits a prose-only permissions block for endpoints
    /// whose authorization is not role-map-shaped (session helpers, Basic-auth
    /// protocols, per-user data).
    /// </summary>
    public string? Notes { get; init; }

    public PermissionVerb? VerbOrNull => Enum.IsDefined(Verb) ? Verb : null;
}
