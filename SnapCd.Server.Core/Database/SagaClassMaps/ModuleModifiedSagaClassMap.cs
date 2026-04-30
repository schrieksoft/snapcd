using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Sagas;

namespace SnapCd.Server.Core.Database.SagaClassMaps;

public class ModuleModifiedSagaClassMap : SagaClassMap<ModuleModifiedSaga>
{
    public ModuleModifiedSagaClassMap()
    {
    }

    protected override void Configure(EntityTypeBuilder<ModuleModifiedSaga> entity, ModelBuilder modelBuilder)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.CorrelationId, e.OrganizationId });

        // Unique index on CorrelationId
        entity.HasIndex(e => e.CorrelationId).IsUnique();

        entity.Property(x => x.CurrentState).HasMaxLength(64);

        entity.Property(x => x.RowVersion).IsRowVersion(); // for optimistic concurrency
    }
}