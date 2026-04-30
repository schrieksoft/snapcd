using SnapCd.Contracts.Dto.ModuleTerraformFlags;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleTerraformFlagCreatedEvent : CreatedEvent<ModuleTerraformFlagReadDto>;
public class ModuleTerraformFlagUpdatedEvent : UpdatedEvent<ModuleTerraformFlagReadDto>;
public class ModuleTerraformFlagDeletedEvent : DeletedEvent<ModuleTerraformFlagReadDto>;
