// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Steps.StateMigrations;

namespace SnapCd.Server.Core.Events.Jobs.Module;

/// <summary>
/// Starts a job that moves, imports or removes addresses in a Module's state, then checks what is
/// there. Each address is run on its own, so a batch can end partly done.
/// </summary>
public class StateMoveJobRequested : ModuleJobEventBase
{
    public AddressOperation Operation { get; set; }

    public List<AddressInstruction> Instructions { get; set; } = [];
}
