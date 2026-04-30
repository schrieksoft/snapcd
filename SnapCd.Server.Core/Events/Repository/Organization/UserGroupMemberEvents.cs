using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class UserGroupMemberCreatedEvent : CreatedEvent<UserGroupMemberReadDto>;

public class UserGroupMemberUpdatedEvent : UpdatedEvent<UserGroupMemberReadDto>;

public class UserGroupMemberDeletedEvent : DeletedEvent<UserGroupMemberReadDto>;
