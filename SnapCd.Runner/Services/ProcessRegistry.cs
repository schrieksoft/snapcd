// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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