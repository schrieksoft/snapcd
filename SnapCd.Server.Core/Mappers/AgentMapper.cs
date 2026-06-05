// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Agents;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class AgentMapper
{
    public static Agent ToEntity(AgentCreateDto dto, Guid organizationId)
    {
        return new Agent
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            Name = dto.Name,
            IsDisabled = dto.IsDisabled,
            AllowMultipleInstances = dto.AllowMultipleInstances,
            IsAssignedToAllModules = dto.IsAssignedToAllModules
        };
    }

    public static AgentReadDto ToDto(Agent entity)
    {
        return new AgentReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            Name = entity.Name,
            IsDisabled = entity.IsDisabled,
            AllowMultipleInstances = entity.AllowMultipleInstances,
            IsAssignedToAllModules = entity.IsAssignedToAllModules
        };
    }

    public static void UpdateEntity(Agent entity, AgentUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.Name = dto.Name;
        entity.IsDisabled = dto.IsDisabled;
        entity.AllowMultipleInstances = dto.AllowMultipleInstances;
        entity.IsAssignedToAllModules = dto.IsAssignedToAllModules;
    }
}
