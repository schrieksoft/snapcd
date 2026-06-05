// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Missions;
using SnapCd.Server.Core.Entities.Definition.Missions;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleMissionMapper
{
    public static ModuleMission ToEntity(ModuleMissionCreateDto dto, Guid organizationId)
    {
        return new ModuleMission
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AgentId = dto.AgentId,
            ModuleId = dto.ModuleId,
            MissionType = dto.MissionType,
            SidecarName = dto.SidecarName,
            IsDisabled = dto.IsDisabled
        };
    }

    public static ModuleMissionReadDto ToDto(ModuleMission entity)
    {
        return new ModuleMissionReadDto
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            ModuleId = entity.ModuleId,
            MissionType = entity.MissionType,
            SidecarName = entity.SidecarName,
            IsDisabled = entity.IsDisabled
        };
    }

    public static void UpdateEntity(ModuleMission entity, ModuleMissionUpdateDto dto)
    {
        entity.AgentId = dto.AgentId;
        entity.ModuleId = dto.ModuleId;
        entity.MissionType = dto.MissionType;
        entity.SidecarName = dto.SidecarName;
        entity.IsDisabled = dto.IsDisabled;
    }
}
