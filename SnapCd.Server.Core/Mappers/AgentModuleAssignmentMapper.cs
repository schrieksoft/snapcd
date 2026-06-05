// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.AgentModuleAssignments;
using SnapCd.Server.Core.Entities.Definition.AgentAssignments;

namespace SnapCd.Server.Core.Mappers;

public static class AgentModuleAssignmentMapper
{
    public static AgentModuleAssignment ToEntity(AgentModuleAssignmentCreateDto dto, Guid organizationId)
    {
        return new AgentModuleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            AgentId = dto.AgentId
        };
    }

    public static AgentModuleAssignmentReadDto ToDto(AgentModuleAssignment entity)
    {
        return new AgentModuleAssignmentReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            AgentId = entity.AgentId
        };
    }

    public static void UpdateEntity(AgentModuleAssignment entity, AgentModuleAssignmentUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.AgentId = dto.AgentId;
    }
}
