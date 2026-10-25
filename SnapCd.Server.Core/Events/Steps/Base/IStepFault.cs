// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Events.Steps.Base;

/// <summary>
/// A step's fault, whichever job family it belongs to: what failed, where, and whether the server or the runner failed it.
/// </summary>
public interface IStepFault
{
    ServerSideStep Step { get; }
    string? ErrorMessage { get; }
    string? StackTrace { get; }
    bool IsServerSideError { get; }
}
