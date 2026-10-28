// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Logs;

public class SecuredLogReaderFactory(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    ILoggerFactory loggerFactory,
    ModuleSecuredRepositoryFactory moduleSecuredRepositoryFactory)
{
    public SecuredLogReader<TJob, TChunk, TLogEntry> Create<TJob, TChunk, TLogEntry>(
        IPrincipalProvider? principalProvider = null)
        where TJob : class, IJobWithLogs, IModuleChild
        where TChunk : JobLogChunk
        where TLogEntry : class, IJobLogEntry, new()
    {
        principalProvider ??= new HttpContextPrincipalProvider(new HttpContextAccessor());

        // One context per reader: a component builds several and uses them at the same time.
        var dbContext = dbContextFactory.CreateDbContext();

        return new SecuredLogReader<TJob, TChunk, TLogEntry>(
            dbContext,
            new JobLogReader<TJob, TChunk, TLogEntry>(
                dbContext,
                loggerFactory.CreateLogger<JobLogReader<TJob, TChunk, TLogEntry>>()),
            moduleSecuredRepositoryFactory.Create(principalProvider));
    }
}

/// <summary>
/// Reading a job's log is reading the Module it ran against: a job carries no permissions of its
/// own, so access is delegated rather than duplicated.
/// </summary>
public class SecuredLogReader<TJob, TChunk, TLogEntry> : IDisposable
    where TJob : class, IJobWithLogs, IModuleChild
    where TChunk : JobLogChunk
    where TLogEntry : class, IJobLogEntry, new()
{
    private readonly SnapCdDbContext _dbContext;
    private readonly JobLogReader<TJob, TChunk, TLogEntry> _logs;
    private readonly ModuleSecuredRepository _modules;

    public SecuredLogReader(
        SnapCdDbContext dbContext,
        JobLogReader<TJob, TChunk, TLogEntry> logs,
        ModuleSecuredRepository modules)
    {
        _dbContext = dbContext;
        _logs = logs;
        _modules = modules;
    }

    private async Task EnsureCanRead(Guid jobId, Guid organizationId)
    {

        var moduleId = await _dbContext.Set<TJob>()
            .AsNoTracking()
            .Where(j => j.Id == jobId && j.OrganizationId == organizationId)
            .Select(j => (Guid?)j.ModuleId)
            .FirstOrDefaultAsync();

        if (moduleId is not { } module || !_modules.CanRead(module, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Job with ID {jobId} not found or the principal does not have permission to read its log.");
    }

    public async Task<List<LogChunkSummary>> GetChunkSummaries(
        Guid jobId, Guid organizationId, Guid? moduleId = null, long afterSequence = -1)
    {
        await EnsureCanRead(jobId, organizationId);
        return await _logs.GetChunkSummaries(jobId, moduleId, afterSequence);
    }

    public async Task<List<TLogEntry>> ReadChunks(
        Guid jobId, Guid organizationId, IReadOnlyCollection<long> sequences, Guid? moduleId = null)
    {
        await EnsureCanRead(jobId, organizationId);
        return await _logs.ReadChunks(jobId, sequences, moduleId);
    }

    public async Task<List<TLogEntry>> GetLogEntries(Guid jobId, Guid organizationId, Guid? moduleId = null)
    {
        await EnsureCanRead(jobId, organizationId);
        return await _logs.GetLogEntries(jobId, moduleId);
    }

    public async Task<Dictionary<string, string>> GetLogStrings(
        Guid jobId, Guid organizationId, Guid? moduleId = null)
    {
        await EnsureCanRead(jobId, organizationId);
        return await _logs.GetLogStrings(jobId, moduleId);
    }

    public async Task<Dictionary<Guid, ProbeWaveOrigin>> ProbeWaveOrigins(
        IReadOnlyCollection<Guid> waveIds, Guid organizationId) =>
        await _logs.ProbeWaveOrigins(waveIds, organizationId);

    public void Dispose() => _modules.Dispose();
}
