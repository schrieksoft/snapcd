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

public static class StackMissionMapper
{
    public static StackMission ToEntity(StackMissionCreateDto dto, Guid organizationId)
    {
        return new StackMission
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AgentId = dto.AgentId,
            StackId = dto.StackId,
            MissionType = dto.MissionType,
            SidecarName = dto.SidecarName,
            IsDisabled = dto.IsDisabled
        };
    }

    public static StackMissionReadDto ToDto(StackMission entity)
    {
        return new StackMissionReadDto
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            StackId = entity.StackId,
            MissionType = entity.MissionType,
            SidecarName = entity.SidecarName,
            IsDisabled = entity.IsDisabled
        };
    }

    public static void UpdateEntity(StackMission entity, StackMissionUpdateDto dto)
    {
        entity.AgentId = dto.AgentId;
        entity.StackId = dto.StackId;
        entity.MissionType = dto.MissionType;
        entity.SidecarName = dto.SidecarName;
        entity.IsDisabled = dto.IsDisabled;
    }
}
