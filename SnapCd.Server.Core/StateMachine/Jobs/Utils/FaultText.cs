// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text;
using MassTransit;

namespace SnapCd.Server.Core.StateMachine.Jobs.Utils;

/// <summary>
/// The exceptions carried by a MassTransit fault, as the text a job shows for its server-side error.
/// </summary>
public static class FaultText
{
    public static string Describe(ExceptionInfo[]? exceptions)
    {
        var text = new StringBuilder();
        foreach (var exception in exceptions ?? [])
            for (var current = exception; current != null; current = current.InnerException)
                text.Append(current.ExceptionType).Append(": ").Append(current.Message).Append('\n').Append(current.StackTrace).Append("\n\n");

        return text.Length > 0 ? text.ToString().TrimEnd() : "No exception details were recorded.";
    }

    /// <summary>The faulted message's type name, for a header: "InitRequested" for a Fault&lt;InitRequested&gt;.</summary>
    public static string MessageName(Type faultOrMessageType) =>
        faultOrMessageType.IsGenericType ? faultOrMessageType.GetGenericArguments()[0].Name : faultOrMessageType.Name;
}
