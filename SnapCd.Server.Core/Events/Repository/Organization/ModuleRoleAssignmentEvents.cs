using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleRoleAssignmentCreatedEvent : CreatedEvent<ModuleRoleAssignmentReadDto>;

public class ModuleRoleAssignmentUpdatedEvent : UpdatedEvent<ModuleRoleAssignmentReadDto>;

public class ModuleRoleAssignmentDeletedEvent : DeletedEvent<ModuleRoleAssignmentReadDto>;