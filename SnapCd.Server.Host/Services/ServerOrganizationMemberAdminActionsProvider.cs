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
