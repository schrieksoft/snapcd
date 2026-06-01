// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.VariableSets;

namespace SnapCd.Runner.Services;

public class PulumiVariableDiscoveryService : IVariableDiscoveryService
{
    public Task<VariableSetCreateDto?> CreateVariableSet(
        string directoryPath,
        Guid moduleId,
        ISet<string>? extraFileNames = null)
    {
        // Pulumi variables are defined in application code (TypeScript/Python/Go/C#),
        // not in declarative files we can reliably parse. Returning null means no
        // VariableSet is stored, so OutputSetParamResolver skips filtering and all
        // selected outputs are injected as inputs.
        return Task.FromResult<VariableSetCreateDto?>(null);
    }

    public Task<Dictionary<string, bool>> DiscoverOutputSourcesAsync(
        string directoryPath,
        ISet<string>? extraFileNames = null)
    {
        // Pulumi outputs are defined in code, not in declarative files we can scan.
        // Return empty - outputs will be discovered at runtime via `pulumi stack output`.
        return Task.FromResult(new Dictionary<string, bool>());
    }
}
