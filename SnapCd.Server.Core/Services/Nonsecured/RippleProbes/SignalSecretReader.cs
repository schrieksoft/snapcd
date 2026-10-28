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
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.RippleProbes;

/// <summary>
/// Reads a sensitive signal's value object back out of the secret store so it can be written into
/// the inputs document.
/// </summary>
public interface ISignalSecretReader
{
    Task<string?> Read(SecretSignalOutput signal, Guid organizationId);
}

public class SignalSecretReader : ISignalSecretReader
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IVaultFactory _vaultFactory;
    private readonly SecretStoreSettings _secretStoreSettings;

    public SignalSecretReader(
        SnapCdDbContext dbContext,
        IVaultFactory vaultFactory,
        IOptions<SecretStoreSettings> secretStoreSettings)
    {
        _dbContext = dbContext;
        _vaultFactory = vaultFactory;
        _secretStoreSettings = secretStoreSettings.Value;
    }

    public async Task<string?> Read(SecretSignalOutput signal, Guid organizationId)
    {

        var vaultUrl = await _dbContext.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => o.InputKeyVaultUrl)
            .FirstOrDefaultAsync();

        using var vault = _vaultFactory.Create(vaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultInputKeyVaultUrl);
        return await vault.GetSecretAsync(signal.RemoteSecretName);
    }
}
