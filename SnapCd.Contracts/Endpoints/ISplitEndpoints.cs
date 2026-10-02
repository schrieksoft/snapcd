// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.SplitMigrate;

namespace SnapCd.Contracts.Endpoints;

/// <summary>
/// The steps a runner must service for a split.
///
/// The server dispatches by member name and the runner implements the interface, so a step
/// the server can send is one the runner has a handler for.
/// </summary>
public interface ISplitEndpoints
{
    Task SplitCancelKill(CancelKillRequest request);

    Task SplitGetModule(GetModuleRequestBase request);

    Task SplitInit(InitRequestBase request);

    Task SplitMigrateMap(SplitMigrateMapRequestBase request);

    Task SplitMigrateProve(SplitMigrateProveRequestBase request);

    Task SplitMigrateRun(SplitMigrateRunRequestBase request);

    Task SplitMigrateVerify(SplitMigrateVerifyRequestBase request);

    Task SplitPlan(PlanRequestBase request);

    Task SplitPlanEmptyVerify(SplitPlanEmptyVerifyRequestBase request);

    Task SplitRefactorDiff(SplitRefactorDiffRequestBase request);

    Task SplitRefactorValidate(SplitRefactorValidateRequestBase request);

    Task SplitValidate(ValidateRequestBase request);
}
