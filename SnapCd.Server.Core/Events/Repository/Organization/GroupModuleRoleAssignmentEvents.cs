using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class GroupModuleRoleAssignmentCreatedEvent : CreatedEvent<GroupModuleRoleAssignmentReadDto>;

public class GroupModuleRoleAssignmentUpdatedEvent : UpdatedEvent<GroupModuleRoleAssignmentReadDto>;

public class GroupModuleRoleAssignmentDeletedEvent : DeletedEvent<GroupModuleRoleAssignmentReadDto>;