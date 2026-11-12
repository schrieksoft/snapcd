// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts.Clients;
using SnapCd.Runner.Tasks;

namespace SnapCd.Runner.Tests;

/// <summary>
/// A step outlives the connection it was dispatched on. Reconnecting replaces the hub connection,
/// so anything a running step holds for the length of that step has to resolve the connection when
/// it uses it. These cover the resolver the task methods take.
/// </summary>
public class HubConnectionResolutionTests
{
    private static HubConnection Build(string url) =>
        new HubConnectionBuilder().WithUrl(url).Build();

    [Fact]
    public void A_resolver_sees_a_connection_replaced_after_it_was_handed_over()
    {
        var first = Build("http://localhost:1/first");
        var current = first;

        Func<HubConnection?> resolver = () => current;

        var second = Build("http://localhost:1/second");
        current = second;

        Assert.Same(second, resolver());
        Assert.NotSame(first, resolver());
    }

    [Fact]
    public void A_resolver_reports_no_connection_while_one_is_being_rebuilt()
    {
        HubConnection? current = Build("http://localhost:1/first");

        Func<HubConnection?> resolver = () => current;

        // DisconnectAndReconnectAsync nulls the field between disposing and starting again.
        current = null;

        Assert.Null(resolver());
    }

    [Fact]
    public void A_reporter_sends_on_the_connection_it_is_given()
    {
        var reporter = new RecordingTaskReporter();
        var first = Build("http://localhost:1/first");
        var second = Build("http://localhost:1/second");
        var jobId = Guid.NewGuid();

        reporter.Report(jobId, "Plan", first);
        reporter.Report(jobId, "Plan", second);

        Assert.Equal([first, second], reporter.Connections);
    }

    [Fact]
    public async Task A_method_group_off_a_resolver_backed_client_resolves_at_invoke_time()
    {
        var disposed = Build("http://localhost:1/first");
        HubConnection? current = disposed;

        var client = new RunnerHubClient(() => current!);

        // Taking the method group binds the client once, which is what a dispatch site does.
        Func<Guid, Task> completed = client.InvokeApplyGetModuleCompleted;

        // Reconnecting disposes the old connection and puts a new one in its place.
        await disposed.DisposeAsync();
        current = Build("http://localhost:1/second");

        // A client bound to the disposed instance would throw ObjectDisposedException here. This
        // one reaches the replacement, which refuses the call for not being started instead.
        await Assert.ThrowsAsync<InvalidOperationException>(() => completed(Guid.NewGuid()));
    }

    private sealed class RecordingTaskReporter : IRunningTaskReporter
    {
        public List<HubConnection> Connections { get; } = [];

        public Task Report(Guid jobId, string taskName, HubConnection connection)
        {
            Connections.Add(connection);
            return Task.CompletedTask;
        }
    }
}
