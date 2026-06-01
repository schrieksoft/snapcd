// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Base;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleInputFromNamespaceClassMap : IEntityTypeConfiguration<ModuleInputFromNamespace>
{
    public void Configure(EntityTypeBuilder<ModuleInputFromNamespace> entity)
    {
        // NamespaceInput relationship - use Restrict to prevent deletion of NamespaceInput if ModuleInputs reference it
        // This breaks the potential cycle: Module -> ModuleInput -> NamespaceInput -> Namespace -> Module
        entity
            .HasOne<NamespaceInput>()
            .WithMany()
            .HasForeignKey(e => new { e.NamespaceInputId, e.OrganizationId })
            .HasPrincipalKey(ni => new { ni.Id, ni.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key index
        entity
            .HasIndex(p => p.NamespaceInputId);
    }
}