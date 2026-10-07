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
/// A role on a Runner a principal holds because it was supplied to a scope they can read, rather
/// than because someone granted it. Maintained by trigger; nothing writes a row here by hand.
/// </summary>
public class DerivedRunnerRoleAssignment
{
    public Guid RunnerId { get; set; }

    public Guid PrincipalId { get; set; }

    public Guid OrganizationId { get; set; }

    public RunnerRole RoleName { get; set; }

    public PrincipalDiscriminator PrincipalDiscriminator { get; set; }
}
