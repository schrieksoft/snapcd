// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

// Base (principal-agnostic) — what the controller/service speak.
public class IntegrationRoleAssignmentCreateDto
{
    /// <summary>ID of the integration the role is granted on.</summary>
    public Guid IntegrationId { get; set; }
    /// <summary>ID of the principal (user / group / service principal).</summary>
    public Guid PrincipalId { get; set; }
    /// <summary>Principal type: User, Group, or ServicePrincipal.</summary>
    public RoleAssignmentPrincipalDiscriminator PrincipalDiscriminator { get; set; }
    /// <summary>Integration role: Owner, Contributor, Reader, or IdentityAccessManager.</summary>
    public IntegrationRole RoleName { get; set; }
}

/// <summary>DTO for IntegrationRoleAssignment responses (GET operations).</summary>
public class IntegrationRoleAssignmentReadDto : IntegrationRoleAssignmentCreateDto, IDto
{
    /// <summary>Unique ID of the role assignment.</summary>
    public Guid Id { get; set; }
}

/// <summary>DTO for updating an existing IntegrationRoleAssignment (PUT operations).</summary>
public class IntegrationRoleAssignmentUpdateDto : IntegrationRoleAssignmentCreateDto, IUpdateDto
{
    /// <summary>Unique ID of the role assignment.</summary>
    public Guid Id { get; set; }
}

// Per-principal — parameterise the concrete repos/events.
public class UserIntegrationRoleAssignmentCreateDto
{
    public Guid UserId { get; set; }
    public Guid IntegrationId { get; set; }
    public IntegrationRole RoleName { get; set; }
}

public class UserIntegrationRoleAssignmentReadDto : UserIntegrationRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}

public class ServicePrincipalIntegrationRoleAssignmentCreateDto
{
    public Guid ServicePrincipalId { get; set; }
    public Guid IntegrationId { get; set; }
    public IntegrationRole RoleName { get; set; }
}

public class ServicePrincipalIntegrationRoleAssignmentReadDto : ServicePrincipalIntegrationRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}

public class GroupIntegrationRoleAssignmentCreateDto
{
    public Guid GroupId { get; set; }
    public Guid IntegrationId { get; set; }
    public IntegrationRole RoleName { get; set; }
}

public class GroupIntegrationRoleAssignmentReadDto : GroupIntegrationRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}
