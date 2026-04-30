using SnapCd.Contracts.Dto.RunnerStackAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class RunnerStackAssignmentCreatedEvent : CreatedEvent<RunnerStackAssignmentReadDto>;

public class RunnerStackAssignmentUpdatedEvent : UpdatedEvent<RunnerStackAssignmentReadDto>;

public class RunnerStackAssignmentDeletedEvent : DeletedEvent<RunnerStackAssignmentReadDto>;
