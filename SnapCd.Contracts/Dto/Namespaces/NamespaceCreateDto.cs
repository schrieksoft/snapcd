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

    /// <summary>ID of the Namespace this one sits inside. Leave unset for a Namespace at the top level.</summary>
    public Guid? ParentNamespaceId { get; set; }

    /// <summary>Assigned when the Namespace is created and never changed. Read only.</summary>
    public int Number { get; set; }

    /// <summary>What Modules below this Namespace give to consumers outside it. Must be 'All' or 'SensitiveOnly'. Unset places no restriction.</summary>
    public OutputSetDownstreamRestriction? DefaultOutputSetDownstreamRestriction { get; set; }

    /// <summary>If this is set to true, any Extra Files inherited from the Namespaces above this one will not be used.</summary>
    public bool IgnoreInheritedExtraFiles { get; set; }
    /// <summary>If this is set to true, any Flags inherited from the Namespaces above this one will not be used.</summary>
    public bool IgnoreInheritedFlags { get; set; }
    /// <summary>If this is set to true, any Hooks inherited from the Namespaces above this one will not be used.</summary>
    public bool IgnoreInheritedHooks { get; set; }
    /// <summary>If this is set to true, any Policies inherited from the Namespaces above this one will not be evaluated.</summary>
    public bool IgnoreInheritedPolicies { get; set; }
    /// <summary>If this is set to true, any Missions inherited from the Namespaces above this one will not match.</summary>
    public bool IgnoreInheritedMissions { get; set; }
    /// <summary>If this is set to true, any Integration Events inherited from the Namespaces above this one will not fire.</summary>
    public bool IgnoreInheritedIntegrationEvents { get; set; }
    /// <summary>If this is set to true, any Additional Trigger Paths inherited from the Namespaces above this one will not apply.</summary>
    public bool IgnoreInheritedTriggerPaths { get; set; }

    /// <summary>If this is set to true, a Namespace or Module below this one cannot set its own clean-init setting; this Namespace's value is used throughout its subtree.</summary>
    public bool? RestrictOverrideOnCleanInitEnabled { get; set; }
    /// <summary>If this is set to true, a Namespace or Module below this one cannot set its own path-scoped triggering setting; this Namespace's value is used throughout its subtree.</summary>
    public bool? RestrictOverrideOnTriggerPathFilterEnabled { get; set; }
    /// <summary>If this is set to true, a Namespace or Module below this one cannot set its own 'Apply' approval threshold; this Namespace's value is used throughout its subtree.</summary>
    public bool? RestrictOverrideOnApplyApprovalThreshold { get; set; }
    /// <summary>If this is set to true, a Namespace or Module below this one cannot set its own 'Destroy' approval threshold; this Namespace's value is used throughout its subtree.</summary>
    public bool? RestrictOverrideOnDestroyApprovalThreshold { get; set; }
    /// <summary>If this is set to true, a Namespace or Module below this one cannot set its own state migration approval threshold; this Namespace's value is used throughout its subtree.</summary>
    public bool? RestrictOverrideOnStateMigrationApprovalThreshold { get; set; }
    /// <summary>If this is set to true, a Namespace or Module below this one cannot set its own approval timeout; this Namespace's value is used throughout its subtree.</summary>
    public bool? RestrictOverrideOnApprovalTimeoutMinutes { get; set; }
    /// <summary>If this is set to true, a Namespace or Module below this one cannot set its own engine; this Namespace's value is used throughout its subtree.</summary>
    public bool? RestrictOverrideOnEngine { get; set; }
    /// <summary>If this is set to true, a Namespace or Module below this one cannot set its own drift check setting; this Namespace's value is used throughout its subtree.</summary>
    public bool? RestrictOverrideOnDriftCheckEnabled { get; set; }
    /// <summary>If this is set to true, a Namespace or Module below this one cannot set its own drift check interval; this Namespace's value is used throughout its subtree.</summary>
    public bool? RestrictOverrideOnDriftCheckIntervalMinutes { get; set; }
    /// <summary>If this is set to true, a Namespace or Module below this one cannot set its own downstream output restriction; this Namespace's value is used throughout its subtree.</summary>
    public bool? RestrictOverrideOnOutputSetDownstreamRestriction { get; set; }

    /// <summary>Setting will remove all .terraform* files and folders (state files, locks, downloaded providers, downloaded modules etc.) and perform a clean init every time the Module is executed. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself.</summary>
    public bool? DefaultCleanInitEnabled { get; set; }

    /// <summary>Defaults to 'false'. If 'true', Modules in this Namespace with `trigger_on_source_changed` only fire when a commit actually changes their watched directories instead of on every commit to the tracked source. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself.</summary>
    public bool? DefaultTriggerPathFilterEnabled { get; set; }

    /// <summary>The number of Users (or Service Principals) that need to approve before an 'Apply' plan is executed. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself. If set neither on Module nor on Namespace then a threshold of 0 is used.</summary>
    public int? DefaultApplyApprovalThreshold { get; set; }

    /// <summary>The number of Users (or Service Principals) that need to approve before an 'Destroy' plan is executed. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself. If set neither on Module nor on Namespace then a threshold of 0 is used.</summary>
    public int? DefaultDestroyApprovalThreshold { get; set; }

    /// <summary>The number of Users (or Service Principals) that need to approve before a state migration (a split or transfer job) pushes state. All modules in this Namespace will use this value, unless explicitly overriden on the Module itself. If set neither on Module nor on Namespace then a threshold of 1 is used, since the push is irreversible.</summary>
    public int? DefaultStateMigrationApprovalThreshold { get; set; }

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