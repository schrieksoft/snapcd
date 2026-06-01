// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services.Edition;
using SnapCd.Server.Core.Settings.DataSeeder;

namespace SnapCd.Server.Host.Services;

public class SelfHostedOrganizationCountValidator : IOrganizationCountValidator
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly object _lock = new();
    private DateTime _lastCheck = DateTime.MinValue;
    private bool _isOverLimit;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public SelfHostedOrganizationCountValidator(IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> IsOverLimitAsync()
    {
        lock (_lock)
        {
            if (DateTime.UtcNow - _lastCheck < CacheDuration)
                return _isOverLimit;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var orgCount = await dbContext.Organizations.CountAsync(
            o => o.DeletedDateTime == null && o.Id != PreseededSettings.DefaultId);

        lock (_lock)
        {
            _isOverLimit = orgCount > 1;
            _lastCheck = DateTime.UtcNow;
        }

        return _isOverLimit;
    }
}
