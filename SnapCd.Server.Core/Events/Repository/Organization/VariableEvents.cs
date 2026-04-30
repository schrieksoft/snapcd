using SnapCd.Contracts.Dto.Variables;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class InputCreatedEvent : CreatedEvent<VariableReadDto>;

public class InputUpdatedEvent : UpdatedEvent<VariableReadDto>;

public class InputDeletedEvent : DeletedEvent<VariableReadDto>;
