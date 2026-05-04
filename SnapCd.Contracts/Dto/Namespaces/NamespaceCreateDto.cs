namespace SnapCd.Contracts.Dto.Namespaces;

public class NamespaceCreateDto
{

    public string Name { get; set; } = null!;

    public Guid? StackId { get; set; }

    public bool? DefaultCleanInitEnabled { get; set; }

    public int? DefaultApplyApprovalThreshold { get; set; }

    public int? DefaultDestroyApprovalThreshold { get; set; }

    public int? DefaultApprovalTimeoutMinutes { get; set; }

    public StateManagementEngine? DefaultEngine { get; set; }

    public NamespaceTriggerBehaviour? TriggerBehaviourOnModified { get; set; }

    public bool? DefaultDriftCheckEnabled { get; set; }
    public int? DefaultDriftCheckIntervalMinutes { get; set; }
}