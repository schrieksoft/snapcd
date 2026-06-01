// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Views;

public class RecursiveGroupMember
{
    // Root group (starting point of recursion)
    public Guid RootGroupId { get; set; }
    public Guid RootOrganizationId { get; set; }
    public string RootGroupName { get; set; } = null!;

    // Current group (parent group in the hierarchy)
    public Guid GroupId { get; set; }
    public Guid OrganizationId { get; set; }
    public string GroupName { get; set; } = null!;

    // Recursion metadata
    public int Depth { get; set; }
    public string VisitedPath { get; set; } = null!;

    // Navigation properties
    public Group RootGroup { get; set; } = null!;
    public Group Group { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}