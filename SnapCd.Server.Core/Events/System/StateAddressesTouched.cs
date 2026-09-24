// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Events.System;

/// <summary>
/// What a job observed about a Module's state, address by address. Any job that touches state
/// publishes it, and the transfer ledger is closed from it: a state mv run by hand accounts for an
/// address without knowing a transfer exists.
/// </summary>
public class StateAddressesTouched
{
    public Guid ModuleId { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>The job that observed it, recorded against whatever it closes.</summary>
    public Guid JobId { get; set; }

    /// <summary>Addresses now in this Module's state.</summary>
    public List<string> Present { get; set; } = [];

    /// <summary>Addresses now absent from it.</summary>
    public List<string> Absent { get; set; } = [];
}
