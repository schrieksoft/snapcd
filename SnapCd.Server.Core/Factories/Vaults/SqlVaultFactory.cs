// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Factories.Vaults;

public class SqlVaultFactory : IVaultFactory
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IOptions<SecretStoreSettings> _settings;
    private readonly ILoggerFactory _loggerFactory;

    // The key is validated in Create, not here: a bad key thrown from the constructor fails
    // inside dependency injection, which no page can catch or report.
    public SqlVaultFactory(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IOptions<SecretStoreSettings> settings,
        ILoggerFactory loggerFactory)
    {
        _dbContextFactory = dbContextFactory;
        _settings = settings;
        _loggerFactory = loggerFactory;
    }

    public IVault Create(string vaultUrl)
    {
        if (!SecretStoreConfigurationCheck.TryGetSqlServerKey(_settings.Value, out var key, out var problem))
            throw new InvalidOperationException(problem);

        var logger = _loggerFactory.CreateLogger<SqlVault>();
        return new SqlVault(_dbContextFactory, key, logger);
    }
}
