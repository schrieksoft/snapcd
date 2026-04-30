using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.Secrets;
using SnapCd.Server.Core.Entities.Definition.Secrets;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud.Secrets;

public class SecretServiceFactory
{
    private readonly SecretSecuredRepositoryFactory _securedRepositoryFactory;
    private readonly SecretRepositoryFactory _repositoryFactory;
    private readonly StackSecretSecuredRepositoryFactory _stackSecuredRepositoryFactory;
    private readonly NamespaceSecretSecuredRepositoryFactory _namespaceSecuredRepositoryFactory;
    private readonly ModuleSecretSecuredRepositoryFactory _moduleSecuredRepositoryFactory;
    private readonly IVaultFactory _vaultFactory;
    private readonly IOptions<SecretStoreSettings> _secretStoreSettings;

    public SecretServiceFactory(
        SecretSecuredRepositoryFactory securedRepositoryFactory,
        SecretRepositoryFactory repositoryFactory,
        StackSecretSecuredRepositoryFactory stackSecuredRepositoryFactory,
        NamespaceSecretSecuredRepositoryFactory namespaceSecuredRepositoryFactory,
        ModuleSecretSecuredRepositoryFactory moduleSecuredRepositoryFactory,
        IVaultFactory vaultFactory,
        IOptions<SecretStoreSettings> secretStoreSettings
    )
    {
        _securedRepositoryFactory = securedRepositoryFactory;
        _repositoryFactory = repositoryFactory;
        _stackSecuredRepositoryFactory = stackSecuredRepositoryFactory;
        _namespaceSecuredRepositoryFactory = namespaceSecuredRepositoryFactory;
        _moduleSecuredRepositoryFactory = moduleSecuredRepositoryFactory;
        _vaultFactory = vaultFactory;
        _secretStoreSettings = secretStoreSettings;
    }

    public virtual SecretService Create(IPrincipalProvider? principalProvider = null)
    {
        return new SecretService(
            _securedRepositoryFactory.Create(principalProvider),
            _repositoryFactory.Create(),
            _stackSecuredRepositoryFactory.Create(principalProvider),
            _namespaceSecuredRepositoryFactory.Create(principalProvider),
            _moduleSecuredRepositoryFactory.Create(principalProvider),
            _vaultFactory,
            _secretStoreSettings);
    }
}

public class SecretService : GenericCrudService<Secret, SecretCreateDto, SecretUpdateDto, SecretDto, SecretSecuredRepository, SecretRepository, SecretCreatedEvent, SecretUpdatedEvent,
    SecretDeletedEvent, SecretRepositorySettings>
{
    private readonly IVaultFactory _vaultFactory;
    private readonly StackSecretSecuredRepository _stackSecretSecuredRepository;
    private readonly NamespaceSecretSecuredRepository _namespaceSecretSecuredRepository;
    private readonly ModuleSecretSecuredRepository _moduleSecretSecuredRepository;
    private readonly SecretStoreSettings _secretStoreSettings;

    public SecretService(
        SecretSecuredRepository securedRepository,
        SecretRepository repository,
        StackSecretSecuredRepository stackSecretSecuredRepository,
        NamespaceSecretSecuredRepository namespaceSecretSecuredRepository,
        ModuleSecretSecuredRepository moduleSecretSecuredRepository,
        IVaultFactory vaultFactory,
        IOptions<SecretStoreSettings> secretStoreSettings
    ) : base(securedRepository)
    {
        _vaultFactory = vaultFactory;
        _stackSecretSecuredRepository = stackSecretSecuredRepository;
        _namespaceSecretSecuredRepository = namespaceSecretSecuredRepository;
        _moduleSecretSecuredRepository = moduleSecretSecuredRepository;
        _secretStoreSettings = secretStoreSettings.Value;
    }

    protected override Secret MapToEntity(SecretCreateDto dto, Guid organizationId)
    {
        return SecretMapper.ToEntity(dto, organizationId);
    }

    protected override SecretDto MapToDto(Secret entity)
    {
        return SecretMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(Secret entity, SecretUpdateDto dto)
    {
        SecretMapper.UpdateEntity(entity, dto);
    }

    public override void Dispose()
    {
        base.Dispose();
        SecuredRepository.Repository.Dispose();
        _stackSecretSecuredRepository.Dispose();
        _namespaceSecretSecuredRepository.Dispose();
        _moduleSecretSecuredRepository.Dispose();
    }

    public async Task<SecretDto> GetByName(string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.GetByName(name, organizationId, null));
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


    public async Task<string> GetRemote(Guid id, Guid organizationId)
    {
        if (!SecuredRepository.CanRead(id, organizationId))
            throw new PrincipalNotAuthorizedException($"Principal is not allowd to read denied to Secret {id}");

        var secret = await SecuredRepository.Get(id, organizationId);
        var inputKeyVaultUrl = await GetInputKeyVault(organizationId);

        using var vault = _vaultFactory.Create(inputKeyVaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultInputKeyVaultUrl);
        var value = await vault.GetSecretAsync(MakeRemoteSecretName(secret, organizationId));
        return value;
    }

    public virtual async Task<string> GetRemoteNonsecured(Guid id, Guid organizationId)
    {
        var secret = await SecuredRepository.Repository.Get(id, organizationId);
        var inputKeyVaultUrl = await GetInputKeyVault(organizationId);

        using var vault = _vaultFactory.Create(inputKeyVaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultInputKeyVaultUrl);
        var value = await vault.GetSecretAsync(MakeRemoteSecretName(secret, organizationId));
        return value;
    }


    private bool CanWriteRemote(Secret secret, Guid organizationId)
    {
        // Check permissions including inherited permissions from the secret's scope
        return secret switch
        {
            StackSecret stackSecret => _stackSecretSecuredRepository.CanCreate(stackSecret.StackId, organizationId),
            NamespaceSecret namespaceSecret => _namespaceSecretSecuredRepository.CanCreate(namespaceSecret.NamespaceId, organizationId),
            ModuleSecret moduleSecret => _moduleSecretSecuredRepository.CanCreate(moduleSecret.ModuleId, organizationId),
            _ => false
        };
    }


    private async Task<Secret> Create(Secret secret)
    {
        // Check permissions including inherited permissions from the secret's scope
        return secret switch
        {
            StackSecret stackSecret => await _stackSecretSecuredRepository.Create(stackSecret, false),
            NamespaceSecret namespaceSecret => await _namespaceSecretSecuredRepository.Create(namespaceSecret, false),
            ModuleSecret moduleSecret => await _moduleSecretSecuredRepository.Create(moduleSecret, false),
            _ => throw new InvalidOperationException($"Unsupported secret type: {secret.GetType().Name}")
        };
    }

    private async Task<Secret> Update(Secret secret)
    {
        // Check permissions including inherited permissions from the secret's scope
        return secret switch
        {
            StackSecret stackSecret => await _stackSecretSecuredRepository.Update(stackSecret, false),
            NamespaceSecret namespaceSecret => await _namespaceSecretSecuredRepository.Update(namespaceSecret, false),
            ModuleSecret moduleSecret => await _moduleSecretSecuredRepository.Update(moduleSecret, false),
            _ => throw new InvalidOperationException($"Unsupported secret type: {secret.GetType().Name}")
        };
    }


    private async Task<string?> GetInputKeyVault(Guid organizationId)
    {
        var inputKeyVaultUrl = await SecuredRepository.Repository.DbContext.Organizations
            .Where(x => x.Id == organizationId)
            .Select(x => x.InputKeyVaultUrl)
            .FirstOrDefaultAsync();

        return inputKeyVaultUrl;
    }

    public async Task<string> SetRemote(Secret secret, string value, Guid organizationId)
    {
        if (!CanWriteRemote(secret, organizationId))
            throw new PrincipalNotAuthorizedException($"Access denied to remote set Secret {secret.Id}");

        var inputKeyVaultUrl = await GetInputKeyVault(organizationId);

        using var vault = _vaultFactory.Create(inputKeyVaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultInputKeyVaultUrl);
        return await vault.SetSecretAsync(MakeRemoteSecretName(secret, organizationId), value);
    }

    public async Task<string> SetRemoteIfChanged(Secret secret, string value, Guid organizationId)
    {
        // Check permissions including inherited permissions from the secret's scope
        if (!CanWriteRemote(secret, organizationId))
            throw new PrincipalNotAuthorizedException($"Access denied to remote set Secret {secret.Id}");

        var inputKeyVaultUrl = await GetInputKeyVault(organizationId);

        using var vault = _vaultFactory.Create(inputKeyVaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultInputKeyVaultUrl);
        var result = await vault.SetIfChanged(MakeRemoteSecretName(secret, organizationId), value);
        return result.Version;
    }

    public override async Task Delete(Guid id, Guid organizationId)
    {
        var secret = await SecuredRepository.Get(id, organizationId);
        var inputKeyVaultUrl = await GetInputKeyVault(organizationId);

        await using var transaction = await SecuredRepository.Repository.DbContext.Database.BeginTransactionAsync();
        try
        {
            await SecuredRepository.Delete(id, organizationId, false);

            using var vault = _vaultFactory.Create(inputKeyVaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultInputKeyVaultUrl);
            await vault.DeleteSecretAsync(MakeRemoteSecretName(secret, organizationId));

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task Create(Secret secret, Guid organizationId, string secretValue)
    {
        await using var transaction = await SecuredRepository.Repository.DbContext.Database.BeginTransactionAsync();
        try
        {
            var createdSecret = await Create(secret);
            await SetRemote(createdSecret, secretValue, organizationId);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task Update(Secret secret, Guid organizationId, string secretValue)
    {
        await using var transaction = await SecuredRepository.Repository.DbContext.Database.BeginTransactionAsync();
        try
        {
            var updatedSecret = await Update(secret);
            await SetRemoteIfChanged(updatedSecret, secretValue, organizationId);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}