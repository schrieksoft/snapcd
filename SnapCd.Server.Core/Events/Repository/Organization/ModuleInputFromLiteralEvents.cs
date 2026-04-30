using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleInputFromLiteralCreatedEvent : CreatedEvent<ModuleInputFromLiteralReadDto>;

public class ModuleInputFromLiteralUpdatedEvent : UpdatedEvent<ModuleInputFromLiteralReadDto>;

public class ModuleInputFromLiteralDeletedEvent : DeletedEvent<ModuleInputFromLiteralReadDto>;