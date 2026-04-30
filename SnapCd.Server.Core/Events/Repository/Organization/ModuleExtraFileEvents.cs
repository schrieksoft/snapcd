using SnapCd.Contracts.Dto.ModuleExtraFiles;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleExtraFileCreatedEvent : CreatedEvent<ModuleExtraFileReadDto>;

public class ModuleExtraFileUpdatedEvent : UpdatedEvent<ModuleExtraFileReadDto>;
public class ModuleExtraFileDeletedEvent : DeletedEvent<ModuleExtraFileReadDto>;