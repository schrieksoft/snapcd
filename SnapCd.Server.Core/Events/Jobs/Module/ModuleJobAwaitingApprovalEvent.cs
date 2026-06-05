// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Jobs.Base;

namespace SnapCd.Server.Core.Events.Jobs.Module;

/// <summary>
/// Published when a <see cref="ModuleJob"/> enters the awaiting-approval state — i.e., the runner
/// has produced a plan and the job is now blocked on human approval. The Layer-1
/// <c>ApprovalRequestedCompetingConsumer</c> uses it to trigger the <c>ApprovalRecommend</c> mission.
/// </summary>
public class ModuleJobAwaitingApprovalEvent : ModuleJobEventCompletedBase;
