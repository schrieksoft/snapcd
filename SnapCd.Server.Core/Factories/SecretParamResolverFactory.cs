using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Services.ParamResolver.Helpers;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Factories;

public class SecretParamResolverFactory
{
    private readonly SecretRepositoryFactory _repositoryFactory;
    private readonly IVaultFactory _vaultFactory;
    private readonly IOptions<SecretStoreSettings> _secretStoreSettings;

    public SecretParamResolverFactory(
        SecretRepositoryFactory repositoryFactory,
        IVaultFactory vaultFactory,
        IOptions<SecretStoreSettings> secretStoreSettings
    )
    {
        _repositoryFactory = repositoryFactory;
        _vaultFactory = vaultFactory;
        _secretStoreSettings = secretStoreSettings;
    }

    public virtual SecretParamResolver Create()
    {
        return new SecretParamResolver(
            _repositoryFactory.Create(),
            _vaultFactory,
            _secretStoreSettings);
    }
}