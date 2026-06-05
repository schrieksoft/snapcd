// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.AgentNamespaceAssignments;
using SnapCd.Server.Core.Entities.Definition.AgentAssignments;

namespace SnapCd.Server.Core.Mappers;

public static class AgentNamespaceAssignmentMapper
{
    public static AgentNamespaceAssignment ToEntity(AgentNamespaceAssignmentCreateDto dto, Guid organizationId)
    {
        return new AgentNamespaceAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            AgentId = dto.AgentId
        };
    }

    public static AgentNamespaceAssignmentReadDto ToDto(AgentNamespaceAssignment entity)
    {
        return new AgentNamespaceAssignmentReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            AgentId = entity.AgentId
        };
    }

    public static void UpdateEntity(AgentNamespaceAssignment entity, AgentNamespaceAssignmentUpdateDto dto)
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.AgentId = dto.AgentId;
    }
}
