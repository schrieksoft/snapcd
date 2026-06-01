// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Views;

/// <summary>
/// View model containing only the vault URL information needed to retrieve a secret's value.
/// Used to avoid loading the entire Secret and Organization entities when fetching remote secrets.
/// </summary>
public class SecretVaultInfoView
{
    /// <summary>
    /// The Key Vault URL configured for the organization's input secrets.
    /// Null if using the default vault URL.
    /// </summary>
    public string? InputKeyVaultUrl { get; init; }
}