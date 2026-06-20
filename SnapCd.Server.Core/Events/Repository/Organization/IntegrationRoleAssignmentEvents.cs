// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

public class IntegrationRoleAssignmentCreatedEvent : CreatedEvent<IntegrationRoleAssignmentReadDto>;
public class IntegrationRoleAssignmentUpdatedEvent : UpdatedEvent<IntegrationRoleAssignmentReadDto>;
public class IntegrationRoleAssignmentDeletedEvent : DeletedEvent<IntegrationRoleAssignmentReadDto>;

public class UserIntegrationRoleAssignmentCreatedEvent : CreatedEvent<UserIntegrationRoleAssignmentReadDto>;
public class UserIntegrationRoleAssignmentUpdatedEvent : UpdatedEvent<UserIntegrationRoleAssignmentReadDto>;
public class UserIntegrationRoleAssignmentDeletedEvent : DeletedEvent<UserIntegrationRoleAssignmentReadDto>;

public class ServicePrincipalIntegrationRoleAssignmentCreatedEvent : CreatedEvent<ServicePrincipalIntegrationRoleAssignmentReadDto>;
public class ServicePrincipalIntegrationRoleAssignmentUpdatedEvent : UpdatedEvent<ServicePrincipalIntegrationRoleAssignmentReadDto>;
public class ServicePrincipalIntegrationRoleAssignmentDeletedEvent : DeletedEvent<ServicePrincipalIntegrationRoleAssignmentReadDto>;

public class GroupIntegrationRoleAssignmentCreatedEvent : CreatedEvent<GroupIntegrationRoleAssignmentReadDto>;
public class GroupIntegrationRoleAssignmentUpdatedEvent : UpdatedEvent<GroupIntegrationRoleAssignmentReadDto>;
public class GroupIntegrationRoleAssignmentDeletedEvent : DeletedEvent<GroupIntegrationRoleAssignmentReadDto>;
