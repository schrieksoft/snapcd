// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Azure;
using Azure.Security.KeyVault.Secrets;

namespace SnapCd.Server.Core.Services.Vaults;

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

        KeyVaultSecret secret = await _secretClient.SetSecretAsync(new KeyVaultSecret(name, value));

        return secret.Properties.Version;
    }

    public async Task<string> GetSecretAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(name));

        KeyVaultSecret secret = await _secretClient.GetSecretAsync(name);
        return secret.Value;
    }

    public async Task DeleteSecretAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(name));

        try
        {
            // Start the deletion process
            var deleteOperation = await _secretClient.StartDeleteSecretAsync(name);

            // Wait for the deletion to complete (optional)
            await deleteOperation.WaitForCompletionAsync();

            // If soft-delete is enabled and you want to purge immediately (optional)
            // TODO do not necessarily want to purge, but then need to better handle recreation of soft-deleted secrets
            await _secretClient.PurgeDeletedSecretAsync(name);
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