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

public static class NamespaceMissionMapper
{
    public static NamespaceMission ToEntity(NamespaceMissionCreateDto dto, Guid organizationId)
    {
        return new NamespaceMission
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AgentId = dto.AgentId,
            NamespaceId = dto.NamespaceId,
            MissionType = dto.MissionType,
            SidecarName = dto.SidecarName,
            IsDisabled = dto.IsDisabled
        };
    }

    public static NamespaceMissionReadDto ToDto(NamespaceMission entity)
    {
        return new NamespaceMissionReadDto
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            NamespaceId = entity.NamespaceId,
            MissionType = entity.MissionType,
            SidecarName = entity.SidecarName,
            IsDisabled = entity.IsDisabled
        };
    }

    public static void UpdateEntity(NamespaceMission entity, NamespaceMissionUpdateDto dto)
    {
        entity.AgentId = dto.AgentId;
        entity.NamespaceId = dto.NamespaceId;
        entity.MissionType = dto.MissionType;
        entity.SidecarName = dto.SidecarName;
        entity.IsDisabled = dto.IsDisabled;
    }
}
