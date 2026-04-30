using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class GroupStackRoleAssignmentCreatedEvent : CreatedEvent<GroupStackRoleAssignmentReadDto>;

public class GroupStackRoleAssignmentUpdatedEvent : UpdatedEvent<GroupStackRoleAssignmentReadDto>;

public class GroupStackRoleAssignmentDeletedEvent : DeletedEvent<GroupStackRoleAssignmentReadDto>;