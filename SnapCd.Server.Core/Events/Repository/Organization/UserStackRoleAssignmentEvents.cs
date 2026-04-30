using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class UserStackRoleAssignmentCreatedEvent : CreatedEvent<UserStackRoleAssignmentReadDto>;

public class UserStackRoleAssignmentUpdatedEvent : UpdatedEvent<UserStackRoleAssignmentReadDto>;

public class UserStackRoleAssignmentDeletedEvent : DeletedEvent<UserStackRoleAssignmentReadDto>;
