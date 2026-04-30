using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.SelfHosted.Database.ClassMaps;

public class SelfHostedOrganizationLicenseClassMap : IEntityTypeConfiguration<SelfHostedOrganizationLicense>
{
    public void Configure(EntityTypeBuilder<SelfHostedOrganizationLicense> entity)
    {
        entity.HasKey(e => e.OrganizationId);

        entity.HasOne(e => e.Organization)
            .WithOne()
            .HasForeignKey<SelfHostedOrganizationLicense>(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
