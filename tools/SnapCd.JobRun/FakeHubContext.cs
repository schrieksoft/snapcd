// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR;
using SnapCd.Server.Core.Hubs;

namespace SnapCd.JobRun;

/// <summary>
/// Replaces the SignalR hub context in the container so dispatches land on the fake runner
/// instead of a socket. Only the single-client send is implemented: anything else throws rather
/// than being quietly dropped, so an unanswered path shows up as a failure and not as a hang.
/// </summary>
public class FakeHubContext(FakeRunner runner) : IHubContext<RunnerHub>
{
    public IHubClients Clients { get; } = new FakeClients(runner);

    public IGroupManager Groups => throw new NotSupportedException("Groups are not dispatched to runners.");

    private class FakeClients(FakeRunner runner) : IHubClients
    {
        public IClientProxy Client(string connectionId) => new FakeClientProxy(runner);

        public IClientProxy All => throw new NotSupportedException();
        public IClientProxy AllExcept(IReadOnlyList<string> e) => throw new NotSupportedException();
        public IClientProxy Clients(IReadOnlyList<string> c) => throw new NotSupportedException();
        public IClientProxy Group(string g) => throw new NotSupportedException();
        public IClientProxy Groups(IReadOnlyList<string> g) => throw new NotSupportedException();
        public IClientProxy GroupExcept(string g, IReadOnlyList<string> e) => throw new NotSupportedException();
        public IClientProxy User(string u) => throw new NotSupportedException();
        public IClientProxy Users(IReadOnlyList<string> u) => throw new NotSupportedException();
    }

    private class FakeClientProxy(FakeRunner runner) : IClientProxy
    {
        public Task SendCoreAsync(
            string method, object?[] args, CancellationToken cancellationToken = default)
        {
            // Answered on its own thread: the dispatching consumer must not wait on the reply.
            _ = Task.Run(async () =>
            {
                try
                {
                    await Reply(method, args[0]!, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  !! runner reply to {method} threw: {ex.Message}");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        /// <summary>
        /// A step's reply is only accepted once the saga has moved into the state that expects
        /// it. Real work takes long enough that this is never close, but an answer given at once
        /// can arrive first, and the rejection says exactly that, so it is worth another go.
        /// </summary>
        private async Task Reply(string method, object payload, CancellationToken cancellationToken)
        {
            for (var attempt = 1; ; attempt++)
            {
                await Task.Delay(250, cancellationToken);

                try
                {
                    await runner.Handle(method, payload);
                    return;
                }
                catch (HubException ex) when (ex.Message.Contains("expected") && attempt < 20)
                {
                }
            }
        }
    }
}
