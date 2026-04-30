using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.SelfHosted.Database.ClassMaps;

public class SecretMigrationAuditClassMap : IEntityTypeConfiguration<SecretMigrationAudit>
{
    public void Configure(EntityTypeBuilder<SecretMigrationAudit> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.RunId);
        entity.HasIndex(e => new { e.OrganizationId, e.RunStartedUtc });
    }
}
