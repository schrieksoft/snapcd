using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class OutputCreatedEvent : CreatedEvent<OutputReadDto>;

public class OutputUpdatedEvent : UpdatedEvent<OutputReadDto>;


public class OutputDeletedEvent : DeletedEvent<OutputReadDto>;
