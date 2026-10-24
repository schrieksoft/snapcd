// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Sagas.Base;

namespace SnapCd.Server.Core.StateMachine.Jobs.Utils;

/// <summary>
/// The server-side error a job carries when the server ends it because its runner stopped sending heartbeats.
/// </summary>
public static class LostRunnerError
{
    public const string Header = "Runner stopped responding";

    public static string Message(JobSagaBase saga, string? reason)
    {
        var runner = string.IsNullOrEmpty(saga.RunnerInstanceName)
            ? saga.RunnerName
            : $"{saga.RunnerName} ({saga.RunnerInstanceName})";

        var because = string.IsNullOrEmpty(reason) ? "" : $" {reason}";

        return $"The job was assigned to runner '{runner}' but stopped sending heartbeats, so the server ended it.{because} "
               + $"The job was in state '{saga.CurrentState}' at the time. "
               + "The runner may have lost its connection to the server, been stopped, or failed before it could report an error. "
               + "Check the runner's logs for the period leading up to this job's end time.";
    }
}
