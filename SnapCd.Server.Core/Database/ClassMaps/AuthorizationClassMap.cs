using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class AuthorizationClassMap : IEntityTypeConfiguration<Authorization>
{
    public void Configure(EntityTypeBuilder<Authorization> entity)
    {
        entity
            .HasOne(t => t.Application)
            .WithMany(sp => sp.Authorizations)
            .OnDelete(DeleteBehavior.Cascade);
    }
}