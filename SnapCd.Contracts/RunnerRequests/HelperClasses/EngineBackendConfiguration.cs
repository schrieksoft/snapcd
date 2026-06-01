// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

/// <summary>
/// Engine flag configuration sent with the Init request.
/// </summary>
public class EngineBackendConfiguration
{
    public List<PulumiFlagEntry> PulumiFlags { get; set; } = [];
    public List<PulumiArrayFlagEntry> PulumiArrayFlags { get; set; } = [];

    public List<TerraformFlagEntry> TerraformFlags { get; set; } = [];
    public List<TerraformArrayFlagEntry> TerraformArrayFlags { get; set; } = [];
}

public class PulumiFlagEntry
{
    public PulumiCommandTask Task { get; set; }
    public PulumiFlag Flag { get; set; }
    public string? Value { get; set; }
}

public class PulumiArrayFlagEntry
{
    public PulumiCommandTask Task { get; set; }
    public PulumiArrayFlag Flag { get; set; }
    public string Value { get; set; } = null!;
}

public class TerraformFlagEntry
{
    public TerraformCommandTask Task { get; set; }
    public TerraformFlag Flag { get; set; }
    public string? Value { get; set; }
}

public class TerraformArrayFlagEntry
{
    public TerraformCommandTask Task { get; set; }
    public TerraformArrayFlag Flag { get; set; }
    public string Value { get; set; } = null!;
}
