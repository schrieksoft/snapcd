using SnapCd.Server.Core.Dtos.PreviewFeatureAcceptances;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class PreviewFeatureAcceptanceCreatedEvent : CreatedEvent<PreviewFeatureAcceptanceReadDto>;

public class PreviewFeatureAcceptanceUpdatedEvent : UpdatedEvent<PreviewFeatureAcceptanceReadDto>;

public class PreviewFeatureAcceptanceDeletedEvent : DeletedEvent<PreviewFeatureAcceptanceReadDto>;
