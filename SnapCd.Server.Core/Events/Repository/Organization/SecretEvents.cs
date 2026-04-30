using SnapCd.Contracts.Dto.Secrets;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class SecretCreatedEvent : CreatedEvent<SecretDto>;

public class SecretUpdatedEvent : UpdatedEvent<SecretDto>;

public class SecretDeletedEvent : DeletedEvent<SecretDto>;
