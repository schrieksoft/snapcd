// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
