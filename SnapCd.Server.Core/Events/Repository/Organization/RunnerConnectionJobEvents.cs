using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class RunnerConnectionJobCreatedEvent : CreatedEvent<RunnerConnectionJobReadDto>;

public class RunnerConnectionJobUpdatedEvent : UpdatedEvent<RunnerConnectionJobReadDto>;

public class RunnerConnectionJobDeletedEvent : DeletedEvent<RunnerConnectionJobReadDto>;