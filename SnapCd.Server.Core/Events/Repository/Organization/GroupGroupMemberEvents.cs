using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class GroupGroupMemberCreatedEvent : CreatedEvent<GroupGroupMemberReadDto>;

public class GroupGroupMemberUpdatedEvent : UpdatedEvent<GroupGroupMemberReadDto>;
public class GroupGroupMemberDeletedEvent : DeletedEvent<GroupGroupMemberReadDto>;