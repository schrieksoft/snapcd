// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Contracts.Clients;

public class RunnerHubClient
{
    private readonly HubConnection _hubConnection;

    public RunnerHubClient(HubConnection hubConnection)
    {
        _hubConnection = hubConnection;
    }

    public async Task InvokeGetDefinitiveRevisionCompleted(Guid jobId, string definitiveRevision)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.GetDefinitiveRevisionCompleted, jobId, definitiveRevision);
    }

    public async Task InvokeGetDefinitiveRevisionCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.GetDefinitiveRevisionCancelled, jobId);
    }

    public async Task InvokeGetDefinitiveRevisionFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.GetDefinitiveRevisionFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeGetModuleCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.GetModuleCompleted, jobId);
    }

    public async Task InvokeGetModuleCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.GetModuleCancelled, jobId);
    }

    public async Task InvokeGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.GetModuleFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeInitCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.InitCompleted, jobId);
    }

    public async Task InvokeInitCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.InitCancelled, jobId);
    }

    public async Task InvokeInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.InitFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeValidateCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ValidateCompleted, jobId);
    }

    public async Task InvokePolicyValidateCompleted(Guid jobId, PolicyOutcome outcome)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PolicyValidateCompleted, jobId, outcome);
    }

    public async Task InvokePolicyValidateCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PolicyValidateCancelled, jobId);
    }

    public async Task InvokePolicyValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PolicyValidateFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeValidateCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ValidateCancelled, jobId);
    }

    public async Task InvokeValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ValidateFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokePlanEmptyVerifyCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PlanEmptyVerifyCompleted, jobId);
    }

    public async Task InvokePlanEmptyVerifyCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PlanEmptyVerifyCancelled, jobId);
    }

    public async Task InvokePlanEmptyVerifyFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PlanEmptyVerifyFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeRefactorVerifyCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RefactorVerifyCompleted, jobId);
    }

    public async Task InvokeMigrateRunCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateRunCompleted, jobId);
    }

    public async Task InvokeMigrateMapCompleted(Guid jobId, string? refactorMapHash, List<string> carvedModuleNames, int resourcesMoved)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateMapCompleted, jobId, refactorMapHash, carvedModuleNames, resourcesMoved);
    }

    public async Task InvokeMigrateProveCompleted(Guid jobId, int modulesProven, int modulesPlanningClean)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateProveCompleted, jobId, modulesProven, modulesPlanningClean);
    }

    public async Task InvokeMigrateVerifyCompleted(Guid jobId, int modulesProven, int modulesPlanningClean)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateVerifyCompleted, jobId, modulesProven, modulesPlanningClean);
    }

    public async Task InvokeRefactorVerifyCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RefactorVerifyCancelled, jobId);
    }

    public async Task InvokeRefactorVerifyFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RefactorVerifyFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMigrateMapCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateMapCancelled, jobId);
    }

    public async Task InvokeMigrateMapFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateMapFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMigrateProveCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateProveCancelled, jobId);
    }

    public async Task InvokeMigrateProveFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateProveFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMigrateRunCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateRunCancelled, jobId);
    }

    public async Task InvokeMigrateRunFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateRunFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMigrateVerifyCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateVerifyCancelled, jobId);
    }

    public async Task InvokeMigrateVerifyFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MigrateVerifyFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeVariablesCompleted(Guid jobId, VariableSetCreateDto? variableSet)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.VariablesCompleted, jobId, variableSet);
    }

    public async Task InvokeVariablesCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.VariablesCancelled, jobId);
    }

    public async Task InvokeVariablesFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.VariablesFaulted, jobId, errorMessage, stackTrace);
    }

    // Plan
    public async Task InvokePlanCompleted(Guid jobId, PlanCompletedData data)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PlanCompleted, jobId, data);
    }

    public async Task InvokePlanCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PlanCancelled, jobId);
    }

    public async Task InvokePlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome = null)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PlanFaulted, jobId, errorMessage, stackTrace, policyOutcome);
    }

    // PlanDestroy
    public async Task InvokePlanDestroyCompleted(Guid jobId, PlanCompletedData data)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PlanDestroyCompleted, jobId, data);
    }

    public async Task InvokePlanDestroyCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PlanDestroyCancelled, jobId);
    }

    public async Task InvokePlanDestroyFaulted(Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome = null)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.PlanDestroyFaulted, jobId, errorMessage, stackTrace, policyOutcome);
    }

    // ApplyFromPlan (flat parameters)
    public async Task InvokeApplyFromPlanCompleted(Guid jobId, int actualResourceCount)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyFromPlanCompleted, jobId, actualResourceCount);
    }

    public async Task InvokeApplyFromPlanCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyFromPlanCancelled, jobId);
    }

    public async Task InvokeApplyFromPlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, int? actualResourceCount)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyFromPlanFaulted, jobId, errorMessage, stackTrace, actualResourceCount);
    }

    // DestroyFromPlan (flat parameters)
    public async Task InvokeDestroyFromPlanCompleted(Guid jobId, int actualResourceCount)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyFromPlanCompleted, jobId, actualResourceCount);
    }

    public async Task InvokeDestroyFromPlanCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyFromPlanCancelled, jobId);
    }

    public async Task InvokeDestroyFromPlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, int? actualResourceCount)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyFromPlanFaulted, jobId, errorMessage, stackTrace, actualResourceCount);
    }

    // Output
    public async Task InvokeOutputCompleted(Guid jobId, OutputSetCreateDto? outputSet)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.OutputCompleted, jobId, outputSet);
    }

    public async Task InvokeOutputCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.OutputCancelled, jobId);
    }

    public async Task InvokeOutputFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.OutputFaulted, jobId, errorMessage, stackTrace);
    }

    // SourceRefresh (stateless - no JobId, matched by source parameters)
    public async Task InvokeSourceRefreshCompleted(
        string sourceUrl,
        string sourceRevision,
        SourceType sourceType,
        SourceRevisionType sourceRevisionType,
        string definitiveRevision)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SourceRefreshCompleted,
            sourceUrl, sourceRevision, sourceType, sourceRevisionType, definitiveRevision);
    }

    public async Task InvokeSourceRefreshCompletedV2(
        string sourceUrl,
        string sourceRevision,
        SourceType sourceType,
        SourceRevisionType sourceRevisionType,
        SourceRefreshResult result)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SourceRefreshCompletedV2,
            sourceUrl, sourceRevision, sourceType, sourceRevisionType, result);
    }

    public async Task InvokeSourceRefreshFaulted(
        string sourceUrl,
        string sourceRevision,
        SourceType sourceType,
        SourceRevisionType sourceRevisionType,
        string? errorMessage,
        string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SourceRefreshFaulted,
            sourceUrl, sourceRevision, sourceType, sourceRevisionType, errorMessage, stackTrace);
    }

    // Heartbeat
    public async Task InvokeHeartbeatResponse(string requestId, bool isActive)
    {
        await _hubConnection.InvokeAsync("HeartbeatResponse", requestId, isActive);
    }

    public async Task InvokeReportRunningTask(Guid jobId, string taskName, Guid runnerId, string? runnerInstanceName)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ReportRunningTask, jobId, taskName, runnerId, runnerInstanceName);
    }

    // Cancellation
    public async Task InvokeCancelKillCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.CancelKillCompleted, jobId);
    }

    public async Task InvokeCancelGracefulCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.CancelGracefulCompleted, jobId);
    }
}