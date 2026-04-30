using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class StackSecretCreatedEvent : CreatedEvent<StackSecretDto>;

public class StackSecretUpdatedEvent : UpdatedEvent<StackSecretDto>;

public class StackSecretDeletedEvent : DeletedEvent<StackSecretDto>;
