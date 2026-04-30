using SnapCd.Contracts.Dto.NamespaceBackendConfigs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceBackendConfigCreatedEvent : CreatedEvent<NamespaceBackendConfigReadDto>;
public class NamespaceBackendConfigUpdatedEvent : UpdatedEvent<NamespaceBackendConfigReadDto>;

public class NamespaceBackendConfigDeletedEvent : DeletedEvent<NamespaceBackendConfigReadDto>;