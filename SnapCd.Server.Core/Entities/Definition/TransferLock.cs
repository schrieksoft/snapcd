// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json.Serialization;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// One Module's claim on an open transfer. Its primary key is the rule that a Module is in at most
/// one open transfer at a time, in either role - which two filtered indexes on Transfers could not
/// express, because the pair lives in two columns of one row.
///
/// Written by a trigger on Transfers, never by hand: a transfer that opens takes both Modules, and
/// one that closes releases them.
/// </summary>
public class TransferLock
{
    public Guid OrganizationId { get; set; }

    public Guid ModuleId { get; set; }

    public Guid TransferId { get; set; }

    [JsonIgnore] public Transfer Transfer { get; set; } = null!;
}
