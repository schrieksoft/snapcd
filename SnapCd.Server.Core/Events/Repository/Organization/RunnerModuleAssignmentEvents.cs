using SnapCd.Contracts.Dto.RunnerModuleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class RunnerModuleAssignmentCreatedEvent : CreatedEvent<RunnerModuleAssignmentReadDto>;

public class RunnerModuleAssignmentUpdatedEvent : UpdatedEvent<RunnerModuleAssignmentReadDto>;

public class RunnerModuleAssignmentDeletedEvent : DeletedEvent<RunnerModuleAssignmentReadDto>;
