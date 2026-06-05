// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

/// <summary>
/// Mapper for AgentConnection entity to DTO conversions.
/// </summary>
public static class AgentConnectionMapper
{
    public static AgentConnectionReadDto ToDto(AgentConnection entity)
    {
        return new AgentConnectionReadDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            AgentId = entity.AgentId,
            InstanceName = entity.InstanceName,
            ConnectionId = entity.SignalRConnectionId,
            ServerInstanceId = entity.ServerInstanceId,
            CreatedDateTime = entity.CreatedDateTime,
            ModifiedDateTime = entity.ModifiedDateTime
        };
    }
}
