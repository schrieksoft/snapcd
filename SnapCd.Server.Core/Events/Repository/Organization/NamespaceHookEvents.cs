using SnapCd.Contracts.Dto.NamespaceHooks;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceHookCreatedEvent : CreatedEvent<NamespaceHookReadDto>;
public class NamespaceHookUpdatedEvent : UpdatedEvent<NamespaceHookReadDto>;
public class NamespaceHookDeletedEvent : DeletedEvent<NamespaceHookReadDto>;
