using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class GroupRunnerRoleAssignmentCreatedEvent : CreatedEvent<GroupRunnerRoleAssignmentReadDto>;
public class GroupRunnerRoleAssignmentUpdatedEvent : UpdatedEvent<GroupRunnerRoleAssignmentReadDto>;

public class GroupRunnerRoleAssignmentDeletedEvent : DeletedEvent<GroupRunnerRoleAssignmentReadDto>;