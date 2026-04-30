using SnapCd.Contracts.Dto.NamespaceExtraFiles;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class NamespaceExtraFileCreatedEvent : CreatedEvent<NamespaceExtraFileReadDto>;

public class NamespaceExtraFileUpdatedEvent : UpdatedEvent<NamespaceExtraFileReadDto>;

public class NamespaceExtraFileDeletedEvent : DeletedEvent<NamespaceExtraFileReadDto>;