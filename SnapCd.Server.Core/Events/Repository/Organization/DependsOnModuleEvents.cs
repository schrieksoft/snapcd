using SnapCd.Contracts.Dto.DependsOnModules;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class DependsOnModuleCreatedEvent : CreatedEvent<DependsOnModuleReadDto>;

public class DependsOnModuleUpdatedEvent : UpdatedEvent<DependsOnModuleReadDto>;

public class DependsOnModuleDeletedEvent : DeletedEvent<DependsOnModuleReadDto>;