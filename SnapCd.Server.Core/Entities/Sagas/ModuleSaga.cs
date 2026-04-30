using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Sagas;

public class ModuleSaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    [MaxLength(100)] public string CurrentState { get; set; } = null!;

    public DesiredStateHeadline? DesiredStateHeadline { get; set; }

    public DesiredStateHeadline? QueuedDesiredStateHeadline { get; set; }

    public QueuedReason? QueuedReason { get; set; }

    [MaxLength(255)] public string? DesiredDefinitiveRevision { get; set; }

    [JsonIgnore] public Module Module { get; set; } = null!;

    public int? ActualResourceCount { get; set; }

    public Guid? DriftCheckScheduleTokenId { get; set; }
}