// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Factories;

/// <summary>
/// Builds a log service for one job table. The caller names the table through the type arguments,
/// so nothing here or in the service works out which table an id belongs to.
/// </summary>
public class JobLogServiceFactory
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ILogger<JobLogServiceFactory> _logger;

    public JobLogServiceFactory(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        ILogger<JobLogServiceFactory> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public JobLogService<TJob, TLogEntry> Create<TJob, TLogEntry>()
        where TJob : class, IJobWithLogs
        where TLogEntry : class, IJobLogEntry =>
        new(_dbContextFactory, _logger);
}
