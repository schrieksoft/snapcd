using SnapCd.Contracts;

namespace SnapCd.Server.Core.Misc.Helpers;

public class PermissionMap
{
    public List<OrganizationRole> OrganizationRoles { get; set; } = new();
    public List<StackRole> StackRoles { get; set; } = new();
    public List<NamespaceRole> NamespaceRoles { get; set; } = new();
    public List<ModuleRole> ModuleRoles { get; set; } = new();
}