// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.StateFiles;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class StateFileServiceFactory(
    StateFileSecuredRepositoryFactory securedRepositoryFactory,
    IStateEncryptionService encryptionService,
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    IOptions<StateStoreSettings> stateStoreSettings)
{
    public StateFileService Create(IPrincipalProvider principalProvider)
    {
        return new StateFileService(
            securedRepositoryFactory.Create(principalProvider),
            encryptionService,
            dbContextFactory,
            principalProvider,
            stateStoreSettings);
    }
}

public class StateFileService : GenericCrudService<
    StateFile,
    StateFileCreateDto,
    StateFileUpdateDto,
    StateFileReadDto,
    StateFileSecuredRepository,
    StateFileRepository,
    StateFileCreatedEvent,
    StateFileUpdatedEvent,
    StateFileDeletedEvent,
    StateFileRepositorySettings>
{
    private readonly IStateEncryptionService _encryptionService;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IPrincipalProvider _principalProvider;
    private readonly StateStoreSettings _stateStoreSettings;

    public StateFileService(
        StateFileSecuredRepository securedRepository,
        IStateEncryptionService encryptionService,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IPrincipalProvider principalProvider
    ) : base(securedRepository)
    {
        _encryptionService = encryptionService;
        _dbContextFactory = dbContextFactory;
        _principalProvider = principalProvider;
        _stateStoreSettings = new StateStoreSettings();
    }

    public StateFileService(
        StateFileSecuredRepository securedRepository,
        IStateEncryptionService encryptionService,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IPrincipalProvider principalProvider,
        IOptions<StateStoreSettings> stateStoreSettings
    ) : base(securedRepository)
    {
        _encryptionService = encryptionService;
        _dbContextFactory = dbContextFactory;
        _principalProvider = principalProvider;
        _stateStoreSettings = stateStoreSettings.Value;
    }

    protected override StateFile MapToEntity(StateFileCreateDto dto, Guid organizationId)
    {
        return StateFileMapper.ToEntity(dto, organizationId);
    }

    protected override StateFileReadDto MapToDto(StateFile entity)
    {
        var dto = StateFileMapper.ToDto(entity);
        return dto;
    }

    public override async Task<StateFileReadDto> Create(StateFileCreateDto createDto, Guid organizationId)
    {
        var entity = MapToEntity(createDto, organizationId);
        entity = await SecuredRepository.Create(entity);
        var dto = MapToDto(entity);

        if (!string.IsNullOrEmpty(createDto.Data))
        {
            var encrypted = _encryptionService.Encrypt(Encoding.UTF8.GetBytes(createDto.Data));
            await CreateVersionInternal(entity.Id, organizationId, encrypted);
        }

        return dto;
    }

    public override async Task<StateFileReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(id, organizationId);
        var dto = MapToDto(entity);
        await PopulateLatestVersionData(dto);
        return dto;
    }

    public override async Task<List<StateFileReadDto>> ListByParentId(Guid parentId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByParentId(parentId, organizationId);
        return entities.Select(StateFileMapper.ToDto).ToList();
    }

    public override async Task<List<StateFileReadDto>> List(Guid organizationId)
    {
        var entities = await SecuredRepository.List(organizationId);
        return entities.Select(StateFileMapper.ToDto).ToList();
    }

    public override async Task<StateFileReadDto> Update(StateFileUpdateDto updateDto, Guid id, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(id, organizationId);
        StateFileMapper.UpdateEntity(entity, updateDto);
        entity = await SecuredRepository.Update(entity);
        var dto = MapToDto(entity);

        if (updateDto.Data != null)
        {
            var encrypted = _encryptionService.Encrypt(Encoding.UTF8.GetBytes(updateDto.Data));
            await CreateVersionInternal(id, organizationId, encrypted);
        }

        return dto;
    }

    public async Task ForceUnlock(Guid id, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(id, organizationId);
        entity.LockId = null;
        entity.LockInfo = null;
        entity.LockCreatedAt = null;
        entity.LockedById = null;
        entity.LockedByPrincipalDiscriminator = null;
        await SecuredRepository.Update(entity);
    }

    public async Task<List<StateFileVersionReadDto>> ListVersions(Guid stateFileId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var versions = await dbContext.Set<StateFileVersion>()
            .Where(v => v.StateFileId == stateFileId && v.OrganizationId == organizationId)
            .OrderByDescending(v => v.CreatedDateTime)
            .ToListAsync();

        var latestId = versions.Count > 0 ? versions[0].Id : (Guid?)null;

        var spIds = versions
            .Where(v => v.CreatedByPrincipalDiscriminator == AuditPrincipalDiscriminator.ServicePrincipal)
            .Select(v => v.CreatedBy)
            .Distinct()
            .ToList();

        var spDisplayNames = new Dictionary<Guid, string>();
        if (spIds.Count > 0)
        {
            var sps = await dbContext.Set<ServicePrincipal>()
                .Where(sp => spIds.Contains(sp.Id))
                .Select(sp => new { sp.Id, sp.ClientId })
                .ToListAsync();

            foreach (var sp in sps)
            {
                var displayName = sp.ClientId != null && sp.ClientId.Contains(':')
                    ? sp.ClientId[(sp.ClientId.IndexOf(':') + 1)..]
                    : sp.ClientId;
                spDisplayNames[sp.Id] = displayName ?? sp.Id.ToString();
            }
        }

        return versions.Select(v => new StateFileVersionReadDto
        {
            Id = v.Id,
            StateFileId = v.StateFileId,
            CreatedBy = v.CreatedBy,
            CreatedByPrincipalDiscriminator = v.CreatedByPrincipalDiscriminator.ToString(),
            CreatedByDisplayName = v.CreatedByPrincipalDiscriminator == AuditPrincipalDiscriminator.ServicePrincipal
                && spDisplayNames.TryGetValue(v.CreatedBy, out var name) ? name : v.CreatedBy.ToString(),
            CreatedDateTime = v.CreatedDateTime,
            IsLatest = v.Id == latestId
        }).ToList();
    }

    public async Task RestoreVersion(Guid stateFileId, Guid versionId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var version = await dbContext.Set<StateFileVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && v.StateFileId == stateFileId && v.OrganizationId == organizationId)
            ?? throw new InvalidOperationException("Version not found.");

        var latestVersion = await dbContext.Set<StateFileVersion>()
            .Where(v => v.StateFileId == stateFileId)
            .OrderByDescending(v => v.CreatedDateTime)
            .FirstOrDefaultAsync();

        var currentSerial = latestVersion?.Data != null
            ? ExtractSerialFromEncryptedData(latestVersion.Data)
            : 0;
        var nextSerial = currentSerial + 1;

        var principalId = _principalProvider.GetSubjectOrDefault(organizationId);
        var principalDiscriminator = _principalProvider.GetPrincipalDiscriminatorOrDefault()
            ?? Contracts.PrincipalDiscriminator.User;
        var auditDiscriminator = principalDiscriminator == Contracts.PrincipalDiscriminator.ServicePrincipal
            ? AuditPrincipalDiscriminator.ServicePrincipal
            : AuditPrincipalDiscriminator.User;

        var data = PatchStateJsonForRestore(version.Data, nextSerial);

        dbContext.Set<StateFileVersion>().Add(new StateFileVersion
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StateFileId = stateFileId,
            Data = data,
            CreatedBy = principalId,
            CreatedByPrincipalDiscriminator = auditDiscriminator,
            CreatedDateTime = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        await PruneOldVersions(stateFileId);
    }

    public async Task<string?> GetVersionData(Guid versionId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var version = await dbContext.Set<StateFileVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && v.OrganizationId == organizationId)
            ?? throw new InvalidOperationException("Version not found.");

        if (version.Data == null || version.Data.Length == 0)
            return null;

        return FormatJson(Encoding.UTF8.GetString(_encryptionService.Decrypt(version.Data)));
    }

    public async Task DeleteVersion(Guid versionId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var version = await dbContext.Set<StateFileVersion>()
            .FirstOrDefaultAsync(v => v.Id == versionId && v.OrganizationId == organizationId)
            ?? throw new InvalidOperationException("Version not found.");

        dbContext.Set<StateFileVersion>().Remove(version);
        await dbContext.SaveChangesAsync();
    }

    public async Task<StateFile?> FindByName(string name, Guid stateStoreId)
    {
        return await SecuredRepository.FindByName(name, stateStoreId);
    }

    public async Task<StateFile> GetByName(string name, Guid stateStoreId, Guid organizationId)
    {
        return await SecuredRepository.GetByName(name, stateStoreId, organizationId);
    }

    public async Task<StateFileVersion?> GetLatestVersion(Guid stateFileId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Set<StateFileVersion>()
            .Where(v => v.StateFileId == stateFileId)
            .OrderByDescending(v => v.CreatedDateTime)
            .FirstOrDefaultAsync();
    }

    public string? DecryptData(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return null;
        return Encoding.UTF8.GetString(_encryptionService.Decrypt(data));
    }

    public byte[] EncryptData(string data)
    {
        return _encryptionService.Encrypt(Encoding.UTF8.GetBytes(data));
    }

    public async Task<StateFile> CreateStateFile(StateFile stateFile)
    {
        return await SecuredRepository.Create(stateFile);
    }

    public async Task<StateFile> UpdateStateFile(StateFile stateFile)
    {
        return await SecuredRepository.Update(stateFile);
    }

    public async Task DeleteStateFile(Guid id, Guid organizationId)
    {
        await SecuredRepository.Delete(id, organizationId);
    }

    public bool IsLockExpired(StateFile stateFile)
    {
        if (stateFile.LockCreatedAt == null) return true;
        return DateTimeOffset.UtcNow - stateFile.LockCreatedAt.Value > TimeSpan.FromMinutes(_stateStoreSettings.LockTimeoutMinutes);
    }

    public async Task<StateFileVersion> CreateVersion(Guid stateFileId, Guid organizationId, byte[] encryptedData, Guid principalId, AuditPrincipalDiscriminator principalDiscriminator)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var version = new StateFileVersion
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StateFileId = stateFileId,
            Data = encryptedData,
            CreatedBy = principalId,
            CreatedByPrincipalDiscriminator = principalDiscriminator,
            CreatedDateTime = DateTime.UtcNow
        };
        dbContext.Set<StateFileVersion>().Add(version);
        await dbContext.SaveChangesAsync();

        await PruneOldVersions(stateFileId);

        return version;
    }

    public async Task<Guid> GetOrganizationIdForStateStore(Guid stateStoreId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var stateStore = await dbContext.Set<StateStore>()
            .FirstOrDefaultAsync(ss => ss.Id == stateStoreId);
        if (stateStore == null)
            throw new InvalidOperationException("State store not found.");
        return stateStore.OrganizationId;
    }

    private async Task CreateVersionInternal(Guid stateFileId, Guid organizationId, byte[] encryptedData)
    {
        var principalId = _principalProvider.GetSubjectOrDefault(organizationId);
        var principalDiscriminator = _principalProvider.GetPrincipalDiscriminatorOrDefault()
            ?? Contracts.PrincipalDiscriminator.User;
        var auditDiscriminator = principalDiscriminator == Contracts.PrincipalDiscriminator.ServicePrincipal
            ? AuditPrincipalDiscriminator.ServicePrincipal
            : AuditPrincipalDiscriminator.User;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Set<StateFileVersion>().Add(new StateFileVersion
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StateFileId = stateFileId,
            Data = encryptedData,
            CreatedBy = principalId,
            CreatedByPrincipalDiscriminator = auditDiscriminator,
            CreatedDateTime = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        await PruneOldVersions(stateFileId);
    }

    private async Task PruneOldVersions(Guid stateFileId)
    {
        if (_stateStoreSettings.MaxStateFileVersions <= 0) return;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var versionsToDelete = await dbContext.Set<StateFileVersion>()
            .Where(v => v.StateFileId == stateFileId)
            .OrderByDescending(v => v.CreatedDateTime)
            .Skip(_stateStoreSettings.MaxStateFileVersions)
            .ToListAsync();

        if (versionsToDelete.Count > 0)
        {
            dbContext.Set<StateFileVersion>().RemoveRange(versionsToDelete);
            await dbContext.SaveChangesAsync();
        }
    }

    private long ExtractSerialFromEncryptedData(byte[] encryptedData)
    {
        var json = Encoding.UTF8.GetString(_encryptionService.Decrypt(encryptedData));
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("serial", out var serialProp))
            throw new InvalidOperationException("State file data is missing the 'serial' field.");
        return serialProp.GetInt64();
    }

    private byte[] PatchStateJsonForRestore(byte[]? encryptedData, long newSerial)
    {
        if (encryptedData == null || encryptedData.Length == 0)
            throw new InvalidOperationException("Cannot restore a version with no data.");

        var newLineage = Guid.NewGuid().ToString();
        var json = Encoding.UTF8.GetString(_encryptionService.Decrypt(encryptedData));
        using var doc = JsonDocument.Parse(json);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            var wroteSerial = false;
            var wroteLineage = false;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name == "serial")
                {
                    writer.WriteNumber("serial", newSerial);
                    wroteSerial = true;
                }
                else if (prop.Name == "lineage")
                {
                    writer.WriteString("lineage", newLineage);
                    wroteLineage = true;
                }
                else
                {
                    prop.WriteTo(writer);
                }
            }
            if (!wroteSerial)
                throw new InvalidOperationException("State file data is missing the 'serial' field.");
            if (!wroteLineage)
                throw new InvalidOperationException("State file data is missing the 'lineage' field.");
            writer.WriteEndObject();
        }
        var patched = Encoding.UTF8.GetString(stream.ToArray());
        return _encryptionService.Encrypt(Encoding.UTF8.GetBytes(patched));
    }

    private async Task PopulateLatestVersionData(StateFileReadDto dto)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var latest = await dbContext.Set<StateFileVersion>()
            .Where(v => v.StateFileId == dto.Id)
            .OrderByDescending(v => v.CreatedDateTime)
            .FirstOrDefaultAsync();

        if (latest != null)
        {
            if (latest.Data != null && latest.Data.Length > 0)
                dto.Data = FormatJson(Encoding.UTF8.GetString(_encryptionService.Decrypt(latest.Data)));
        }
    }

    private static string? FormatJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    protected override void UpdateEntityFromDto(StateFile entity, StateFileUpdateDto dto)
    {
        StateFileMapper.UpdateEntity(entity, dto);
    }
}
