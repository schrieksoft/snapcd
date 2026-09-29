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
using SnapCd.Contracts.RunnerRequests.StateMigrations;
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



    public async Task InvokePlanEmptyVerifyCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitPlanEmptyVerifyCompleted, jobId);
    }

    public async Task InvokePlanEmptyVerifyCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitPlanEmptyVerifyCancelled, jobId);
    }

    public async Task InvokePlanEmptyVerifyFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitPlanEmptyVerifyFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeRefactorValidateCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitRefactorValidateCompleted, jobId);
    }

    public async Task InvokeRefactorValidateCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitRefactorValidateCancelled, jobId);
    }

    public async Task InvokeRefactorValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitRefactorValidateFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeRefactorDiffCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitRefactorDiffCompleted, jobId);
    }

    public async Task InvokeMigrateRunCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateRunCompleted, jobId);
    }









    public async Task InvokeTransferMigrateMapCompleted(
        Guid jobId, Guid moduleId, List<string> needsOutputs)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.TransferMigrateMapCompleted, jobId, moduleId, needsOutputs);
    }

    public async Task InvokeTransferMigrateMapFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.TransferMigrateMapFaulted, jobId, moduleId, errorMessage, stackTrace);
    }

    public async Task InvokeTransferMigrateProveCompleted(
        Guid jobId, Guid moduleId, int exitCode, Dictionary<string, string> outputs, string? verdict)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.TransferMigrateProveCompleted,
            jobId, moduleId, exitCode, outputs, verdict);
    }

    public async Task InvokeTransferMigrateProveFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.TransferMigrateProveFaulted, jobId, moduleId, errorMessage, stackTrace);
    }

    public async Task InvokeTransferMigrateRunCompleted(
        Guid jobId, Guid moduleId, List<string> transferredAddresses, bool gaveUp)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.TransferMigrateRunCompleted, jobId, moduleId, transferredAddresses, gaveUp);
    }

    public async Task InvokeTransferMigrateRunFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.TransferMigrateRunFaulted, jobId, moduleId, errorMessage, stackTrace);
    }

    public async Task InvokeStateMoveCompleted(Guid jobId, List<StateAddressResult> results)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.StateMoveCompleted, jobId, results);
    }

    public async Task InvokeStateMoveFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.StateMoveFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMoveDryRunCompleted(Guid jobId, List<StateAddressResult> results)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MoveDryRunCompleted, jobId, results);
    }

    public async Task InvokeMoveDryRunFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MoveDryRunFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeRemoveDryRunCompleted(Guid jobId, List<StateAddressResult> results)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RemoveDryRunCompleted, jobId, results);
    }

    public async Task InvokeRemoveDryRunFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RemoveDryRunFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeImportPreCheckCompleted(Guid jobId, List<StateAddressResult> results)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ImportPreCheckCompleted, jobId, results);
    }

    public async Task InvokeImportPreCheckFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ImportPreCheckFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeStateListFilteredCompleted(Guid jobId, List<StateAddressResult> results)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.StateListFilteredCompleted, jobId, results);
    }

    public async Task InvokeStateListFilteredFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.StateListFilteredFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeTransferOutputsCompleted(
        Guid jobId, Guid moduleId, OutputSetCreateDto? outputSet)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.TransferOutputsCompleted, jobId, moduleId, outputSet);
    }

    public async Task InvokeTransferOutputsFaulted(
        Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.TransferOutputsFaulted, jobId, moduleId, errorMessage, stackTrace);
    }

    public async Task InvokeTransferMigrateVerifyCompleted(Guid jobId, Guid moduleId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.TransferMigrateVerifyCompleted, jobId, moduleId);
    }

    public async Task InvokeTransferMigrateVerifyFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.TransferMigrateVerifyFaulted, jobId, moduleId, errorMessage, stackTrace);
    }

    public async Task InvokeMigrateMapCompleted(Guid jobId, string? refactorMapHash, List<string> carvedModuleNames, int resourcesMoved)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateMapCompleted, jobId, refactorMapHash, carvedModuleNames, resourcesMoved);
    }

    public async Task InvokeMigrateProveCompleted(Guid jobId, int modulesProven, int modulesPlanningClean)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateProveCompleted, jobId, modulesProven, modulesPlanningClean);
    }

    public async Task InvokeMigrateVerifyCompleted(Guid jobId, int modulesProven, int modulesPlanningClean)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateVerifyCompleted, jobId, modulesProven, modulesPlanningClean);
    }

    public async Task InvokeRefactorDiffCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitRefactorDiffCancelled, jobId);
    }

    public async Task InvokeRefactorDiffFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitRefactorDiffFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMigrateMapCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateMapCancelled, jobId);
    }

    public async Task InvokeMigrateMapFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateMapFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMigrateProveCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateProveCancelled, jobId);
    }

    public async Task InvokeMigrateProveFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateProveFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMigrateRunCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateRunCancelled, jobId);
    }

    public async Task InvokeMigrateRunFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateRunFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMigrateVerifyCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateVerifyCancelled, jobId);
    }

    public async Task InvokeMigrateVerifyFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitMigrateVerifyFaulted, jobId, errorMessage, stackTrace);
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
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyPlanCompleted, jobId, data);
    }

    public async Task InvokePlanCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyPlanCancelled, jobId);
    }

    public async Task InvokePlanFaulted(Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome = null)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyPlanFaulted, jobId, errorMessage, stackTrace, policyOutcome);
    }

    // PlanDestroy
    public async Task InvokePlanDestroyCompleted(Guid jobId, PlanCompletedData data)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyPlanCompleted, jobId, data);
    }

    public async Task InvokePlanDestroyCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyPlanCancelled, jobId);
    }

    public async Task InvokePlanDestroyFaulted(Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome = null)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyPlanFaulted, jobId, errorMessage, stackTrace, policyOutcome);
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

    // Cancellation: one wrapper per family, as every other step has

    public async Task InvokeApplyCancelKillCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyCancelKillCompleted, jobId);
    }

    public async Task InvokeDestroyCancelKillCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyCancelKillCompleted, jobId);
    }

    public async Task InvokeSplitCancelKillCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitCancelKillCompleted, jobId);
    }

    // One wrapper per family per outcome, so a task is handed the exact endpoint to answer on
    // and a wrong one cannot compile.

    public async Task InvokeStateListFilteredGetModuleCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.StateListFilteredGetModuleCompleted, jobId);
    }

    public async Task InvokeStateListFilteredGetModuleCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.StateListFilteredGetModuleCancelled, jobId);
    }

    public async Task InvokeStateListFilteredGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.StateListFilteredGetModuleFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeStateListFilteredInitCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.StateListFilteredInitCompleted, jobId);
    }

    public async Task InvokeStateListFilteredInitCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.StateListFilteredInitCancelled, jobId);
    }

    public async Task InvokeStateListFilteredInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.StateListFilteredInitFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMoveGetModuleCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MoveGetModuleCompleted, jobId);
    }

    public async Task InvokeMoveGetModuleCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MoveGetModuleCancelled, jobId);
    }

    public async Task InvokeMoveGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MoveGetModuleFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeMoveInitCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MoveInitCompleted, jobId);
    }

    public async Task InvokeMoveInitCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MoveInitCancelled, jobId);
    }

    public async Task InvokeMoveInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.MoveInitFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeImportGetModuleCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ImportGetModuleCompleted, jobId);
    }

    public async Task InvokeImportGetModuleCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ImportGetModuleCancelled, jobId);
    }

    public async Task InvokeImportGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ImportGetModuleFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeImportInitCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ImportInitCompleted, jobId);
    }

    public async Task InvokeImportInitCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ImportInitCancelled, jobId);
    }

    public async Task InvokeImportInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ImportInitFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeRemoveGetModuleCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RemoveGetModuleCompleted, jobId);
    }

    public async Task InvokeRemoveGetModuleCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RemoveGetModuleCancelled, jobId);
    }

    public async Task InvokeRemoveGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RemoveGetModuleFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeRemoveInitCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RemoveInitCompleted, jobId);
    }

    public async Task InvokeRemoveInitCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RemoveInitCancelled, jobId);
    }

    public async Task InvokeRemoveInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.RemoveInitFaulted, jobId, errorMessage, stackTrace);
    }

    // Apply and destroy answer on their own endpoints, so a task is handed the exact pair to use.

    public async Task InvokeApplyGetModuleCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyGetModuleCompleted, jobId);
    }

    public async Task InvokeApplyGetModuleCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyGetModuleCancelled, jobId);
    }

    public async Task InvokeApplyGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyGetModuleFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeApplyInitCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyInitCompleted, jobId);
    }

    public async Task InvokeApplyInitCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyInitCancelled, jobId);
    }

    public async Task InvokeApplyInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyInitFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeApplyValidateCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyValidateCompleted, jobId);
    }

    public async Task InvokeApplyValidateCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyValidateCancelled, jobId);
    }

    public async Task InvokeApplyValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyValidateFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeApplyVariablesCompleted(Guid jobId, VariableSetCreateDto? variableSet)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyVariablesCompleted, jobId, variableSet);
    }

    public async Task InvokeApplyVariablesCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyVariablesCancelled, jobId);
    }

    public async Task InvokeApplyVariablesFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyVariablesFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeDestroyGetModuleCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyGetModuleCompleted, jobId);
    }

    public async Task InvokeDestroyGetModuleCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyGetModuleCancelled, jobId);
    }

    public async Task InvokeDestroyGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyGetModuleFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeDestroyInitCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyInitCompleted, jobId);
    }

    public async Task InvokeDestroyInitCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyInitCancelled, jobId);
    }

    public async Task InvokeDestroyInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyInitFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeDestroyValidateCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyValidateCompleted, jobId);
    }

    public async Task InvokeDestroyValidateCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyValidateCancelled, jobId);
    }

    public async Task InvokeDestroyValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyValidateFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeDestroyVariablesCompleted(Guid jobId, VariableSetCreateDto? variableSet)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyVariablesCompleted, jobId, variableSet);
    }

    public async Task InvokeDestroyVariablesCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyVariablesCancelled, jobId);
    }

    public async Task InvokeDestroyVariablesFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyVariablesFaulted, jobId, errorMessage, stackTrace);
    }

    // Plan and Validate answer per family, so each task is handed the pair it should use.

    public async Task InvokeApplyPlanCompleted(Guid jobId, PlanCompletedData data)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyPlanCompleted, jobId, data);
    }

    public async Task InvokeApplyPlanFaulted(
        Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.ApplyPlanFaulted, jobId, errorMessage, stackTrace, policyOutcome);
    }

    public async Task InvokeDestroyPlanCompleted(Guid jobId, PlanCompletedData data)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyPlanCompleted, jobId, data);
    }

    public async Task InvokeDestroyPlanFaulted(
        Guid jobId, string? errorMessage, string? stackTrace, PolicyOutcome? policyOutcome)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.DestroyPlanFaulted, jobId, errorMessage, stackTrace, policyOutcome);
    }

    public async Task InvokeSplitPlanCompleted(Guid jobId, PlanCompletedData data)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitPlanCompleted, jobId, data);
    }

    public async Task InvokeSplitPlanFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitPlanFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeSplitValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitValidateFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeApplyPlanCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyPlanCancelled, jobId);
    }

    public async Task InvokeDestroyPlanCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyPlanCancelled, jobId);
    }

    public async Task InvokeSplitPlanCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitPlanCancelled, jobId);
    }

    public async Task InvokeApplyGetDefinitiveRevisionCompleted(Guid jobId, string definitiveRevision)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyGetDefinitiveRevisionCompleted, jobId, definitiveRevision);
    }

    public async Task InvokeApplyGetDefinitiveRevisionCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyGetDefinitiveRevisionCancelled, jobId);
    }

    public async Task InvokeApplyGetDefinitiveRevisionFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyGetDefinitiveRevisionFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeApplyPolicyValidateCompleted(Guid jobId, PolicyOutcome outcome)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyPolicyValidateCompleted, jobId, outcome);
    }

    public async Task InvokeApplyPolicyValidateCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyPolicyValidateCancelled, jobId);
    }

    public async Task InvokeApplyPolicyValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyPolicyValidateFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeApplyOutputCompleted(Guid jobId, OutputSetCreateDto? outputSet)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyOutputCompleted, jobId, outputSet);
    }

    public async Task InvokeApplyOutputCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyOutputCancelled, jobId);
    }

    public async Task InvokeApplyOutputFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.ApplyOutputFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeDestroyGetDefinitiveRevisionCompleted(Guid jobId, string definitiveRevision)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyGetDefinitiveRevisionCompleted, jobId, definitiveRevision);
    }

    public async Task InvokeDestroyGetDefinitiveRevisionCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyGetDefinitiveRevisionCancelled, jobId);
    }

    public async Task InvokeDestroyGetDefinitiveRevisionFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyGetDefinitiveRevisionFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeDestroyPolicyValidateCompleted(Guid jobId, PolicyOutcome outcome)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyPolicyValidateCompleted, jobId, outcome);
    }

    public async Task InvokeDestroyPolicyValidateCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyPolicyValidateCancelled, jobId);
    }

    public async Task InvokeDestroyPolicyValidateFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyPolicyValidateFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeDestroyOutputCompleted(Guid jobId, OutputSetCreateDto? outputSet)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyOutputCompleted, jobId, outputSet);
    }

    public async Task InvokeDestroyOutputCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyOutputCancelled, jobId);
    }

    public async Task InvokeDestroyOutputFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.DestroyOutputFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeSplitGetModuleCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitGetModuleCompleted, jobId);
    }

    public async Task InvokeSplitGetModuleCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitGetModuleCancelled, jobId);
    }

    public async Task InvokeSplitGetModuleFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitGetModuleFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeSplitInitCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitInitCompleted, jobId);
    }

    public async Task InvokeSplitInitCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitInitCancelled, jobId);
    }

    public async Task InvokeSplitInitFaulted(Guid jobId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitInitFaulted, jobId, errorMessage, stackTrace);
    }

    public async Task InvokeSplitValidateCompleted(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitValidateCompleted, jobId);
    }

    public async Task InvokeSplitValidateCancelled(Guid jobId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.SplitValidateCancelled, jobId);
    }


    public async Task InvokeTransferGetModuleCompleted(Guid jobId, Guid moduleId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.TransferGetModuleCompleted, jobId, moduleId);
    }

    public async Task InvokeTransferGetModuleFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.TransferGetModuleFaulted, jobId, moduleId, errorMessage, stackTrace);
    }

    public async Task InvokeTransferInitCompleted(Guid jobId, Guid moduleId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.TransferInitCompleted, jobId, moduleId);
    }

    public async Task InvokeTransferInitFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.TransferInitFaulted, jobId, moduleId, errorMessage, stackTrace);
    }

    public async Task InvokeTransferValidateCompleted(Guid jobId, Guid moduleId)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.TransferValidateCompleted, jobId, moduleId);
    }

    public async Task InvokeTransferValidateFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.TransferValidateFaulted, jobId, moduleId, errorMessage, stackTrace);
    }

    public async Task InvokeTransferPlanCompleted(Guid jobId, Guid moduleId, PlanCompletedData data)
    {
        await _hubConnection.InvokeAsync(ServerEndpoints.TransferPlanCompleted, jobId, moduleId, data);
    }

    public async Task InvokeTransferPlanFaulted(Guid jobId, Guid moduleId, string? errorMessage, string? stackTrace)
    {
        await _hubConnection.InvokeAsync(
            ServerEndpoints.TransferPlanFaulted, jobId, moduleId, errorMessage, stackTrace);
    }
}