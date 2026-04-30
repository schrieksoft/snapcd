using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class OutputSetCreatedEvent : CreatedEvent<OutputSetReadDto>;

public class OutputSetUpdatedEvent : UpdatedEvent<OutputSetReadDto>;

public class OutputSetDeletedEvent : DeletedEvent<OutputSetReadDto>;

