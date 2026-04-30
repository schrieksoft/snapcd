using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class UserNamespaceRoleAssignmentCreatedEvent : CreatedEvent<UserNamespaceRoleAssignmentReadDto>;

public class UserNamespaceRoleAssignmentUpdatedEvent : UpdatedEvent<UserNamespaceRoleAssignmentReadDto>;

public class UserNamespaceRoleAssignmentDeletedEvent : DeletedEvent<UserNamespaceRoleAssignmentReadDto>;
