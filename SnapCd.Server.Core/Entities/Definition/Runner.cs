using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class Runner : AuditBase, IEntity, IOrganizationChild, ICreationTrackable
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ServicePrincipalId { get; set; }

    [MaxLength(255)] public string Name { get; set; } = null!;

    public bool IsDisabled { get; set; }

    public bool AllowMultipleInstances { get; set; }

    // Navigation properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual ServicePrincipal ServicePrincipal { get; set; } = null!;

    public List<Module> Modules { get; set; } = null!;

    public List<RunnerModuleAssignment> RunnerModuleAssignments { get; set; } = null!;

    public List<RunnerNamespaceAssignment> RunnerNamespaceAssignments { get; set; } = null!;

    public List<RunnerStackAssignment> RunnerStackAssignments { get; set; } = null!;

    public List<SourceRefresherPreselection> SourceRefresherPreselections { get; set; } = null!;

    public List<RunnerRoleAssignment> RunnerRoleAssignments { get; set; } = null!;

    public bool IsAssignedToAllModules { get; set; }

    public Guid ParentId()
    {
        return OrganizationId;
    }
}