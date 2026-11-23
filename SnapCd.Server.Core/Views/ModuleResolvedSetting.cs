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
/// The value each inheritable setting resolves to for one Module: its own value, or the nearest
/// one set on a Namespace above it, as far as the pins on those Namespaces allow. Null where
/// nothing sets it, so each reader keeps its own default. Maintained by triggers.
/// </summary>
public class ModuleResolvedSetting
{
    public Guid ModuleId { get; set; }

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
    public OutputRestrictionExemption? OutputRestrictionExemption { get; set; }

    /// <summary>The Namespace the exemption was set on, or the Module's own where the Module set it. "Own Namespace" means this one.</summary>
    public Guid? OutputRestrictionExemptionNamespaceId { get; set; }
}
