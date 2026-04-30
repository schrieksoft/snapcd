using Microsoft.Extensions.Logging;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner;

public class RunnerTaskContext
{
    private readonly Guid _jobId;
    private readonly string _taskName;
    private readonly ILogger _logger;
    private readonly JobMetadata _metadata;


    public RunnerTaskContext(Guid jobId, string taskName, ILogger logger, JobMetadata metadata)
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
