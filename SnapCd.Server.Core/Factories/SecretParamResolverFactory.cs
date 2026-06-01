// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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