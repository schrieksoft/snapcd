// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.Vaults;

/// <summary>
/// Result of a SetIfChanged operation, indicating the version and whether the value was changed.
/// </summary>
/// <param name="Version">The version identifier of the secret (new version if changed, current version if unchanged).</param>
/// <param name="WasChanged">True if the secret was created or updated, false if it already existed with the same value.</param>
public record SetIfChangedResult(string Version, bool WasChanged);

public interface IVault : IDisposable
{
    public Task<SetIfChangedResult> SetIfChanged(string secretName, string value);
    public Task<string> GetSecretAsync(string secretName);
    public Task<string> SetSecretAsync(string secretName, string value);

    public Task DeleteSecretAsync(string secretName);
}