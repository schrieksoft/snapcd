// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Namespaces;

/// <summary>DTO for creating a new Namespace (POST operations).</summary>
public class NamespaceCreateDto
{

    /// <summary>Name of the Namespace. Must be unique in combination with `stack_id`.</summary>
    public string Name { get; set; } = null!;

    /// <summary>ID of the Namespace's parent Stack.</summary>
    public Guid? StackId { get; set; }

    /// <summary>Setting will remove all .terraform* files and folders (state files, locks, downloaded providers, downloaded modules etc.) and perform a clean init every time the Module is executed. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself.</summary>
    public bool? DefaultCleanInitEnabled { get; set; }

    /// <summary>The number of Users (or Service Principals) that need to approve before an 'Apply' plan is executed. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself. If set neither on Module nor on Namespace then a threshold of 0 is used.</summary>
    public int? DefaultApplyApprovalThreshold { get; set; }

    /// <summary>The number of Users (or Service Principals) that need to approve before an 'Destroy' plan is executed. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself. If set neither on Module nor on Namespace then a threshold of 0 is used.</summary>
    public int? DefaultDestroyApprovalThreshold { get; set; }

    /// <summary>The number of minutes a Job should remain in the 'WaitingForApproval' in the case of an 'Apply' or 'Destroy' plan that requires approval. After this time elapses the Job will be stopped and any queued Jobs will start. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself. If set neither on Module nor on Namespace the Jobs will wait for an approval decision indefinitely.</summary>
    public int? DefaultApprovalTimeoutMinutes { get; set; }

    /// <summary>Determines which binary will be used during deployment. Must be one of 'OpenTofu', 'Terraform' or 'Pulumi'. Setting this to 'OpenTofu' will use `tofu`. Setting it to 'Terraform' will use `terraform`. Setting this to 'Pulumi' will use `pulumi`. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself.</summary>
    public StateManagementEngine? DefaultEngine { get; set; }

    /// <summary>Behaviour with respect to applying modules within the Namespace if any of the fields on the Namespace resource (or any of its Param, Env Var or Extra File resources) has changed. Must be one of 'TriggerAllImmediately' or 'DoNotTrigger'. Setting to 'TriggerAllImmediately' will trigger *all* Modules within the Stack to run an apply Job simultaneously. Setting to 'DoNotTrigger' will do nothing. The default (and recommended) setting is 'DoNotTrigger'.</summary>
    public NamespaceTriggerBehaviour? TriggerBehaviourOnModified { get; set; }

    /// <summary>Setting this to true will periodically trigger an Apply job to check for drift in the deployed infrastructure. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself.</summary>
    public bool? DefaultDriftCheckEnabled { get; set; }
    /// <summary>The number of minutes between drift checks. If not set, the system default (24 hours) is used. Note that irrespective of what is set here, these those will not be fired more regularly than the minimum internal as defined by your subscription tier. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself.</summary>
    public int? DefaultDriftCheckIntervalMinutes { get; set; }
}