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
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.RippleProbes;

/// <summary>
/// Puts a sensitive signal's value object in the secret store, so the row holds a reference rather
/// than the value.
/// </summary>
public interface ISignalSecretWriter
{
    Task Write(string remoteSecretName, string valueObjectJson, Guid organizationId);
}

public class SignalSecretWriter : ISignalSecretWriter
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IVaultFactory _vaultFactory;
    private readonly SecretStoreSettings _secretStoreSettings;

    public SignalSecretWriter(
        SnapCdDbContext dbContext,
        IVaultFactory vaultFactory,
        IOptions<SecretStoreSettings> secretStoreSettings)
    {
        _dbContext = dbContext;
        _vaultFactory = vaultFactory;
        _secretStoreSettings = secretStoreSettings.Value;
    }

    public async Task Write(string remoteSecretName, string valueObjectJson, Guid organizationId)
    {

        var vaultUrl = await _dbContext.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => o.InputKeyVaultUrl)
            .FirstOrDefaultAsync();

        using var vault = _vaultFactory.Create(vaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultInputKeyVaultUrl);
        await vault.SetSecretAsync(remoteSecretName, valueObjectJson);
    }
}
