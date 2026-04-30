using SnapCd.Contracts.Dto.ModuleTerraformArrayFlags;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleTerraformArrayFlagCreatedEvent : CreatedEvent<ModuleTerraformArrayFlagReadDto>;
public class ModuleTerraformArrayFlagUpdatedEvent : UpdatedEvent<ModuleTerraformArrayFlagReadDto>;
public class ModuleTerraformArrayFlagDeletedEvent : DeletedEvent<ModuleTerraformArrayFlagReadDto>;
