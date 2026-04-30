using SnapCd.Contracts.Dto.RunnerNamespaceAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class RunnerNamespaceAssignmentCreatedEvent : CreatedEvent<RunnerNamespaceAssignmentReadDto>;

public class RunnerNamespaceAssignmentUpdatedEvent : UpdatedEvent<RunnerNamespaceAssignmentReadDto>;

public class RunnerNamespaceAssignmentDeletedEvent : DeletedEvent<RunnerNamespaceAssignmentReadDto>;
