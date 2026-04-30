using SnapCd.Contracts.Dto.ModuleHooks;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleHookCreatedEvent : CreatedEvent<ModuleHookReadDto>;
public class ModuleHookUpdatedEvent : UpdatedEvent<ModuleHookReadDto>;
public class ModuleHookDeletedEvent : DeletedEvent<ModuleHookReadDto>;
