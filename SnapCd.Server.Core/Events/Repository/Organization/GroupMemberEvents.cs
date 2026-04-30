using SnapCd.Contracts.Dto.GroupMembers.Base;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class GroupMemberCreatedEvent : CreatedEvent<GroupMemberReadDto>;

public class GroupMemberUpdatedEvent : UpdatedEvent<GroupMemberReadDto>;

public class GroupMemberDeletedEvent : DeletedEvent<GroupMemberReadDto>;