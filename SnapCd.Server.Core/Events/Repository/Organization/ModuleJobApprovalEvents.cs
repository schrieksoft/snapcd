using SnapCd.Contracts.Dto.ModuleJobApprovals;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class ModuleJobApprovalCreatedEvent : CreatedEvent<ModuleJobApprovalReadDto>;

public class ModuleJobApprovalUpdatedEvent : UpdatedEvent<ModuleJobApprovalReadDto>;

public class ModuleJobApprovalDeletedEvent : DeletedEvent<ModuleJobApprovalReadDto>;