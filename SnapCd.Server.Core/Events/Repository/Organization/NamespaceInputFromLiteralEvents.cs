using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceInputFromLiteralCreatedEvent : CreatedEvent<NamespaceInputFromLiteralReadDto>;

public class NamespaceInputFromLiteralUpdatedEvent : UpdatedEvent<NamespaceInputFromLiteralReadDto>;
public class NamespaceInputFromLiteralDeletedEvent : DeletedEvent<NamespaceInputFromLiteralReadDto>;