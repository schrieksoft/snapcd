// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Hubs.Handlers.SplitMigrate;
using SnapCd.Server.Core.Hubs.Handlers.Transfers;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Hubs.Handlers;

/// <summary>
/// Sends a reply from a step several job families share to the family that is waiting for it.
///
/// Deployment, split and transfer jobs all run GetModule, Init, Validate and Plan on the same
/// runner endpoints, so a reply names only the job. Which family owns that job is resolved by
/// looking it up, and getting it wrong sends the reply to a saga that is not listening - which
/// looks exactly like a runner that never answered.
/// </summary>
public class SharedStepRouter(
    GetModuleHandler getModule,
    InitHandler init,
    ValidateHandler validate,
    PlanHandler plan,
    SplitGetModuleHandler splitGetModule,
    SplitInitHandler splitInit,
    SplitValidateHandler splitValidate,
    SplitPlanHandler splitPlan,
    TransferStepHandler transferStep)
{
    public Task GetModuleCompleted(JobAuthorization auth, Guid jobId) => auth.Family switch
    {
        JobSagaFamily.SplitMigrate => splitGetModule.Complete(jobId, auth.OrganizationId),
        JobSagaFamily.TransferMigrate =>
            transferStep.Complete<TransferGetModuleCompleted>(jobId, auth.OrganizationId),
        JobSagaFamily.StateListFiltered =>
            transferStep.Complete<StateListFilteredGetModuleCompleted>(jobId, auth.OrganizationId),
        _ => getModule.Complete(jobId)
    };

    public Task GetModuleFaulted(JobAuthorization auth, Guid jobId, string? error, string? stackTrace) =>
        auth.Family switch
        {
            JobSagaFamily.SplitMigrate =>
                splitGetModule.Fault(jobId, auth.OrganizationId, error, stackTrace),
            JobSagaFamily.TransferMigrate =>
                transferStep.Fault<TransferGetModuleFaulted>(jobId, auth.OrganizationId, error, stackTrace),
            JobSagaFamily.StateListFiltered =>
                transferStep.Fault<StateListFilteredGetModuleFaulted>(jobId, auth.OrganizationId, error, stackTrace),
            _ => getModule.Fault(jobId, error, stackTrace)
        };

    public Task InitCompleted(JobAuthorization auth, Guid jobId) => auth.Family switch
    {
        JobSagaFamily.SplitMigrate => splitInit.Complete(jobId, auth.OrganizationId),
        JobSagaFamily.TransferMigrate =>
            transferStep.Complete<TransferInitCompleted>(jobId, auth.OrganizationId),
        JobSagaFamily.StateListFiltered =>
            transferStep.Complete<StateListFilteredInitCompleted>(jobId, auth.OrganizationId),
        _ => init.Complete(jobId)
    };

    public Task InitFaulted(JobAuthorization auth, Guid jobId, string? error, string? stackTrace) =>
        auth.Family switch
        {
            JobSagaFamily.SplitMigrate => splitInit.Fault(jobId, auth.OrganizationId, error, stackTrace),
            JobSagaFamily.TransferMigrate =>
                transferStep.Fault<TransferInitFaulted>(jobId, auth.OrganizationId, error, stackTrace),
            JobSagaFamily.StateListFiltered =>
                transferStep.Fault<StateListFilteredInitFaulted>(jobId, auth.OrganizationId, error, stackTrace),
            _ => init.Fault(jobId, error, stackTrace)
        };

    public Task ValidateCompleted(JobAuthorization auth, Guid jobId) => auth.Family switch
    {
        JobSagaFamily.SplitMigrate => splitValidate.Complete(jobId, auth.OrganizationId),
        JobSagaFamily.TransferMigrate =>
            transferStep.Complete<TransferValidateCompleted>(jobId, auth.OrganizationId),
        _ => validate.Complete(jobId)
    };

    public Task ValidateFaulted(JobAuthorization auth, Guid jobId, string? error, string? stackTrace) =>
        auth.Family switch
        {
            JobSagaFamily.SplitMigrate => splitValidate.Fault(jobId, auth.OrganizationId, error, stackTrace),
            JobSagaFamily.TransferMigrate =>
                transferStep.Fault<TransferValidateFaulted>(jobId, auth.OrganizationId, error, stackTrace),
            _ => validate.Fault(jobId, error, stackTrace)
        };

    public Task PlanCompleted(JobAuthorization auth, Guid jobId, PlanCompletedData data) =>
        auth.Family switch
        {
            JobSagaFamily.SplitMigrate =>
                splitPlan.Complete(jobId, auth.OrganizationId, data.TotalChangedCount),
            JobSagaFamily.TransferMigrate =>
                transferStep.Complete<TransferPlanCompleted>(
                    jobId, auth.OrganizationId, c => c.TotalChangedCount = data.TotalChangedCount),
            _ => plan.Complete(jobId, data)
        };

    public Task PlanFaulted(
        JobAuthorization auth, Guid jobId, string? error, string? stackTrace, PolicyOutcome? policyOutcome) =>
        auth.Family switch
        {
            JobSagaFamily.SplitMigrate => splitPlan.Fault(jobId, auth.OrganizationId, error, stackTrace),
            JobSagaFamily.TransferMigrate =>
                transferStep.Fault<TransferPlanFaulted>(jobId, auth.OrganizationId, error, stackTrace),
            _ => plan.Fault(jobId, error, stackTrace, policyOutcome)
        };
}
