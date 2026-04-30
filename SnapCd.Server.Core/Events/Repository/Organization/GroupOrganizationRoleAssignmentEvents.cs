using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class GroupOrganizationRoleAssignmentCreatedEvent : CreatedEvent<GroupOrganizationRoleAssignmentReadDto>;

public class GroupOrganizationRoleAssignmentUpdatedEvent : UpdatedEvent<GroupOrganizationRoleAssignmentReadDto>;

public class GroupOrganizationRoleAssignmentDeletedEvent : DeletedEvent<GroupOrganizationRoleAssignmentReadDto>;