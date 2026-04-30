using SnapCd.Contracts.Dto.ModuleBackendConfigs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleBackendConfigCreatedEvent : CreatedEvent<ModuleBackendConfigReadDto>;

public class ModuleBackendConfigUpdatedEvent : UpdatedEvent<ModuleBackendConfigReadDto>;

public class ModuleBackendConfigDeletedEvent : DeletedEvent<ModuleBackendConfigReadDto>;