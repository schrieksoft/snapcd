using System.Collections.Concurrent;
using SnapCd.Contracts;

namespace SnapCd.Runner.Services;

public class ProcessRegistry
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningProcesses = new();

    public void Register(Guid requestId, CancellationTokenSource cts, CancellationType cancellationType)
    {
        _runningProcesses[FormatKey(requestId, cancellationType)] = cts;
    }

    public bool TryCancel(Guid requestId, CancellationType cancellationType)
    {
        if (_runningProcesses.TryRemove(FormatKey(requestId, cancellationType), out var cts))
        {
            cts.Cancel();
            return true;
        }

        return false;
    }

    public void Remove(Guid requestId, CancellationType cancellationType)
    {
        _runningProcesses.TryRemove(FormatKey(requestId, cancellationType), out _);
    }

    private string FormatKey(Guid requestId, CancellationType cancellationType)
    {
        return $"{cancellationType.ToString()}-{requestId}";
    }

    public bool IsActive(Guid requestId, CancellationType cancellationType)
    {
        return _runningProcesses.TryGetValue(FormatKey(requestId, cancellationType), out _);
    }
}