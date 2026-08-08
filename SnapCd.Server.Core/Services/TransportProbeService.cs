// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

public record QueueDepth(string Queue, long Active, long Scheduled, long Error, long DeadLetter);

public record TransportDepths(string Provider, IReadOnlyList<QueueDepth> Queues, string? ProbeError)
{
    /// <summary>
    /// The transport half of the Parked-to-Silent gate: no active messages outside diagnostic
    /// surfaces. Scheduled messages are re-derived by the reconciler, never drained; error and
    /// dead-letter contents are forensics, not in-flight work.
    /// </summary>
    public bool IsTransportQuiet => ProbeError == null && Queues.Where(q => !IsDiagnosticQueue(q.Queue)).All(q => q.Active == 0);

    public static bool IsDiagnosticQueue(string name)
        => name.EndsWith("_error", StringComparison.OrdinalIgnoreCase) || name.EndsWith("_skipped", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Per-queue depths straight from the configured transport: the SQL transport via its schema
/// tables, Azure Service Bus via the management API. Read-only; never touches MassTransit.
/// </summary>
public class TransportProbeService
{
    private readonly ServiceBusSettings _settings;
    private readonly IConfiguration _configuration;

    public TransportProbeService(IOptions<ServiceBusSettings> settings, IConfiguration configuration)
    {
        _settings = settings.Value;
        _configuration = configuration;
    }

    public async Task<TransportDepths> GetDepthsAsync()
    {
        try
        {
            return _settings.BusType == BusType.AzureServiceBus ? await ProbeAzureServiceBusAsync() : await ProbeSqlTransportAsync();
        }
        catch (Exception ex)
        {
            return new TransportDepths(_settings.BusType.ToString(), [], ex.Message);
        }
    }

    private async Task<TransportDepths> ProbeSqlTransportAsync()
    {
        var connectionString = _settings.TransportOptions.SqlServer?.ConnectionString
                               ?? _configuration["ConnectionString"]
                               ?? throw new InvalidOperationException("No SQL transport connection string configured");

        // Queue rows come in one per (Name, Type): 1 = queue, 2 = error, 3 = dead-letter.
        const string sql = """
                           SELECT q.Name, q.Type,
                                  COUNT(md.MessageDeliveryId) AS Total,
                                  ISNULL(SUM(CASE WHEN md.EnqueueTime > SYSUTCDATETIME() THEN 1 ELSE 0 END), 0) AS Scheduled
                           FROM transport.[Queue] q
                           LEFT JOIN transport.MessageDelivery md ON md.QueueId = q.Id
                           GROUP BY q.Name, q.Type
                           """;

        var byName = new Dictionary<string, (long Active, long Scheduled, long Error, long DeadLetter)>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            var type = reader.GetByte(1);
            var total = reader.GetInt32(2);
            var scheduled = reader.GetInt32(3);

            var entry = byName.TryGetValue(name, out var existing) ? existing : default;
            entry = type switch
            {
                1 => entry with { Active = total - scheduled, Scheduled = scheduled },
                2 => entry with { Error = total },
                3 => entry with { DeadLetter = total },
                _ => entry
            };
            byName[name] = entry;
        }

        var queues = byName
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new QueueDepth(kvp.Key, kvp.Value.Active, kvp.Value.Scheduled, kvp.Value.Error, kvp.Value.DeadLetter))
            .ToList();
        return new TransportDepths("SqlServer", queues, null);
    }

    private async Task<TransportDepths> ProbeAzureServiceBusAsync()
    {
        var connectionString = _settings.TransportOptions.AzureServiceBus?.ConnectionString
                               ?? throw new InvalidOperationException("No Azure Service Bus connection string configured");

        var admin = connectionString.StartsWith("sb://", StringComparison.OrdinalIgnoreCase)
            ? new ServiceBusAdministrationClient(new Uri(connectionString).Host, new DefaultAzureCredential())
            : new ServiceBusAdministrationClient(connectionString);

        var queues = new List<QueueDepth>();
        await foreach (var queue in admin.GetQueuesRuntimePropertiesAsync())
            queues.Add(new QueueDepth(queue.Name, queue.ActiveMessageCount, queue.ScheduledMessageCount, 0, queue.DeadLetterMessageCount));

        return new TransportDepths("AzureServiceBus", queues.OrderBy(q => q.Queue).ToList(), null);
    }
}
