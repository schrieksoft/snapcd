using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.Secrets;
using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.ParamResolver.Helpers;

public class SecretParamResolver
{
    private readonly SecretRepository _repository;
    private readonly IVaultFactory _vaultFactory;
    private readonly SecretStoreSettings _secretStoreSettings;

    public SecretParamResolver(
        SecretRepository repository,
        IVaultFactory vaultFactory,
        IOptions<SecretStoreSettings> secretStoreSettings
    )
    {
        _repository = repository;
        _vaultFactory = vaultFactory;
        _secretStoreSettings = secretStoreSettings.Value;
    }

    public string MakeRemoteSecretName(Secret secret, Guid organizationId)
    {
        var prefix = secret switch
        {
            ModuleSecret => "module",
            NamespaceSecret => "namespace",
            StackSecret => "stack",
            _ => throw new InvalidOperationException($"Unknown secret type: {secret.GetType().Name}")
        };
        return $"{prefix}--{organizationId}--{secret.Id}";
    }

    public async Task<List<MappedSecretDto>> ListRemoteByIds(List<Guid> ids, Guid organizationId)
    {
        var secrets = await _repository.ListByIds(ids, organizationId);

        // Start all Get operations concurrently
        var tasks = secrets.Select(async secret =>
        {
            using var vault = _vaultFactory.Create(secret.Organization.InputKeyVaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultInputKeyVaultUrl);
            var value = await vault.GetSecretAsync(MakeRemoteSecretName(secret, organizationId));
            return new MappedSecretDto
            {
                Id = secret.Id,
                Value = value
            };
        });

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        // Convert the array to a list and return
        return results.ToList();
    }
}
