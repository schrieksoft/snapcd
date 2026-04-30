using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleInputFromOutputCreatedEvent : CreatedEvent<ModuleInputFromOutputDtoRead>;

public class ModuleInputFromOutputUpdatedEvent : UpdatedEvent<ModuleInputFromOutputDtoRead>;

public class ModuleInputFromOutputDeletedEvent : DeletedEvent<ModuleInputFromOutputDtoRead>;