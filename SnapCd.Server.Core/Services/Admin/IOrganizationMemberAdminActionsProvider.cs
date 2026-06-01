// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Components;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Admin;

/// <summary>
/// Returns the per-row admin-action fragment rendered for each member in the org users page,
/// or null when the edition has no admin row actions.
/// </summary>
public interface IOrganizationMemberAdminActionsProvider
{
    RenderFragment? RenderRowActions(
        OrganizationUser orgUser,
        Guid organizationId,
        EventCallback onActionCompleted);
}
