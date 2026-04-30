using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class GroupNamespaceRoleAssignmentCreatedEvent : CreatedEvent<GroupNamespaceRoleAssignmentReadDto>;

public class GroupNamespaceRoleAssignmentUpdatedEvent : UpdatedEvent<GroupNamespaceRoleAssignmentReadDto>;

public class GroupNamespaceRoleAssignmentDeletedEvent : DeletedEvent<GroupNamespaceRoleAssignmentReadDto>;