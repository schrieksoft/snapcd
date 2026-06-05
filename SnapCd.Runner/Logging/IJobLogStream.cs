// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging;

namespace SnapCd.Runner.Logging;

/// <summary>
/// Producer-side contract for shipping a single job-progress log entry to the server.
/// Implementations own the queueing, batching, and outage-buffering machinery; callers
/// just hand over an envelope + message. Holding an <see cref="IJobLogStream"/> at the
/// call site is the type-level signal that "this log ships" — distinct from a vanilla
/// <c>ILogger&lt;T&gt;</c> which is terminal-only.
/// </summary>
public interface IJobLogStream
{
    Task EmitAsync(JobLogEnvelope envelope, string message, CancellationToken ct = default);
}

/// <summary>
/// Structured envelope identifying which job/task the log line belongs to. Required at every
/// emit site so the server can route the entry to the right <c>module_job_logs</c> view.
/// </summary>
public sealed record JobLogEnvelope(
    Guid JobId,
    Guid ModuleId,
    string TaskName,
    string StackName,
    string NamespaceName,
    string ModuleName,
    LogLevel Level);
