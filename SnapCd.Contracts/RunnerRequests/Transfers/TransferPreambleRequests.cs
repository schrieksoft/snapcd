// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests.Transfers;

/// <summary>
/// A transfer runs the ordinary preamble against two Modules under one job, so each request and
/// each reply names the Module it belongs to. The payloads are otherwise the ordinary ones.
/// </summary>
public class TransferGetModuleRequestBase : GetModuleRequestBase
{
    public Guid ModuleId { get; set; }
}

public class TransferInitRequestBase : InitRequestBase
{
    public Guid ModuleId { get; set; }
}

public class TransferValidateRequestBase : ValidateRequestBase
{
    public Guid ModuleId { get; set; }
}

public class TransferPlanRequestBase : PlanRequestBase
{
    public Guid ModuleId { get; set; }
}
