using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;

namespace SnapCd.Server.Core.Database.SagaClassMaps;

public class DestroyJobSagaClassMap : SagaClassMap<DestroyJobSaga>
{
    public DestroyJobSagaClassMap()
    {
    }

    protected override void Configure(EntityTypeBuilder<DestroyJobSaga> entity, ModelBuilder modelBuilder)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.CorrelationId, e.OrganizationId });

        // Unique index on CorrelationId
        entity.HasIndex(e => e.CorrelationId).IsUnique();

        entity.Property(x => x.CurrentState).HasMaxLength(64);

        // Configure FK relationship to Module
        var moduleEntity = modelBuilder.Entity<Module>();
        entity
            .HasOne<Module>()
            .WithMany(m => m.DestroyModuleSaga)
            .HasForeignKey(s => new { s.ModuleId, s.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(x => x.RowVersion).IsRowVersion(); // for optimistic concurrency
        
        entity
            .Property(d => d.CurrentState)
            .HasConversion<string>();
        
    }
}