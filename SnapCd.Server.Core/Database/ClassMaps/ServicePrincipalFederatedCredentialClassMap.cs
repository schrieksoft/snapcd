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

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ServicePrincipalFederatedCredentialClassMap : IEntityTypeConfiguration<ServicePrincipalFederatedCredential>
{
    public void Configure(EntityTypeBuilder<ServicePrincipalFederatedCredential> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasIndex(p => new { p.Name, p.ServicePrincipalId, p.OrganizationId })
            .IsUnique();

        // One trust record per issuer and subject: two rows matching the same presented token
        // would make which one applied arbitrary.
        entity
            .HasIndex(p => new { p.Issuer, p.Subject, p.ServicePrincipalId, p.OrganizationId })
            .IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ServicePrincipalFederatedCredentials)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict, not Cascade: SQL Server allows one cascade path into a table, and the
        // Organization already reaches this one through the Service Principal. The Service
        // Principal repository clears these rows itself on delete.
        entity
            .HasOne(e => e.ServicePrincipal)
            .WithMany(x => x.ServicePrincipalFederatedCredentials)
            .HasForeignKey(e => new { e.ServicePrincipalId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => e.ServicePrincipalId);

        // The assertion path looks a row up by issuer and subject before it knows which Service
        // Principal is involved.
        entity.HasIndex(e => new { e.Issuer, e.Subject });
    }
}
