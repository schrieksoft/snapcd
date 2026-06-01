// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Secrets;
using SnapCd.Server.Core.Entities.Definition.Secrets;

namespace SnapCd.Server.Core.Mappers;

public static class SecretMapper
{
    public static Secret ToEntity(SecretCreateDto dto, Guid organizationId)
    {
        return new Secret
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name
        };
    }

    public static SecretDto ToDto(Secret entity)
    {
        return new SecretDto
        {
            Id = entity.Id,
            Name = entity.Name
        };
    }

    public static void UpdateEntity(Secret entity, SecretUpdateDto dto)
    {
        entity.Name = dto.Name;
    }
}