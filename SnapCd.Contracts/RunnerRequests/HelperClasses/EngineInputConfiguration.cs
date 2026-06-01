// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Contracts.Dto.NamespaceInputs;

namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

/// <summary>
/// Complete input configuration needed for Terraform parameter resolution.
/// Only sent once during Init operation - subsequent operations load resolved vars from file.
/// </summary>
public class EngineInputConfiguration
{
    // Namespace inputs (4 collections)
    public List<NamespaceInputFromLiteralReadDto>? NamespaceParamFromLiterals { get; set; }
    public List<NamespaceInputFromLiteralReadDto>? NamespaceEnvVarFromLiterals { get; set; }
    public List<NamespaceInputFromDefinitionReadDto>? NamespaceParamFromDefinitions { get; set; }
    public List<NamespaceInputFromDefinitionReadDto>? NamespaceEnvVarFromDefinitions { get; set; }

    // Module inputs (6 collections)
    public List<ModuleInputFromDefinitionReadDto>? ModuleParamFromDefinitions { get; set; }
    public List<ModuleInputFromLiteralReadDto>? ModuleParamFromLiterals { get; set; }
    public List<ModuleInputFromNamespaceReadDto>? ModuleParamFromNamespaces { get; set; }
    public List<ModuleInputFromDefinitionReadDto>? ModuleEnvVarFromDefinitions { get; set; }
    public List<ModuleInputFromLiteralReadDto>? ModuleEnvVarFromLiterals { get; set; }
    public List<ModuleInputFromNamespaceReadDto>? ModuleEnvVarFromNamespaces { get; set; }

    // Secrets (4 collections)
    public List<SelectedModuleSecret> SelectedModuleParamsFromSecrets { get; set; } = [];
    public List<SelectedModuleSecret> SelectedModuleEnvVarsFromSecrets { get; set; } = [];
    public List<SelectedNamespaceSecret> SelectedNamespaceParamsFromSecrets { get; set; } = [];
    public List<SelectedNamespaceSecret> SelectedNamespaceEnvVarsFromSecrets { get; set; } = [];

    // IDs and metadata needed for param resolution
    public Guid StackId { get; set; }
    public required string StackName { get; set; }
    public Guid NamespaceId { get; set; }
    public required string NamespaceName { get; set; }
    public Guid ModuleId { get; set; }
    public required string ModuleName { get; set; }
    public required string SourceRevision { get; set; }
    public required string SourceUrl { get; set; }
    public required string SourceSubdirectory { get; set; }
}
