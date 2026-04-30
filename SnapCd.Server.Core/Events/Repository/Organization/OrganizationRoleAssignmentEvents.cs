using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class OrganizationRoleAssignmentCreatedEvent : CreatedEvent<OrganizationRoleAssignmentReadDto>;

public class OrganizationRoleAssignmentUpdatedEvent : UpdatedEvent<OrganizationRoleAssignmentReadDto>;

public class OrganizationRoleAssignmentDeletedEvent : DeletedEvent<OrganizationRoleAssignmentReadDto>;
