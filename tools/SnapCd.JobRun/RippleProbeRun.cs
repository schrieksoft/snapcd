// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.JobRun;

/// <summary>
/// A ripple probe: a read of one Module with an upstream's changed outputs supplied as the values
/// they will become. Unlike the manual job kinds it does not pause the Module, because it runs
/// alongside whatever that Module is already doing.
/// </summary>
public static class RippleProbeRun
{
    /// <summary>
    /// What a probe is handed. The after value is base64 of d4 00 00, cty's msgpack encoding of an
    /// unknown, so a run proves the unknown survives the trip rather than that a string does.
    /// </summary>
    private const string Inputs =
        """{"format_version":"1","variables":{"prefix_source":{"action":"unknown","type":"InN0cmluZyI=","before":"pGRlbW8=","after":"1AAA"}}}""";

    public static async Task<int> Execute(
        IServiceProvider services,
        AsyncServiceScope scope,
        IPrincipalProvider principal,
        IDbContextFactory<SnapCdDbContext> dbFactory,
        RunOptions options,
        NotificationWatch notifications)
    {
        var probes = scope.ServiceProvider
            .GetRequiredService<RippleProbeServiceFactory>().Create(principal);

        // --counterparty names the upstream, as it does for a transfer's other side. Which
        // Modules really consume a changed output is the server's lookup, not this harness's.
        var job = await probes.Start(
            options.ModuleId,
            options.OrganizationId,
            options.CounterpartyModuleId,
            Inputs);

        Console.WriteLine($"Started ripple probe {job.Id}");

        var status = await Watch(dbFactory, job.Id, options);
        await notifications.SettleAsync();
        await Report(dbFactory, job.Id);

        return status == ExecutionStatus.Completed ? 0 : 1;
    }

    private static async Task<ExecutionStatus> Watch(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        Guid jobId,
        RunOptions options)
    {
        var deadline = DateTime.UtcNow + options.Timeout;
        ExecutionStatus? last = null;

        while (DateTime.UtcNow < deadline)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var job = await db.RippleProbeJobs.AsNoTracking()
                .SingleOrDefaultAsync(j => j.Id == jobId);

            if (job is null)
            {
                await Task.Delay(500);
                continue;
            }

            if (job.Status != last)
            {
                Console.WriteLine($"  status {job.Status}");
                last = job.Status;
            }

            if (job.Status != ExecutionStatus.Running) return job.Status;

            await Task.Delay(500);
        }

        Console.WriteLine("  timed out");
        return last ?? ExecutionStatus.Unknown;
    }

    /// <summary>
    /// The signals are the point of the run, so they are printed rather than summarised: a probe
    /// that completes while reporting nothing has not done its job.
    /// </summary>
    private static async Task Report(IDbContextFactory<SnapCdDbContext> dbFactory, Guid jobId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var job = await db.RippleProbeJobs.AsNoTracking().SingleOrDefaultAsync(j => j.Id == jobId);

        if (job is null)
        {
            Console.WriteLine("##### No RippleProbeJobs row was ever read");
            return;
        }

        Console.WriteLine($"##### Result {job.Status}");
        Console.WriteLine($"Inputs  {job.InputsJson}");
        Console.WriteLine($"Signals {job.SignalsJson ?? "(none)"}");

        if (job.ServerSideError is not null)
            Console.WriteLine($"Error   {job.ServerSideError}");

        DescribeUnknowns(job.SignalsJson);
    }

    /// <summary>
    /// Reading the column proves a string survived; decoding it proves an unknown did. An unknown
    /// is cty's msgpack d4 00 00, so the check is on the decoded bytes rather than on the text.
    /// </summary>
    private static void DescribeUnknowns(string? signalsJson)
    {
        if (string.IsNullOrWhiteSpace(signalsJson)) return;

        using var document = JsonDocument.Parse(signalsJson);
        if (!document.RootElement.TryGetProperty("outputs", out var outputs)) return;

        // Both halves are decoded, because a check that can only report "unknown" proves nothing:
        // before is the concrete value the output holds today, after is what the probe reported.
        foreach (var output in outputs.EnumerateObject())
        foreach (var field in new[] { "before", "after" })
        {
            if (!output.Value.TryGetProperty(field, out var encoded)) continue;
            if (encoded.GetString() is not { } value) continue;

            var bytes = Convert.FromBase64String(value);
            var hex = Convert.ToHexString(bytes).ToLowerInvariant();
            var description = bytes is [0xd4, 0x00, 0x00]
                ? "unknown (msgpack fixext1, ext type 0)"
                : "a concrete value";

            Console.WriteLine($"  decoded {output.Name}.{field}: {hex} -> {description}");
        }
    }
}
