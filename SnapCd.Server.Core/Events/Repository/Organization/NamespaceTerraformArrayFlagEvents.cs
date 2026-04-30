using SnapCd.Contracts.Dto.NamespaceTerraformArrayFlags;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceTerraformArrayFlagCreatedEvent : CreatedEvent<NamespaceTerraformArrayFlagReadDto>;
public class NamespaceTerraformArrayFlagUpdatedEvent : UpdatedEvent<NamespaceTerraformArrayFlagReadDto>;
public class NamespaceTerraformArrayFlagDeletedEvent : DeletedEvent<NamespaceTerraformArrayFlagReadDto>;
