// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Settings;

public class RunnerSettings
{
    public Guid OrganizationId { get; set; }

    public string Instance { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public Credentials Credentials { get; set; } = new() { ClientId = string.Empty, ClientSecret = string.Empty };
}

public class Credentials
{
    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }
}