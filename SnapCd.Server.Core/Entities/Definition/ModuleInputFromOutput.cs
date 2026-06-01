// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleInputFromOutput : ModuleInputWithOutputModuleId, IModuleInputFromOutput
{
    [MaxLength(255)] public string OutputName { get; set; } = null!;

    [JsonIgnore] public Module OutputModule { get; set; } = null!;
}

public class ModuleParamFromOutput : ModuleInputFromOutput
{
    public override InputKind InputKind { get; init; } = InputKind.Param;
}

public class ModuleEnvVarFromOutput : ModuleInputFromOutput
{
    public override InputKind InputKind { get; init; } = InputKind.EnvVar;
}

public class ModuleInputWithOutputModuleId : ModuleInput
{
    public Guid OutputModuleId { get; set; }
    // Note: OrganizationId inherited from ModuleInput base class is used as part of the composite foreign key
}