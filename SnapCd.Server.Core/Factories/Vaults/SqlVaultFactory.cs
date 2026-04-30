using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Factories.Vaults;

public class SqlVaultFactory : IVaultFactory
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly byte[] _key;
    private readonly ILoggerFactory _loggerFactory;

    public SqlVaultFactory(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IOptions<SecretStoreSettings> settings,
        ILoggerFactory loggerFactory)
    {
        _dbContextFactory = dbContextFactory;
        _loggerFactory = loggerFactory;

        var base64 = settings.Value.SqlServer?.SymmetricKey
            ?? throw new InvalidOperationException(
                "SecretStore.Provider is SqlServer but SecretStore.SqlServer.SymmetricKey is not set.");
        try
        {
            _key = Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "SecretStore.SqlServer.SymmetricKey must be a Base64-encoded 32-byte AES-256 key.", ex);
        }

        if (_key.Length != 32)
            throw new InvalidOperationException(
                $"SecretStore.SqlServer.SymmetricKey must decode to 32 bytes (got {_key.Length}).");
    }

    public IVault Create(string vaultUrl)
    {
        var logger = _loggerFactory.CreateLogger<SqlVault>();
        return new SqlVault(_dbContextFactory, _key, logger);
    }
}
