using SnapCd.Contracts.Dto.ModulePulumiFlags;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModulePulumiFlagCreatedEvent : CreatedEvent<ModulePulumiFlagReadDto>;
public class ModulePulumiFlagUpdatedEvent : UpdatedEvent<ModulePulumiFlagReadDto>;
public class ModulePulumiFlagDeletedEvent : DeletedEvent<ModulePulumiFlagReadDto>;
