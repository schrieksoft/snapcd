// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Services.Notification;

namespace SnapCd.JobRun;

/// <summary>
/// Subscribes the way the page does, so a run says whether the page would have heard anything.
/// The page never polls: it renders once and then relies on these, so a notification that is not
/// raised is a job that appears to do nothing until someone reloads.
/// </summary>
public sealed class NotificationWatch : IDisposable
{
    private readonly ManualJobUpdatedNotificationService _jobUpdated;
    private readonly LogReceivedNotificationService _logReceived;
    private readonly Guid _moduleId;
    private readonly Action<string> _trace;

    private int _jobUpdates;
    private int _logArrivals;

    public NotificationWatch(
        IServiceProvider services, Guid moduleId, Action<string> trace)
    {
        _jobUpdated = services.GetRequiredService<ManualJobUpdatedNotificationService>();
        _logReceived = services.GetRequiredService<LogReceivedNotificationService>();
        _moduleId = moduleId;
        _trace = trace;

        _jobUpdated.Subscribe(moduleId, OnJobUpdated);
        _logReceived.Subscribe(moduleId, OnLogReceived);
    }

    public int JobUpdates => _jobUpdates;

    public int LogArrivals => _logArrivals;

    /// <summary>
    /// The job row reaches its final status before the notification about it has been consumed, so
    /// a count read the moment the watch loop ends misses the last one. A page has the same race
    /// and does not care; a report that says nothing arrived does.
    /// </summary>
    public async Task SettleAsync()
    {
        var seen = _jobUpdates + _logArrivals;

        for (var i = 0; i < 20; i++)
        {
            await Task.Delay(250);

            var now = _jobUpdates + _logArrivals;
            if (now == seen) return;

            seen = now;
        }
    }

    private Task OnJobUpdated(Guid jobId, Guid moduleId)
    {
        Interlocked.Increment(ref _jobUpdates);
        _trace($"  ~~ page would refresh: job {jobId} changed");
        return Task.CompletedTask;
    }

    private Task OnLogReceived(Guid jobId, Guid moduleId)
    {
        Interlocked.Increment(ref _logArrivals);
        _trace($"  ~~ page would refresh: logs arrived for job {jobId}");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _jobUpdated.Unsubscribe(_moduleId, OnJobUpdated);
        _logReceived.Unsubscribe(_moduleId, OnLogReceived);
    }
}
