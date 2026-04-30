using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleInputFromNamespaceCreatedEvent : CreatedEvent<ModuleInputFromNamespaceReadDto>;

public class ModuleInputFromNamespaceUpdatedEvent : UpdatedEvent<ModuleInputFromNamespaceReadDto>;

public class ModuleInputFromNamespaceDeletedEvent : DeletedEvent<ModuleInputFromNamespaceReadDto>;