using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleSecretCreatedEvent : CreatedEvent<ModuleSecretDto>;

public class ModuleSecretUpdatedEvent : UpdatedEvent<ModuleSecretDto>;
public class ModuleSecretDeletedEvent : DeletedEvent<ModuleSecretDto>;