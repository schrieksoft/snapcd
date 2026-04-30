using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceInputFromDefinitionCreatedEvent : CreatedEvent<NamespaceInputFromDefinitionReadDto>;

public class NamespaceInputFromDefinitionUpdatedEvent : UpdatedEvent<NamespaceInputFromDefinitionReadDto>;

public class NamespaceInputFromDefinitionDeletedEvent : DeletedEvent<NamespaceInputFromDefinitionReadDto>;