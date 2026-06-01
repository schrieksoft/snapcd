// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Misc;
using SnapCd.Contracts.Dto.NamespaceExtraFiles;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceExtraFileMapper
{
    public static NamespaceExtraFile ToEntity(NamespaceExtraFileCreateDto dto, Guid organizationId)
    {
        return new NamespaceExtraFile
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            FileName = dto.FileName,
            Contents = dto.Contents,
            Overwrite = dto.Overwrite
        };
    }

    public static NamespaceExtraFileReadDto ToDto(NamespaceExtraFile entity)
    {
        return new NamespaceExtraFileReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            FileName = entity.FileName,
            Contents = entity.Contents,
            Overwrite = entity.Overwrite
        };
    }

    public static ExtraFileDto ToExtraFileDto(NamespaceExtraFile entity)
    {
        return new ExtraFileDto
        {
            FileName = entity.FileName,
            Contents = entity.Contents,
            Overwrite = entity.Overwrite,
            Source = ExtraFileSource.Namespace.ToString()
        };
    }

    public static void UpdateEntity(NamespaceExtraFile entity, NamespaceExtraFileUpdateDto dto)
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.FileName = dto.FileName;
        entity.Contents = dto.Contents;
        entity.Overwrite = dto.Overwrite;
    }
}