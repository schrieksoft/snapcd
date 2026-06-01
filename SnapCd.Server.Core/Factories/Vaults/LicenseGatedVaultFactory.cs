// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Licensing;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Services.Vaults;

namespace SnapCd.Server.Core.Factories.Vaults;

/// <summary>
/// Decorates the configured <see cref="IVaultFactory"/> (resolved as the keyed "inner" service)
/// with a per-call licence check. When <see cref="IPremiumSecretStorePolicy"/> denies, throws
/// <see cref="LicenceFeatureUnavailableException"/> so the caller sees a clear server-side error.
/// SecretMigratorService is structurally exempt because it injects the concrete factories
/// (AzureVaultFactory / SqlVaultFactory) directly, bypassing this decorator.
/// </summary>
public class LicenseGatedVaultFactory : IVaultFactory
{
    public const string InnerKey = "inner";

    private readonly IVaultFactory _inner;
    private readonly IPremiumSecretStorePolicy _policy;
    private readonly ILogger<LicenseGatedVaultFactory> _logger;

    public LicenseGatedVaultFactory(
        [FromKeyedServices(InnerKey)] IVaultFactory inner,
        IPremiumSecretStorePolicy policy,
        ILogger<LicenseGatedVaultFactory> logger)
    {
        _inner = inner;
        _policy = policy;
        _logger = logger;
    }

    public IVault Create(string vaultUrl)
    {
        // IVaultFactory.Create is sync; the policy is async but the SH impl caches its decision
        // so 99%+ of calls hit the in-memory cache. Using GetAwaiter().GetResult() here is the
        // pragmatic bridge — making IVaultFactory async would touch every caller.
        if (!_policy.IsAllowedAsync().GetAwaiter().GetResult())
        {
            _logger.LogWarning("Vault create rejected for url '{VaultUrl}': PremiumSecretStore feature not licensed.", vaultUrl);
            throw new LicenceFeatureUnavailableException(Feature.PremiumSecretStore,
                "Configured secret-store backend requires the PremiumSecretStore feature. " +
                "Either set SecretStore:Provider=SqlServer or upgrade to a Lite/Enterprise licence.");
        }

        return _inner.Create(vaultUrl);
    }
}
