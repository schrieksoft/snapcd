using SnapCd.Contracts.Dto.VariableSets;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class VariableSetCreatedEvent : CreatedEvent<VariableSetReadDto>;

public class VariableSetUpdatedEvent : UpdatedEvent<VariableSetReadDto>;

public class VariableSetDeletedEvent : DeletedEvent<VariableSetReadDto>;
