using SnapCd.Contracts.Dto.Stacks;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class StackCreatedEvent : CreatedEvent<StackReadDto>;

public class StackUpdatedEvent : UpdatedEvent<StackReadDto>;

public class StackDeletedEvent : DeletedEvent<StackReadDto>;
