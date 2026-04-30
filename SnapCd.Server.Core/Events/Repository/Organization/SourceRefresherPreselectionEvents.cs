using SnapCd.Contracts.Dto.SourceRefresherPreselections;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class SourceRefresherPreselectionCreatedEvent : CreatedEvent<SourceRefresherPreselectionReadDto>;

public class SourceRefresherPreselectionUpdatedEvent : UpdatedEvent<SourceRefresherPreselectionReadDto>;

public class SourceRefresherPreselectionDeletedEvent : DeletedEvent<SourceRefresherPreselectionReadDto>;
