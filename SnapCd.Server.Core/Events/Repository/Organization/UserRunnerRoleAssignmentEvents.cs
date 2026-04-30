using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class UserRunnerRoleAssignmentCreatedEvent : CreatedEvent<UserRunnerRoleAssignmentReadDto>;

public class UserRunnerRoleAssignmentUpdatedEvent : UpdatedEvent<UserRunnerRoleAssignmentReadDto>;

public class UserRunnerRoleAssignmentDeletedEvent : DeletedEvent<UserRunnerRoleAssignmentReadDto>;
