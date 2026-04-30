using SnapCd.Contracts.Dto.NamespaceTerraformFlags;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceTerraformFlagCreatedEvent : CreatedEvent<NamespaceTerraformFlagReadDto>;
public class NamespaceTerraformFlagUpdatedEvent : UpdatedEvent<NamespaceTerraformFlagReadDto>;
public class NamespaceTerraformFlagDeletedEvent : DeletedEvent<NamespaceTerraformFlagReadDto>;
