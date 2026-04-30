using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleInputFromOutputSetCreatedEvent : CreatedEvent<ModuleInputFromOutputSetReadDto>;

public class ModuleInputFromOutputSetUpdatedEvent : UpdatedEvent<ModuleInputFromOutputSetReadDto>;

public class ModuleInputFromOutputSetDeletedEvent : DeletedEvent<ModuleInputFromOutputSetReadDto>;