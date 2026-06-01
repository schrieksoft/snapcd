// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Server.Core;

public class ServerTaskContext
{
    private readonly Guid _jobId;
    private readonly string _taskName;
    private readonly ILogger _logger;
    private readonly JobMetadata _metadata;


    public ServerTaskContext(Guid jobId, string taskName, ILogger logger, JobMetadata metadata)
    {
        _jobId = jobId;
        _taskName = taskName;
        _logger = logger;
        _metadata = metadata;
    }

    public void LogInformation(string message, string subContext = "")
    {
        LogSomething((log, msg, args) => log.LogInformation(msg, args), message, subContext);
    }

    public void LogWarning(string message, string subContext = "")
    {
        LogSomething((log, msg, args) => log.LogWarning(msg, args), message, subContext);
    }

    public void LogError(string message, string subContext = "")
    {
        LogSomething((log, msg, args) => log.LogError(msg, args), message, subContext);
    }


    public void LogSomething(
        Action<ILogger, string, object[]> logAction, // Delegate for logging action
        string message,
        string subContext = ""
    )
    {
        var logMessage = "[{JobId}] [{TaskName}] [{StackName}.{NamespaceName}.{ModuleName}] | {ModuleId} | {Message}";
        var args = new object[]
        {
            _jobId,
            string.Join(".", _taskName, subContext).Trim('.'),
            _metadata.StackName,
            _metadata.NamespaceName,
            _metadata.ModuleName,
            _metadata.ModuleId,
            message
        };

        logAction(_logger, logMessage, args);
    }
}
