// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.Crud.Interfaces;

namespace SnapCd.Server.Core.Factories;

public class NamespaceInputFromSecretServiceFactory
{
    private readonly NamespaceInputFromSecretService<NamespaceParamFromSecret> _paramService;
    private readonly NamespaceInputFromSecretService<NamespaceEnvVarFromSecret> _envVarService;

    public NamespaceInputFromSecretServiceFactory(
        NamespaceInputFromSecretService<NamespaceParamFromSecret> paramService,
        NamespaceInputFromSecretService<NamespaceEnvVarFromSecret> envVarService)
    {
        _paramService = paramService;
        _envVarService = envVarService;
    }

    public INamespaceInputFromSecretService GetService(InputKind inputKind)
    {
        return inputKind switch
        {
            InputKind.Param => _paramService,
            InputKind.EnvVar => _envVarService,
            _ => throw new ArgumentException($"Unsupported InputKind: {inputKind}")
        };
    }
}