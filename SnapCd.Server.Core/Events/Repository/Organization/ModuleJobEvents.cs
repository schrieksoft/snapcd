using SnapCd.Server.Core.Dtos.ModuleJobs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleJobCreatedEvent : CreatedEvent<ModuleJobReadDto>;

public class ModuleJobUpdatedEvent : UpdatedEvent<ModuleJobReadDto>;

public class ModuleJobDeletedEvent : DeletedEvent<ModuleJobReadDto>;