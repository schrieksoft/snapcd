using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.Outputs;

namespace SnapCd.Server.Core.Database.ClassMaps.Secrets.Scoped;

public class SecretOutputClassMap : IEntityTypeConfiguration<SecretOutput>
{
    public void Configure(EntityTypeBuilder<SecretOutput> entity)
    {
    }
}