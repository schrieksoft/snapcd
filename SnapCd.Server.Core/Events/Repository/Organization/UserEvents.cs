using SnapCd.Server.Core.Dtos.Users;
using SnapCd.Server.Core.Events.Repository.System.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class UserCreatedEvent : SystemCreatedEvent<UserReadDto>;

public class UserUpdatedEvent : SystemUpdatedEvent<UserReadDto>;

public class UserDeletedEvent : SystemDeletedEvent<UserReadDto>;
