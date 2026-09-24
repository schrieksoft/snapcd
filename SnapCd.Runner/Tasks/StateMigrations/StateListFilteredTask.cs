// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests.StateMigrations;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>
    /// Reports which of the addresses asked about are in this Module's state. The state itself is
    /// never logged or returned: only the verdict on the addresses in the filter.
    /// </summary>
    public Task StateListFiltered(StateListFilteredRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, Guid.Empty, nameof(StateListFiltered), request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken, gracefulToken) =>
            {
                taskContext.LogNarration($"Checking {request.Addresses.Count} addresses against this module's state");

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                var (present, absent) = await engine.StateListFiltered(
                    request.Addresses, killToken, gracefulToken);

                foreach (var address in present) taskContext.LogInformation($"Found: {address}");
                foreach (var address in absent) taskContext.LogInformation($"Not found: {address}");

                var results = present
                    .Select(a => new StateAddressResult { Address = a, Outcome = "Present" })
                    .Concat(absent.Select(a => new StateAddressResult { Address = a, Outcome = "Absent" }))
                    .ToList();

                await InvokeWithRetryAsync(
                    () => client.InvokeStateListFilteredCompleted(request.JobId, results),
                    nameof(client.InvokeStateListFilteredCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeStateListFilteredFaulted(request.JobId, message, stackTrace));
}
