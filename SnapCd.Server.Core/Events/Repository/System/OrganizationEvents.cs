using SnapCd.Server.Core.Dtos.Organizations;
using SnapCd.Server.Core.Events.Repository.System.Base;

namespace SnapCd.Server.Core.Events.Repository.System;

public class OrganizationCreatedEvent : SystemCreatedEvent<OrganizationReadDto>;
public class OrganizationUpdatedEvent : SystemUpdatedEvent<OrganizationReadDto>;
public class OrganizationDeletedEvent : SystemDeletedEvent<OrganizationReadDto>;