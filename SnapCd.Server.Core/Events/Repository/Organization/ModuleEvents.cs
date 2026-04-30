using SnapCd.Contracts.Dto.Modules;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleCreatedEvent : CreatedEvent<ModuleReadDto>;

public class ModuleUpdatedEvent : UpdatedEvent<ModuleReadDto>;

public class ModuleDeletedEvent : DeletedEvent<ModuleReadDto>;