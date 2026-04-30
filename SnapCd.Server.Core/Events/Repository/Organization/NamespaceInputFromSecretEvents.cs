using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceInputFromSecretCreatedEvent : CreatedEvent<NamespaceInputFromSecretReadDto>;

public class NamespaceInputFromSecretUpdatedEvent : UpdatedEvent<NamespaceInputFromSecretReadDto>;

public class NamespaceInputFromSecretDeletedEvent : DeletedEvent<NamespaceInputFromSecretReadDto>;