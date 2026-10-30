// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Azure;
using Azure.Security.KeyVault.Secrets;
using SnapCd.Server.Core.Services.Interfaces.Vaults;

namespace SnapCd.Server.Core.Services.Utils.Vaults;

public class AzureVault : IVault
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<AzureVault> _logger;

    public AzureVault(string keyVaultUrl, Azure.Core.TokenCredential credential,
        ILogger<AzureVault> logger)
    {
        if (string.IsNullOrEmpty(keyVaultUrl))
            throw new ArgumentException("Key Vault URL cannot be null or empty", nameof(keyVaultUrl));

        _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        _logger = logger;
    }

    public void Dispose()
    {
    }

    public async Task<SetIfChangedResult> SetIfChanged(string secretName, string value)
    {
        // Retrieve the current secret value
        try
        {
            var currentSecret = await _secretClient.GetSecretAsync(secretName);
            if (currentSecret.Value.Value == value)
            {
                _logger.LogInformation($"Secret with name \"{secretName}\" already exists with same value. Doing nothing.");
                return new SetIfChangedResult(currentSecret.Value.Properties.Version, WasChanged: false);
            }

            _logger.LogInformation($"Secret with name \"{secretName}\" already exists, but value differs. Updating value.");
        }
        catch (RequestFailedException ex)
        {
            if (ex.Status == 404)
                _logger.LogInformation($"Secret with name \"{secretName}\" does not exist, now setting it for the first time");
            else
                throw;
        }

        var newVersion = await SetSecretAsync(secretName, value);
        return new SetIfChangedResult(newVersion, WasChanged: true);
    }

    public async Task<string> SetSecretAsync(string name, string value)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
            throw new ArgumentException("Secret name and value cannot be null or empty");

        try
        {
            KeyVaultSecret secret = await _secretClient.SetSecretAsync(new KeyVaultSecret(name, value));
            return secret.Properties.Version;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            // Key Vault refuses a write to a name held in the soft-deleted state. The name has to
            // be recovered before it can be written again.
            _logger.LogInformation("Secret '{SecretName}' is soft-deleted; recovering it before writing.", name);

            var recoverOperation = await _secretClient.StartRecoverDeletedSecretAsync(name);
            await recoverOperation.WaitForCompletionAsync();

            KeyVaultSecret secret = await _secretClient.SetSecretAsync(new KeyVaultSecret(name, value));
            return secret.Properties.Version;
        }
    }

    public async Task<string> GetSecretAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(name));

        KeyVaultSecret secret = await _secretClient.GetSecretAsync(name);
        return secret.Value;
    }

    public async Task<VersionedSecret> GetSecretWithVersionAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(name));

        KeyVaultSecret secret = await _secretClient.GetSecretAsync(name);
        return new VersionedSecret(secret.Value, secret.Properties.Version);
    }

    public async Task<bool> IsSoftDeletedAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(name));

        try
        {
            await _secretClient.GetDeletedSecretAsync(name, ct);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task DeleteSecretAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(name));

        try
        {
            // Soft delete only. The value stays recoverable for the vault's retention period,
            // which is what makes a half-completed delete repairable; SetSecretAsync recovers
            // the name if it is ever written again.
            var deleteOperation = await _secretClient.StartDeleteSecretAsync(name);
            await deleteOperation.WaitForCompletionAsync();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Secret already deleted or doesn't exist - this is fine
            _logger.LogInformation("Secret '{SecretName}' not found or already deleted.", name);
        }
        catch (Exception ex)
        {
            // Rethrow any other unexpected errors
            throw new Exception($"Failed to delete secret '{name}'.", ex);
        }
    }
}