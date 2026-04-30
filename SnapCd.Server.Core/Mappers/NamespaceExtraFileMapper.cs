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