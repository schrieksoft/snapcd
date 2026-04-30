using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceRoleAssignmentCreatedEvent : CreatedEvent<NamespaceRoleAssignmentReadDto>;

public class NamespaceRoleAssignmentUpdatedEvent : UpdatedEvent<NamespaceRoleAssignmentReadDto>;
public class NamespaceRoleAssignmentDeletedEvent : DeletedEvent<NamespaceRoleAssignmentReadDto>;