// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Contracts;

namespace SnapCd.Server.Core.Views;

/// <summary>
/// A Namespace's defaults as its subtree sees them: the nearest Namespace that sets a value, unless
/// one further up pins it. Read-only, backed by a view over NamespaceAncestors.
/// </summary>
public class NamespaceResolvedDefault
{
    public Guid NamespaceId { get; set; }

    public Guid OrganizationId { get; set; }

    public bool? CleanInitEnabled { get; set; }
    public bool? TriggerPathFilterEnabled { get; set; }
    public int? ApplyApprovalThreshold { get; set; }
    public int? DestroyApprovalThreshold { get; set; }
    public int? StateMigrationApprovalThreshold { get; set; }
    public int? ApprovalTimeoutMinutes { get; set; }
    public StateManagementEngine? Engine { get; set; }
    public bool? DriftCheckEnabled { get; set; }
    public int? DriftCheckIntervalMinutes { get; set; }
    public OutputSetDownstreamRestriction? OutputSetDownstreamRestriction { get; set; }
}
