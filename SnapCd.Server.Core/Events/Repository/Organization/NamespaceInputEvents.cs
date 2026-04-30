using SnapCd.Contracts.Dto.NamespaceInputs.Base;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceInputCreatedEvent : CreatedEvent<NamespaceInputReadDto>;

public class NamespaceInputUpdatedEvent : UpdatedEvent<NamespaceInputReadDto>;

public class NamespaceInputDeletedEvent : DeletedEvent<NamespaceInputReadDto>;