// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites;

public class NeedsApprovalJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : JobSagaBase
    where TMessage : class
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IApprovalPolicy _approvalPolicy;
    private readonly ModuleJobRepository _moduleJobRepository;
    private readonly ILogger<NeedsApprovalJobActivity<TSaga, TMessage>> _logger;

    public NeedsApprovalJobActivity(
        SnapCdDbContext dbContext,
        IApprovalPolicy approvalPolicy,
        ModuleJobRepository moduleJobRepository,
        ILogger<NeedsApprovalJobActivity<TSaga, TMessage>> logger)
    {
        _dbContext = dbContext;
        _approvalPolicy = approvalPolicy;
        _moduleJobRepository = moduleJobRepository;
        _logger = logger;
    }

    private int GetThreshold(TSaga saga, Thresholds thresholds)
    {
        switch (saga)
        {
            case ApplyJobSaga:
                return thresholds.ApplyApprovalThreshold;
            case DestroyJobSaga:
                return thresholds.DestroyApprovalThreshold;
            default:
                throw new NotImplementedException($"{nameof(NeedsApprovalJobActivity<TSaga, TMessage>)} not implemented for TSaga of type {saga.GetType().Name}");
        }
    }

    private class Thresholds
    {
        public int ApplyApprovalThreshold { get; set; }
        public int DestroyApprovalThreshold { get; set; }
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        var thresholds = _dbContext.Modules
            .Include(x => x.Namespace)
            .Where(x => x.Id == context.Saga.ModuleId && x.OrganizationId == context.Saga.OrganizationId)
            .Select(x => new Thresholds
            {
                ApplyApprovalThreshold = x.ApplyApprovalThreshold ?? x.Namespace.DefaultApplyApprovalThreshold ?? 0,
                DestroyApprovalThreshold = x.DestroyApprovalThreshold ?? x.Namespace.DefaultDestroyApprovalThreshold ?? 0
            })
            .Single();

        var threshold = GetThreshold(context.Saga, thresholds);

        if (!await _approvalPolicy.SupportsApprovalWorkflowsAsync(context.Saga.OrganizationId))
        {
            // The org's tier does not include the ApprovalWorkflows feature. If a non-zero
            // approval threshold is configured we must NOT silently bypass it — fail the
            // job (route through the Declined branch → ExecutionStatus.NotApproved) so the
            // misconfiguration is visible instead of being treated as auto-approved.
            if (threshold > 0)
            {
                _logger.LogWarning(
                    "Approval threshold ({Threshold}) is configured on module {ModuleId} but the organization's licence tier does not include ApprovalWorkflows; failing job {CorrelationId} with NotApproved.",
                    threshold, context.Saga.ModuleId, context.Saga.CorrelationId);

                await _moduleJobRepository.SetServerSideError(
                    context.Saga.CorrelationId,
                    context.Saga.OrganizationId,
                    ServerSideStep.Approval,
                    "Approval workflows not licenced",
                    $"This module has an approval threshold of {threshold} configured, but the organization's licence tier does not include the ApprovalWorkflows feature. Either remove the approval threshold from the module/namespace, or upgrade to a tier that includes approval workflows.");

                context.Saga.IsApproved = false;
                context.Saga.IsDeclined = true;
                await next.Execute(context).ConfigureAwait(false);
                return;
            }

            // No approval threshold configured — auto-approve and skip the approval workflow.
            context.Saga.IsApproved = true;
            context.Saga.IsDeclined = false;
            await next.Execute(context).ConfigureAwait(false);
            return;
        }

        var approvals = _dbContext.ModuleJobApprovals
            .Where(x => x.ModuleJobId == context.Saga.CorrelationId)
            .ToList();

        var isApproved = false;
        var isDeclined = approvals.Any(x => x.Declined);

        if (!isDeclined) isApproved = approvals.Count() >= threshold;

        context.Saga.IsApproved = isApproved;
        context.Saga.IsDeclined = isDeclined;

        // Proceed to the next activity
        await next.Execute(context).ConfigureAwait(false);
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
        context.CreateScope("needs-approval-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
