using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ServicePrincipalStackRoleAssignmentCreatedEvent : CreatedEvent<ServicePrincipalStackRoleAssignmentReadDto>;

public class ServicePrincipalStackRoleAssignmentUpdatedEvent : UpdatedEvent<ServicePrincipalStackRoleAssignmentReadDto>;

public class ServicePrincipalStackRoleAssignmentDeletedEvent : DeletedEvent<ServicePrincipalStackRoleAssignmentReadDto>;
