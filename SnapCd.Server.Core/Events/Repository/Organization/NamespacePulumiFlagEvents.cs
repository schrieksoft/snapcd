using SnapCd.Contracts.Dto.NamespacePulumiFlags;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespacePulumiFlagCreatedEvent : CreatedEvent<NamespacePulumiFlagReadDto>;
public class NamespacePulumiFlagUpdatedEvent : UpdatedEvent<NamespacePulumiFlagReadDto>;
public class NamespacePulumiFlagDeletedEvent : DeletedEvent<NamespacePulumiFlagReadDto>;
