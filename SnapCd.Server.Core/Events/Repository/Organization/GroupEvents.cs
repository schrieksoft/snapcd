using SnapCd.Contracts.Dto.Groups;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class GroupCreatedEvent : CreatedEvent<GroupReadDto>;

public class GroupUpdatedEvent : UpdatedEvent<GroupReadDto>;

public class GroupDeletedEvent : DeletedEvent<GroupReadDto>;