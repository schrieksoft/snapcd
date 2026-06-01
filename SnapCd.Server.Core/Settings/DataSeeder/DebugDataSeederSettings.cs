// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Settings.DataSeeder.ToSeed;

namespace SnapCd.Server.Core.Settings.DataSeeder;

public class DebugDataSeederSettings
{
    public List<ServicePrincipalToSeed> ServicePrincipals { get; set; } = new();

    public List<UserToSeed> Users { get; set; } = new();

    public List<Stack> Stacks { get; set; } = new();

    public List<Runner> Runners { get; set; } = new();

    /// <summary>
    /// When true, seeds a debug-signed Enterprise license token onto the preseeded organization
    /// so debug runs can skip the paste-opaque-key flow. Defaults to false.
    /// </summary>
    public bool SeedLicenseToken { get; set; } = false;
}