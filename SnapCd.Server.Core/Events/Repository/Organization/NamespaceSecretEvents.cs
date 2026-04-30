using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceSecretCreatedEvent : CreatedEvent<NamespaceSecretDto>;
public class NamespaceSecretUpdatedEvent : UpdatedEvent<NamespaceSecretDto>;
public class NamespaceSecretDeletedEvent : DeletedEvent<NamespaceSecretDto>;