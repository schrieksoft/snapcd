// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites;

/// <summary>
/// Activity that sends runner task messages to the specific server instance
/// that owns the runner's SignalR connection.
/// </summary>
public class SendToRunnerActivity<TSaga, TMessage, TOutgoingMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : JobSagaBase
    where TMessage : class
    where TOutgoingMessage : StepRequestBase, new()
{
    private readonly SnapCdDbContext _dbContext;
    private readonly ILogger<SendToRunnerActivity<TSaga, TMessage, TOutgoingMessage>> _logger;

    public SendToRunnerActivity(
        SnapCdDbContext dbContext,
        ILogger<SendToRunnerActivity<TSaga, TMessage, TOutgoingMessage>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        var saga = context.Saga;

        _logger.LogInformation(
            "SendToRunnerActivity: Looking up RunnerConnection for RunnerId={RunnerId}, InstanceName={InstanceName}, OrgId={OrgId}",
            saga.RunnerId, saga.RunnerInstanceName, saga.OrganizationId);

        // Query RunnerConnection table to find which server owns this runner
        var connection = await _dbContext.RunnerConnections
            .FirstOrDefaultAsync(rc =>
                rc.RunnerId == saga.RunnerId &&
                rc.OrganizationId == saga.OrganizationId &&
                rc.InstanceName == saga.RunnerInstanceName);

        if (connection == null)
        {
            _logger.LogWarning(
                "SendToRunnerActivity: Runner not connected. RunnerId={RunnerId}, InstanceName={InstanceName}",
                saga.RunnerId, saga.RunnerInstanceName);

            // Set flag so saga knows to transition to waiting state
            saga.PreviousStateBeforeWaiting = saga.CurrentState;
            await next.Execute(context);
            return;
        }

        _logger.LogInformation(
            "SendToRunnerActivity: Found runner on ServerInstanceId={ServerInstanceId}",
            connection.ServerInstanceId);

        // Clear any previous waiting state
        saga.PreviousStateBeforeWaiting = null;
        saga.ServerInstanceId = connection.ServerInstanceId;

        // Create the message
        var message = CreateMessage(saga);

        // Construct endpoint URI for the specific server instance and message type
        var endpointUri = MassTransitHelpers.GetConsumerEndpoint(connection.ServerInstanceId, typeof(TOutgoingMessage).Name);

        _logger.LogInformation(
            "SendToRunnerActivity: Sending {MessageType} to endpoint {EndpointUri}",
            typeof(TOutgoingMessage).Name, endpointUri);

        try
        {
            // Get send endpoint and send message with non-durable, auto-delete queue settings
            var endpoint = await context.GetSendEndpoint(new Uri(endpointUri));
            await endpoint.Send(message, sendContext =>
            {
                // // Configure RabbitMQ to match receive endpoint settings (non-durable, auto-delete)
                // if (sendContext is RabbitMqSendContext rabbitContext)
                // {
                //     rabbitContext.Durable = false;
                // }
            }, context.CancellationToken);

            _logger.LogInformation(
                "SendToRunnerActivity: Successfully sent {MessageType} to {EndpointUri}",
                typeof(TOutgoingMessage).Name, endpointUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SendToRunnerActivity: Failed to send {MessageType} to {EndpointUri}",
                typeof(TOutgoingMessage).Name, endpointUri);
            throw;
        }

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context,
        IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope($"send-to-runner-{typeof(TOutgoingMessage).Name}");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    private TOutgoingMessage CreateMessage(TSaga saga)
    {
        var declared = JsonSerializer.Deserialize<ResolvedModule>(saga.DeclaredJson);

        if (declared == null)
            throw new InvalidOperationException("ResolvedModule is null");

        return new TOutgoingMessage
        {
            CorrelationId = saga.CorrelationId,
            OrganizationId = saga.OrganizationId,
            Declared = declared,
            RunnerId = saga.RunnerId,
            RunnerInstanceName = saga.RunnerInstanceName ?? string.Empty
        };
    }

}
