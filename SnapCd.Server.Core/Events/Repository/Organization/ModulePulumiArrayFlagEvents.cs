using SnapCd.Contracts.Dto.ModulePulumiArrayFlags;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModulePulumiArrayFlagCreatedEvent : CreatedEvent<ModulePulumiArrayFlagReadDto>;
public class ModulePulumiArrayFlagUpdatedEvent : UpdatedEvent<ModulePulumiArrayFlagReadDto>;
public class ModulePulumiArrayFlagDeletedEvent : DeletedEvent<ModulePulumiArrayFlagReadDto>;
