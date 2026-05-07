using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Host.Database.ClassMaps;

public class VaultSecretClassMap : IEntityTypeConfiguration<VaultSecret>
{
    public void Configure(EntityTypeBuilder<VaultSecret> entity)
    {
        entity.HasKey(e => e.Name);
    }
}
