using SnapCd.Contracts.Dto.ModuleInputs.Base;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleInputCreatedEvent : CreatedEvent<ModuleInputReadDto>;

public class ModuleInputUpdatedEvent : UpdatedEvent<ModuleInputReadDto>;

public class ModuleInputDeletedEvent : DeletedEvent<ModuleInputReadDto>;