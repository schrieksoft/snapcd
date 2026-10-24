// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Runner.Services;
using SnapCd.Runner.Services.ModuleSourceRefresher;

namespace SnapCd.Runner.Factories;

public class VariableDiscoveryServiceFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly SnapCdInspect _inspect;

    public VariableDiscoveryServiceFactory(ILoggerFactory loggerFactory, SnapCdInspect inspect)
    {
        _loggerFactory = loggerFactory;
        _inspect = inspect;
    }

    public IVariableDiscoveryService Create(string engine)
    {
        return engine switch
        {
            "terraform" or "tofu" => new TerraformVariableDiscoveryService(
                _loggerFactory.CreateLogger<TerraformVariableDiscoveryService>(), _inspect),
            "pulumi" => new PulumiVariableDiscoveryService(),
            _ => throw new NotSupportedException($"Engine '{engine}' is not supported for variable discovery")
        };
    }
}
