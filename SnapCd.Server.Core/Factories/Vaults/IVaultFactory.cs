using SnapCd.Server.Core.Services.Vaults;

namespace SnapCd.Server.Core.Factories.Vaults;

public interface IVaultFactory
{
    /// <summary>
    /// Creates an <see cref="IVault"/> instance. <paramref name="vaultUrl"/> is the Azure Key Vault
    /// URL for the Azure implementation; the SQL-backed implementation ignores it (one logical store).
    /// </summary>
    IVault Create(string vaultUrl);
}
