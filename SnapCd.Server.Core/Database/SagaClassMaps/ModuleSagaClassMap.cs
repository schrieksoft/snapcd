using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;

namespace SnapCd.Server.Core.Database.SagaClassMaps;

public class ModuleSagaClassMap : SagaClassMap<ModuleSaga>
{
    public ModuleSagaClassMap()
    {
    }

    protected override void Configure(EntityTypeBuilder<ModuleSaga> entity, ModelBuilder modelBuilder)
    {
        entity.Property(x => x.CurrentState).HasMaxLength(64);

        entity.Property(x => x.RowVersion).IsRowVersion(); // for optimistic concurrency

        entity
            .Property(d => d.DesiredStateHeadline)
            .HasConversion<string>();

        entity
            .Property(d => d.QueuedDesiredStateHeadline)
            .HasConversion<string>();

        entity
            .Property(d => d.QueuedReason)
            .HasConversion<string>();

        entity
            .HasOne(e => e.Module)
            .WithOne(u => u.ModuleSaga)
            .HasForeignKey<ModuleSaga>(e => new { e.CorrelationId, e.OrganizationId })
            .HasPrincipalKey<Module>(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on CorrelationId alone (since it maps to Module.Id)
        entity
            .HasIndex(e => e.CorrelationId)
            .IsUnique();
    }
}