// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Logging;

namespace SnapCd.Runner;

/// <summary>
/// Job-progress logging surface. Each call fans out to (a) the MEL logger for terminal echo and
/// (b) <see cref="IJobLogStream"/> for shipping to the server. Holding a
/// <see cref="RunnerTaskContext"/> at the call site is the type-level signal that "this log ships".
/// Code with only an <c>ILogger&lt;T&gt;</c> in scope writes terminal-only.
/// </summary>
public class RunnerTaskContext
{
    private readonly Guid _jobId;
    private readonly string _taskName;
    private readonly ILogger _logger;
    private readonly IJobLogStream _shipper;
    private readonly JobMetadata _metadata;


    public RunnerTaskContext(Guid jobId, string taskName, ILogger logger, IJobLogStream shipper, JobMetadata metadata)
    {
        _jobId = jobId;
        _taskName = taskName;
        _logger = logger;
        _shipper = shipper;
        _metadata = metadata;
    }

    public void LogInformation(string message, string subContext = "")
        => Emit(LogLevel.Information, message, subContext);

    public void LogWarning(string message, string subContext = "")
        => Emit(LogLevel.Warning, message, subContext);

    public void LogError(string message, string subContext = "")
        => Emit(LogLevel.Error, message, subContext);

    private void Emit(LogLevel level, string message, string subContext)
    {
        var taskName = string.Join(".", _taskName, subContext).Trim('.');

        // Terminal — drop JobId/ModuleId GUIDs from the rendered line (they're noise in the
        // console; the shipper envelope below still carries them for the server).
        _logger.Log(level,
            "[{TaskName}] [{StackName}.{NamespaceName}.{ModuleName}] {Message}",
            taskName, _metadata.StackName, _metadata.NamespaceName, _metadata.ModuleName, message);

        // Ship — IJobLogStream gets the explicit envelope. HubJobLogStream returns CompletedTask
        // synchronously after enqueueing into the Serilog batching pipeline.
        var envelope = new JobLogEnvelope(
            JobId: _jobId,
            ModuleId: _metadata.ModuleId,
            TaskName: taskName,
            StackName: _metadata.StackName,
            NamespaceName: _metadata.NamespaceName,
            ModuleName: _metadata.ModuleName,
            Level: level);
        _ = _shipper.EmitAsync(envelope, message);
    }
}
