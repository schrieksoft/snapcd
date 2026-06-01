// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Constants;

public static class RunnerEndpoints
{
    public const string Init = "Init";
    public const string GetModule = "GetModule";
    public const string Validate = "Validate";
    public const string Variables = "Input";
    public const string GetDefinitiveRevision = "GetDefinitiveRevision";
    public const string Plan = "Plan";
    public const string PlanDestroy = "PlanDestroy";
    public const string ApplyFromPlan = "ApplyFromPlan";
    public const string DestroyFromPlan = "DestroyFromPlan";
    public const string Output = "Output";
    public const string SourceRefresh = "SourceRefresh";
    public const string CancelKill = "CancelKill";
    public const string CancelGraceful = "CancelGraceful";
}