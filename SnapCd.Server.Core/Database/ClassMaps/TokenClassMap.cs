using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class TokenClassMap : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> entity)
    {
        entity
            .HasOne(t => t.Application)
            .WithMany(sp => sp.Tokens)
            .OnDelete(DeleteBehavior.Cascade);
    }
}