// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;

namespace SnapCd.Server.Core.Mappers.GroupMembers;

public static class ServicePrincipalGroupMemberMapper
{
    public static ServicePrincipalGroupMember ToEntity(ServicePrincipalGroupMemberCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalGroupMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            ServicePrincipalId = dto.ServicePrincipalId
        };
    }

    public static ServicePrincipalGroupMemberReadDto ToDto(ServicePrincipalGroupMember entity)
    {
        return new ServicePrincipalGroupMemberReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            ServicePrincipalId = entity.ServicePrincipalId
        };
    }

    public static void UpdateEntity(ServicePrincipalGroupMember entity, ServicePrincipalGroupMemberUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.ServicePrincipalId = dto.ServicePrincipalId;
    }
}