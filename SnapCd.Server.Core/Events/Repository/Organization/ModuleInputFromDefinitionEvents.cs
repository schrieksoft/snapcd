using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleInputFromDefinitionCreatedEvent : CreatedEvent<ModuleInputFromDefinitionReadDto>;

public class ModuleInputFromDefinitionUpdatedEvent : UpdatedEvent<ModuleInputFromDefinitionReadDto>;

public class ModuleInputFromDefinitionDeletedEvent : DeletedEvent<ModuleInputFromDefinitionReadDto>;