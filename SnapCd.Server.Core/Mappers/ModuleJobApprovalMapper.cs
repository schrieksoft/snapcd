// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleJobApprovals;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleJobApprovalMapper
{
    public static ModuleJobApproval ToEntity(ModuleJobApprovalCreateDto dto, Guid organizationId)
    {
        return new ModuleJobApproval
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleJobId = dto.ModuleJobId,
            PrincipalId = dto.PrincipalId,
            PrincipalDiscriminator = dto.PrincipalDiscriminator,
            DecisionDateTime = dto.DecisionDateTime,
            Declined = dto.Declined
        };
    }

    public static ModuleJobApprovalReadDto ToDto(ModuleJobApproval entity)
    {
        return new ModuleJobApprovalReadDto
        {
            Id = entity.Id,
            ModuleJobId = entity.ModuleJobId,
            PrincipalId = entity.PrincipalId,
            PrincipalDiscriminator = entity.PrincipalDiscriminator,
            DecisionDateTime = entity.DecisionDateTime,
            Declined = entity.Declined
        };
    }

    public static void UpdateEntity(ModuleJobApproval entity, ModuleJobApprovalUpdateDto dto)
    {
        entity.ModuleJobId = dto.ModuleJobId;
        entity.PrincipalId = dto.PrincipalId;
        entity.PrincipalDiscriminator = dto.PrincipalDiscriminator;
        entity.DecisionDateTime = dto.DecisionDateTime;
        entity.Declined = dto.Declined;
    }
}