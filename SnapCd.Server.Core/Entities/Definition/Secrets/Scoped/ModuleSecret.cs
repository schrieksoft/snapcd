// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

public class ModuleSecret : Secret, IModuleSecret, IModuleChild
{
    public override SecretScope ScopeKind { get; init; } = SecretScope.Module;

    public Guid ModuleId { get; set; }

    public Module Module { get; set; } = null!;

    public override Guid ParentId()
    {
        return ModuleId;
    }

    public virtual SecretDiscriminator GetSecretDiscriminator()
    {
        return SecretDiscriminator.ModuleSecret;
    }
}