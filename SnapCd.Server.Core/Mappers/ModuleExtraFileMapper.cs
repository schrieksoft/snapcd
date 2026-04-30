using SnapCd.Contracts.Dto.Misc;
using SnapCd.Contracts.Dto.ModuleExtraFiles;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleExtraFileMapper
{
    public static ModuleExtraFile ToEntity(ModuleExtraFileCreateDto dto, Guid organizationId)
    {
        return new ModuleExtraFile
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            FileName = dto.FileName,
            Contents = dto.Contents,
            Overwrite = dto.Overwrite ?? false
        };
    }

    public static ModuleExtraFileReadDto ToDto(ModuleExtraFile entity)
    {
        return new ModuleExtraFileReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            FileName = entity.FileName,
            Contents = entity.Contents,
            Overwrite = entity.Overwrite
        };
    }

    public static ExtraFileDto ToExtraFileDto(ModuleExtraFile entity)
    {
        return new ExtraFileDto
        {
            FileName = entity.FileName,
            Contents = entity.Contents,
            Overwrite = entity.Overwrite,
            Source = ExtraFileSource.Module.ToString()
        };
    }

    public static void UpdateEntity(ModuleExtraFile entity, ModuleExtraFileUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.FileName = dto.FileName;
        entity.Contents = dto.Contents;
        entity.Overwrite = dto.Overwrite ?? false;
    }
}