// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.Edition;

/// <summary>
/// Whether completing the invitation registration form should automatically accept the
/// org invitation in the same step. Self-Hosted: true (single-org deployment, the accept
/// step would be ceremonial). SaaS: false (users may belong to multiple orgs; explicit
/// accept-or-decline is meaningful).
/// </summary>
public interface IInvitationAutoAcceptPolicy
{
    bool AutoAcceptOnRegistration { get; }
}
