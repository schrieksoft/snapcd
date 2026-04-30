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