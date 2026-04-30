using System.ComponentModel.DataAnnotations.Schema;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;

public class ServicePrincipalSystemRoleAssignment : AuditBase, ISystemRoleAssignment, ISystemEntity
{
    public Guid Id { get; set; }

    public Guid ServicePrincipalId { get; set; }

    public ServicePrincipal ServicePrincipal { get; set; } = null!;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public Guid PrincipalId { get; set; }

    public SystemRole RoleName { get; set; }
}