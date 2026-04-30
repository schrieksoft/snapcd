using SnapCd.Contracts.Dto.ServicePrincipals;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ServicePrincipalCreatedEvent : CreatedEvent<ServicePrincipalReadDto>;

public class ServicePrincipalUpdatedEvent : UpdatedEvent<ServicePrincipalReadDto>;

public class ServicePrincipalDeletedEvent : DeletedEvent<ServicePrincipalReadDto>;
