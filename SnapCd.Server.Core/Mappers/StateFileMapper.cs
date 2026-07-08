// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.StateFiles;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class StateFileMapper
{
    public static StateFile ToEntity(StateFileCreateDto dto, Guid organizationId)
    {
        return new StateFile
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StateStoreId = dto.StateStoreId,
            Name = dto.Name,
        };
    }

    public static StateFileReadDto ToDto(StateFile entity)
    {
        return new StateFileReadDto
        {
            Id = entity.Id,
            StateStoreId = entity.StateStoreId,
            Name = entity.Name,
            LockId = entity.LockId,
            LockInfo = entity.LockInfo,
            LockCreatedAt = entity.LockCreatedAt,
            LockedById = entity.LockedById,
            LockedByPrincipalDiscriminator = entity.LockedByPrincipalDiscriminator?.ToString()
        };
    }

    public static void UpdateEntity(StateFile entity, StateFileUpdateDto dto)
    {
        entity.Name = dto.Name;
    }
}
