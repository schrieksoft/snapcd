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
