using SnapCd.Contracts.Dto.Runners;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class RunnerCreatedEvent : CreatedEvent<RunnerReadDto>;

public class RunnerUpdatedEvent : UpdatedEvent<RunnerReadDto>;

public class RunnerDeletedEvent : DeletedEvent<RunnerReadDto>;
