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