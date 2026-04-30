using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class StackRoleAssignmentCreatedEvent : CreatedEvent<StackRoleAssignmentDto>;

public class StackRoleAssignmentUpdatedEvent : UpdatedEvent<StackRoleAssignmentDto>;

public class StackRoleAssignmentDeletedEvent : DeletedEvent<StackRoleAssignmentDto>;
