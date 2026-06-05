// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Settings.DataSeeder.ToSeed;

namespace SnapCd.Server.Core.Settings.DataSeeder;

/// <summary>
/// Debug-time data seeder for the Server. Runs on startup only when the Server is built with the
/// Development environment; lets a developer pre-populate a fresh database with Service Principals,
/// Users, Stacks, Runners, and an Enterprise license token without going through the Dashboard's
/// usual setup flow. Never runs in non-Development environments.
/// </summary>
public class DebugDataSeederSettings
{
    /// <summary>
    /// Service Principals to seed alongside the default preseeded ones. Useful for developer
    /// workstations that need extra SPs for testing per-SP authorization paths.
    /// </summary>
    public List<ServicePrincipalToSeed> ServicePrincipals { get; set; } = new();

    /// <summary>
    /// Additional Users to seed alongside the preseeded admin. Useful for developer workstations
    /// that need multiple Users for testing role-assignment flows.
    /// </summary>
    public List<UserToSeed> Users { get; set; } = new();

    // [JsonIgnore] keeps the raw EF entity types Stack / Runner out of the generated JSON Schema —
    // they have navigation properties back into the entity graph (Modules → Namespaces → Stacks
    // → Modules …) and recurse infinitely. ConfigurationBinder ignores [JsonIgnore], so runtime
    // binding is unaffected.
    //
    // TODO: align with the ServicePrincipals/Users pattern by introducing StackToSeed /
    // RunnerToSeed wrappers under DataSeeder/ToSeed/. The entity-shaped variants are an
    // inconsistency, not a feature.
    [JsonIgnore]
    public List<Stack> Stacks { get; set; } = new();

    [JsonIgnore]
    public List<Runner> Runners { get; set; } = new();

    /// <summary>
    /// When true, seeds a debug-signed Enterprise license token onto the preseeded organization
    /// so debug runs can skip the paste-opaque-key flow. Defaults to false.
    /// </summary>
    public bool SeedLicenseToken { get; set; } = false;
}