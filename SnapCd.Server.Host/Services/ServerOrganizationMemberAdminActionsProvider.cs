// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Components;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Admin;
using SnapCd.Server.Host.UI.Dashboard.Components.Users;

namespace SnapCd.Server.Host.Services;

public class ServerOrganizationMemberAdminActionsProvider : IOrganizationMemberAdminActionsProvider
{
    public RenderFragment? RenderRowActions(
        OrganizationUser orgUser,
        Guid organizationId,
        EventCallback onActionCompleted) =>
        builder =>
        {
            builder.OpenComponent<ServerMemberAdminActions>(0);
            builder.AddAttribute(1, nameof(ServerMemberAdminActions.OrgUser), orgUser);
            builder.AddAttribute(2, nameof(ServerMemberAdminActions.OrganizationId), organizationId);
            builder.AddAttribute(3, nameof(ServerMemberAdminActions.OnActionCompleted), onActionCompleted);
            builder.CloseComponent();
        };
}
