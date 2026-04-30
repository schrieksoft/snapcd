using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class UserOrganizationRoleAssignmentCreatedEvent : CreatedEvent<UserOrganizationRoleAssignmentReadDto>;

public class UserOrganizationRoleAssignmentUpdatedEvent : UpdatedEvent<UserOrganizationRoleAssignmentReadDto>;

public class UserOrganizationRoleAssignmentDeletedEvent : DeletedEvent<UserOrganizationRoleAssignmentReadDto>;
