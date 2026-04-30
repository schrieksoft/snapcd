using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class UserModuleRoleAssignmentCreatedEvent : CreatedEvent<UserModuleRoleAssignmentReadDto>;

public class UserModuleRoleAssignmentUpdatedEvent : UpdatedEvent<UserModuleRoleAssignmentReadDto>;

public class UserModuleRoleAssignmentDeletedEvent : DeletedEvent<UserModuleRoleAssignmentReadDto>;
