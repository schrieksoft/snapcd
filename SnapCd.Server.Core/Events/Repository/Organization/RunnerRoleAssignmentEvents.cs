using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class RunnerRoleAssignmentCreatedEvent : CreatedEvent<RunnerRoleAssignmentReadDto>;

public class RunnerRoleAssignmentUpdatedEvent : UpdatedEvent<RunnerRoleAssignmentReadDto>;

public class RunnerRoleAssignmentDeletedEvent : DeletedEvent<RunnerRoleAssignmentReadDto>;
