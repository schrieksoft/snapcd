using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class RunnerConnectionCreatedEvent : CreatedEvent<RunnerConnectionReadDto>;

public class RunnerConnectionUpdatedEvent : UpdatedEvent<RunnerConnectionReadDto>;

public class RunnerConnectionDeletedEvent : DeletedEvent<RunnerConnectionReadDto>;