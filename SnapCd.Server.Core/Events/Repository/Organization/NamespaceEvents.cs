using SnapCd.Contracts.Dto.Namespaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceCreatedEvent : CreatedEvent<NamespaceReadDto>;
public class NamespaceUpdatedEvent : UpdatedEvent<NamespaceReadDto>;
public class NamespaceDeletedEvent : DeletedEvent<NamespaceReadDto>;