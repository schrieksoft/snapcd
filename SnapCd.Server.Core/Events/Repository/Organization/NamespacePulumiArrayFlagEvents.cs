using SnapCd.Contracts.Dto.NamespacePulumiArrayFlags;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespacePulumiArrayFlagCreatedEvent : CreatedEvent<NamespacePulumiArrayFlagReadDto>;
public class NamespacePulumiArrayFlagUpdatedEvent : UpdatedEvent<NamespacePulumiArrayFlagReadDto>;
public class NamespacePulumiArrayFlagDeletedEvent : DeletedEvent<NamespacePulumiArrayFlagReadDto>;
