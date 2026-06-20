// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Integrations;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Integrations.Codecs;

namespace SnapCd.Server.Core.Services.Integrations;

/// <summary>
/// CRUD for integrations: the row goes through the non-secured repo (auto audit + CRUD events), while the
/// connection blob is read/written/deleted in the secret backend. Reads return the redacted connection;
/// updates merge masked secret fields. Within an HTTP request the repo's default principal attributes audit
/// to the calling user.
/// </summary>
public sealed class IntegrationService(
    IntegrationRepositoryFactory repoFactory,
    IntegrationSecuredRepositoryFactory securedFactory,
    IIntegrationCodecRegistry codecs,
    IntegrationSecretStore secrets,
    IntegrationConnectionCache connectionCache,
    IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public async Task<List<IntegrationReadDto>> List(Guid organizationId)
    {
        // Only rows the caller may read (org-role filtered by the secured repo). All fields are on the row —
        // no decrypt needed for a list.
        using var secured = securedFactory.Create();
        var entities = await secured.List(organizationId);

        return entities
            .OrderBy(i => i.Name)
            .Select(ToReadDto)
            .ToList();
    }

    private static IntegrationReadDto ToReadDto(Integration i) => new()
    {
        Id = i.Id,
        OrganizationId = i.OrganizationId,
        Name = i.Name,
        IntegrationType = i.IntegrationType,
        Enabled = i.Enabled,
        IsSuppliedToAllModules = i.IsSuppliedToAllModules
    };

    /// <summary>
    /// Read of the row fields only — never the connection. This is the API/Terraform-facing read: the
    /// credentials blob (even redacted) must never cross the API surface or land in Terraform state.
    /// </summary>
    public async Task<IntegrationReadDto> GetRead(Guid id, Guid organizationId)
    {
        using var secured = securedFactory.Create();
        if (!secured.CanRead(id, organizationId))
            throw new PrincipalNotAuthorizedException($"Not permitted to read integration '{id}'.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Integrations
            .Where(i => i.Id == id && i.OrganizationId == organizationId)
            .FirstOrDefaultAsync();
        if (row is null) throw new EntityNotFoundException($"Integration '{id}' not found");
        return ToReadDto(row);
    }

    public async Task<IntegrationReadDto> GetReadByName(string name, Guid organizationId)
    {
        Guid? id;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            id = await db.Integrations
                .Where(i => i.OrganizationId == organizationId && i.Name == name)
                .Select(i => (Guid?)i.Id)
                .FirstOrDefaultAsync();
        }
        if (id is null) throw new EntityNotFoundException($"Integration '{name}' not found");
        return await GetRead(id.Value, organizationId); // permission-checked
    }

    public async Task<IntegrationDetailDto> Get(Guid id, Guid organizationId)
    {
        // Read fresh (redacted) — this is the low-frequency edit/lookup path. The connection cache serves the
        // high-frequency dispatch path, not display reads.
        using var secured = securedFactory.Create();
        if (!secured.CanRead(id, organizationId))
            throw new PrincipalNotAuthorizedException($"Not permitted to read integration '{id}'.");

        return await GetUncached(id, organizationId);
    }

    private async Task<IntegrationDetailDto> GetUncached(Guid id, Guid organizationId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Integrations
            .Where(i => i.Id == id && i.OrganizationId == organizationId)
            .FirstOrDefaultAsync();
        if (row is null) throw new EntityNotFoundException($"Integration '{id}' not found");

        var codec = codecs.Get(row.IntegrationType);
        var json = await secrets.ReadAsync(organizationId, id);

        string? connectionJson = null;
        if (json is not null)
        {
            var connection = codec.Deserialize(json);
            connectionJson = System.Text.Json.JsonSerializer.Serialize(codec.ToRedactedView(connection));
        }

        return new IntegrationDetailDto
        {
            Id = row.Id,
            OrganizationId = row.OrganizationId,
            Name = row.Name,
            IntegrationType = row.IntegrationType,
            Enabled = row.Enabled,
            IsSuppliedToAllModules = row.IsSuppliedToAllModules,
            Connection = connectionJson
        };
    }

    public async Task<Codecs.IntegrationTestResult> TestConnection(Guid id, Guid organizationId)
    {
        using (var secured = securedFactory.Create())
        {
            if (!secured.CanRead(id, organizationId))
                throw new PrincipalNotAuthorizedException($"Not permitted to test integration '{id}'.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var type = await db.Integrations
            .Where(i => i.Id == id && i.OrganizationId == organizationId)
            .Select(i => (IntegrationType?)i.IntegrationType)
            .FirstOrDefaultAsync();
        if (type is null) throw new EntityNotFoundException($"Integration '{id}' not found");

        var json = await secrets.ReadAsync(organizationId, id);
        if (json is null) return new Codecs.IntegrationTestResult(false, "Connection secret missing.");

        var codec = codecs.Get(type.Value);
        return await codec.TestConnectionAsync(codec.Deserialize(json), default);
    }

    public async Task<Guid> Create(Guid organizationId, IntegrationCreateDto dto)
    {
        using (var secured = securedFactory.Create())
        {
            if (!secured.CanCreate(organizationId, organizationId))
                throw new PrincipalNotAuthorizedException("Not permitted to create integrations in this organization.");
        }

        var codec = codecs.Get(dto.IntegrationType);
        var connection = codec.FromInput(dto.Connection, existing: null);
        var errors = codec.Validate(connection);
        if (errors.Count > 0) throw new ValidationException(string.Join(" ", errors));

        var id = Guid.NewGuid();
        var entity = new Integration
        {
            Id = id,
            OrganizationId = organizationId,
            Name = dto.Name,
            IntegrationType = dto.IntegrationType,
            Enabled = dto.Enabled,
            IsSuppliedToAllModules = dto.IsSuppliedToAllModules
        };

        using var repo = repoFactory.Create();
        await repo.Create(entity); // unique (org, type, name) enforced at the DB — throws on duplicate
        try
        {
            await secrets.WriteAsync(organizationId, id, codec.Serialize(connection));
        }
        catch
        {
            await repo.Delete(id, organizationId); // compensate: no row without its connection blob
            throw;
        }

        return id;
    }

    public async Task Update(Guid id, Guid organizationId, IntegrationUpdateDto dto)
    {
        using (var secured = securedFactory.Create())
        {
            if (!secured.CanUpdate(id, organizationId))
                throw new PrincipalNotAuthorizedException($"Not permitted to update integration '{id}'.");
        }

        using var repo = repoFactory.Create();
        var entity = await repo.DbContext.Integrations
            .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organizationId);
        if (entity is null) throw new EntityNotFoundException($"Integration '{id}' not found");

        var codec = codecs.Get(entity.IntegrationType);
        var existingJson = await secrets.ReadAsync(organizationId, id);
        var existing = existingJson is null ? null : codec.Deserialize(existingJson);
        var connection = codec.FromInput(dto.Connection, existing);
        var errors = codec.Validate(connection);
        if (errors.Count > 0) throw new ValidationException(string.Join(" ", errors));

        entity.Name = dto.Name;
        entity.Enabled = dto.Enabled;
        entity.IsSuppliedToAllModules = dto.IsSuppliedToAllModules;
        await repo.Update(entity);
        await secrets.WriteAsync(organizationId, id, codec.Serialize(connection));
        connectionCache.Evict(id); // immediate on this instance; the fanout consumer covers the others
    }

    public async Task Delete(Guid id, Guid organizationId)
    {
        using (var secured = securedFactory.Create())
        {
            if (!secured.CanDelete(id, organizationId))
                throw new PrincipalNotAuthorizedException($"Not permitted to delete integration '{id}'.");
        }

        using var repo = repoFactory.Create();
        var exists = await repo.DbContext.Integrations
            .AnyAsync(i => i.Id == id && i.OrganizationId == organizationId);
        if (!exists) throw new EntityNotFoundException($"Integration '{id}' not found");

        await repo.Delete(id, organizationId);
        await secrets.DeleteAsync(organizationId, id);
        connectionCache.Evict(id); // immediate on this instance; the fanout consumer covers the others
    }
}
