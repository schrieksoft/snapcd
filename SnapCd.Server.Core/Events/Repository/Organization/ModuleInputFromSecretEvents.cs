using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleInputFromSecretCreatedEvent : CreatedEvent<ModuleInputFromSecretReadDto>;

public class ModuleInputFromSecretUpdatedEvent : UpdatedEvent<ModuleInputFromSecretReadDto>;

public class ModuleInputFromSecretDeletedEvent : DeletedEvent<ModuleInputFromSecretReadDto>;