// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.RunnerRequests;

namespace SnapCd.Contracts.Endpoints;

/// <summary>
/// The steps a runner must service for an apply.
///
/// The server dispatches by member name and the runner implements the interface, so a step
/// the server can send is one the runner has a handler for.
///
/// The runner runs one checkout and one init whatever asked for it, but each family declares its
/// own, because the endpoint is what tells the runner which family to answer on.
/// </summary>
public interface IApplyEndpoints
{
    Task ApplyCancelKill(CancelKillRequest request);

    Task ApplyFromPlan(ApplyFromPlanRequestBase request);

    Task ApplyGetDefinitiveRevision(GetDefinitiveRevisionRequest request);

    Task ApplyGetModule(GetModuleRequestBase request);

    Task ApplyInit(InitRequestBase request);

    Task ApplyOutput(OutputRequestBase request);

    Task ApplyPlan(PlanRequestBase request);

    Task ApplyPolicyValidate(PolicyValidateRequestBase request);

    Task ApplyValidate(ValidateRequestBase request);

    Task ApplyVariables(VariablesRequestBase request);
}
