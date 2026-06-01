// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.Dto.VariableSets;

namespace SnapCd.Server.Core.Events.Handlers;

/// <summary>
/// Published when OutputHandler.Complete is invoked from SignalR.
/// Processed by OutputCompletedInvokedConsumer to handle database work.
/// </summary>
public class OutputCompletedInvoked
{
    public required Guid JobId { get; set; }
    public OutputSetCreateDto? OutputSet { get; set; }
}

/// <summary>
/// Published when VariableHandler.Complete is invoked from SignalR.
/// Processed by VariablesCompletedInvokedConsumer to handle database work.
/// </summary>
public class VariablesCompletedInvoked
{
    public required Guid JobId { get; set; }
    public VariableSetCreateDto? VariableSet { get; set; }
}

/// <summary>
/// Published when ReportRunningTaskHandler.Report is invoked from SignalR.
/// Processed by ReportRunningTaskInvokedConsumer to handle database work.
/// </summary>
public class ReportRunningTaskInvoked
{
    public required Guid OrganizationId { get; set; }
    public required Guid JobId { get; set; }
    public required string TaskName { get; set; }
    public required Guid RunnerId { get; set; }
    public string? RunnerInstanceName { get; set; }
}
