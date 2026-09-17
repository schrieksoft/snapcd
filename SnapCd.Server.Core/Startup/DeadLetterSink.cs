// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// One durable queue per namespace that every endpoint forwards its dead-lettered messages to.
/// A forwarded dead letter becomes an ordinary message there and expires with the queue's TTL,
/// so dead letters can never accumulate past a day, whatever caused them.
/// </summary>
public static class DeadLetterSink
{
    public const string QueueName = "dead-letter-sink";

    public static readonly TimeSpan TimeToLive = TimeSpan.FromDays(1);
}

/// <summary>Creates the sink before the bus starts: Service Bus refuses a forward target that does not exist.</summary>
public class DeadLetterSinkHostedService(string connectionString, ILogger<DeadLetterSinkHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var client = connectionString.StartsWith("sb://", StringComparison.OrdinalIgnoreCase)
            ? new ServiceBusAdministrationClient(new Uri(connectionString).Host, new DefaultAzureCredential())
            : new ServiceBusAdministrationClient(connectionString);

        if (!await client.QueueExistsAsync(DeadLetterSink.QueueName, cancellationToken))
        {
            await client.CreateQueueAsync(new CreateQueueOptions(DeadLetterSink.QueueName)
            {
                DefaultMessageTimeToLive = DeadLetterSink.TimeToLive,
                DeadLetteringOnMessageExpiration = false
            }, cancellationToken);
            logger.LogInformation("Created dead-letter sink queue {Queue} with a TTL of {TimeToLive}", DeadLetterSink.QueueName, DeadLetterSink.TimeToLive);
        }

        await ForwardExistingDurableQueues(client, cancellationToken);
    }

    // MassTransit only creates entities it finds missing, so queues from earlier releases keep their
    // settings. Instance-specific queues are skipped: a forward disables their AutoDeleteOnIdle.
    private async Task ForwardExistingDurableQueues(ServiceBusAdministrationClient client, CancellationToken cancellationToken)
    {
        await foreach (var queue in client.GetQueuesAsync(cancellationToken))
        {
            if (queue.Name == DeadLetterSink.QueueName || IsInstanceSpecific(queue.Name) || queue.AutoDeleteOnIdle < TimeSpan.FromDays(365))
                continue;
            if (queue.ForwardDeadLetteredMessagesTo == DeadLetterSink.QueueName)
                continue;

            queue.ForwardDeadLetteredMessagesTo = DeadLetterSink.QueueName;
            try
            {
                await client.UpdateQueueAsync(queue, cancellationToken);
                logger.LogInformation("Queue {Queue} now forwards dead letters to {Sink}", queue.Name, DeadLetterSink.QueueName);
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                logger.LogDebug("Queue {Queue} was deleted while being updated", queue.Name);
            }
        }
    }

    private static bool IsInstanceSpecific(string queueName) =>
        queueName.StartsWith("runner--", StringComparison.Ordinal)
        || queueName.StartsWith("agent--", StringComparison.Ordinal)
        || queueName.StartsWith("fanout--", StringComparison.Ordinal);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
