using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ServicePrincipalGroupMemberCreatedEvent : CreatedEvent<ServicePrincipalGroupMemberReadDto>;

public class ServicePrincipalGroupMemberUpdatedEvent : UpdatedEvent<ServicePrincipalGroupMemberReadDto>;

public class ServicePrincipalGroupMemberDeletedEvent : DeletedEvent<ServicePrincipalGroupMemberReadDto>;
