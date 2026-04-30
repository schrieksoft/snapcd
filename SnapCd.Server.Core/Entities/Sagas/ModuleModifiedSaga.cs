using System.ComponentModel.DataAnnotations;
using MassTransit;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Entities.Sagas;

public class ModuleModifiedSaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    [MaxLength(100)] public string CurrentState { get; set; } = null!;
    public DateTime? LastUpdated { get; set; }
    public Guid? TimeoutTokenId { get; set; }

    public virtual Module? Module { get; set; }

    // Required by EF Core
    public byte[] RowVersion { get; set; } = null!;
}