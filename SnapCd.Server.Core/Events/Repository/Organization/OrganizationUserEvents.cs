using SnapCd.Server.Core.Dtos.OrganizationUsers;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class OrganizationUserCreatedEvent : CreatedEvent<OrganizationUserReadDto>;

public class OrganizationUserUpdatedEvent : UpdatedEvent<OrganizationUserReadDto>;

public class OrganizationUserDeletedEvent : DeletedEvent<OrganizationUserReadDto>;
