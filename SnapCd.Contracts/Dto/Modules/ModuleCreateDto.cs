// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Modules;

/// <summary>
/// DTO for creating a new Module (POST operations).
/// </summary>
public class ModuleCreateDto
{
    /// <summary>Name of the Module. Must be unique in combination with `namespace_id`.</summary>
    public string Name { get; set; } = null!;
    /// <summary>ID of the Module's parent Namespace.</summary>
    public Guid NamespaceId { get; set; }

    /// <summary>Remote revision (e.g. version number, branch, commit or tag) where the source module code is found.</summary>
    public string SourceRevision { get; set; } = null!;
    /// <summary>Remote URL where the source module code is found.</summary>
    public string SourceUrl { get; set; } = null!;

    /// <summary>Subdirectory where the source module code is found.</summary>
    public string SourceSubdirectory { get; set; } = null!;

    /// <summary>Names of Modules in the same Namespace that this Module depends on. Apply jobs wait on these dependencies according to `waitForApplyDependencies`.</summary>
    public List<string>? DependsOnModules { get; set; }

    /// <summary>The type of remote module store that the source module code should be retrieved from. Must be one of 'Git' or 'Registry'</summary>
    public SourceType SourceType { get; set; } = SourceType.Git;

    /// <summary>How Snap CD should interpret the `source_revision` field. Must be one of 'Default' or 'SemanticVersionRange'. Setting to 'Default' means Snap CD will interpret the revision type based on the source type (for example, for a 'Git' source type it will automatically figure out whether the `source_revision` refers to a branch, tag or commit). Setting to 'SemanticVersionRange' means that Snap CD will resolve the revision to a semantic version line `vX.Y.Z` (alternatively witout the 'v' prefix of that is how your semantic version are tagged, i.e. 'X.Y.Z'). It will take the highest version within the major or minor version range that you specify. For example, specify `v2.20.*` or `v2.*`. You can also specify a specific semantic version here, e.g. `v2.20.7`. In that case the behaviour is the same as with when using 'Default', except that only valid semantic versions are accepted. NOTE that 'SemanticVersionRange' is currently only supported in combination with the 'Git' `source_type`.</summary>
    public SourceRevisionType SourceRevisionType { get; set; } = SourceRevisionType.Default;

    /// <summary>Defaults to 'OnFirstApply'. Controls when the Module should wait for dependencies during apply operations. Valid values are 'Always', 'Never', or 'OnFirstApply'. 'Always' means the Module will always wait for Modules its depends on to reach the 'Applied' state before applying. 'Never' means dependencies are ignored. 'OnFirstApply' means the Module will wait for dependencies only on its first apply.</summary>
    public WaitForApplyDependencies WaitForApplyDependencies { get; set; } = WaitForApplyDependencies.OnFirstApply;

    /// <summary>Defaults to 'Always'. Controls when the Module should wait for dependencies during destroy operations. Valid values are 'Always' or 'Never'. 'Always' means the Module will always wait Modules that depend on it to reach the 'Destroyed' state before destroying. 'Never' means dependencies are ignored.</summary>
    public WaitForDestroyDependencies WaitForDestroyDependencies { get; set; } = WaitForDestroyDependencies.Always;

    /// <summary>The number of Users (or Service Principals) that need to approve before an 'Apply' plan is executed. Setting this will override any default value set on the Module's parent Namespace. If set neither on Module nor on Namespace then a threshold of 0 is used.</summary>
    public int? ApplyApprovalThreshold { get; set; }

    /// <summary>The number of Users (or Service Principals) that need to approve before an 'Destroy' plan is executed. Setting this will override any default value set on the Module's parent Namespace. If set neither on Module nor on Namespace then a threshold of 0 is used.</summary>
    public int? DestroyApprovalThreshold { get; set; }

    /// <summary>The number of minutes a Job should remain in the 'WaitingForApproval' in the case of an 'Apply' or 'Destroy' plan that requires approval. After this time elapses the Job will be stopped and any queued Jobs will start. Setting this will override any default value set on the Module's parent Namespace. If set neither on Module nor on Namespace the Jobs will wait for an approval decision indefinitely.</summary>
    public int? ApprovalTimeoutMinutes { get; set; }

    /// <summary>ID of the Runner that will receive the instructions when triggering a deployment on this Module.</summary>
    public Guid RunnerId { get; set; }
    /// <summary>Name a specific runner instance to select (should unique identify the the instance). Use this if you have enabled multiple instances on your runner, but want all jobs for this Module to go to a specific instance.</summary>
    public string? RunnerInstanceName { get; set; }

    /// <summary>If this is set to true, any Extra Files that have been set on Namespace level will not be used on this specific Module.</summary>
    public bool IgnoreNamespaceExtraFiles { get; set; }
    /// <summary>If this is set to true, any Flags (Terraform Flags, Terraform Array Flags, Pulumi Flags, Pulumi Array Flags) that have been set on Namespace level will not be used on this specific Module.</summary>
    public bool IgnoreNamespaceFlags { get; set; }
    /// <summary>If this is set to true, any Hooks set on Namespace level will not be used on this specific Module.</summary>
    public bool IgnoreNamespaceHooks { get; set; }

    /// <summary>Setting will remove all .terraform* files and folders (state files, locks, downloaded providers, downloaded modules etc.) and perform a clean init every time the Module is executed. Setting this will override any default value set on the Module's parent Namespace.</summary>
    public bool? CleanInitEnabled { get; set; }

    /// <summary>Determines which binary will be used during deployment. Must be one of 'OpenTofu', 'Terraform' or 'Pulumi'. Setting this to 'OpenTofu' will use `tofu`. Setting it to 'Terraform' will use `terraform`. Setting this to 'Pulumi' will use `pulumi`. Setting this will override any default value set on the Module's parent Namespace.</summary>
    public StateManagementEngine? Engine { get; set; }

    /// <summary>Defaults to 'true'. If 'true', the Module will automatically be applied when its definition changes. A definition change results from fields on the Module itself, on any of its Inputs (Param or Env Var) or Extra Files being altered. So too changes to its Namespace (including Inputs and Extra Files) or Stack. Note however that Namespace and Stack changes are not notified by default. This behaviour can be changed in `snapcd_namespace` and `snapcd_stack` resource definitions.</summary>
    public bool TriggerOnDefinitionChanged { get; set; }

    /// <summary>Defaults to 'true'. If 'true', the Module will automatically be applied when any Outputs from other Modules that it references as Inputs (Param or Env Var) have changed.</summary>
    public bool TriggerOnUpstreamOutputChanged { get; set; }

    /// <summary>Defaults to 'true'. If 'true', the Module will automatically be applied when the source it is referencing has changed. For example, if tracking a Git branch: a new commit would constitute a change.</summary>
    public bool TriggerOnSourceChanged { get; set; }

    /// <summary>Defaults to 'false'. If 'true', the Module will automatically be applied when the 'api/Hooks/SourceChanged' endpoint is called for this Module. Use this if you want to use external tooling to inform Snap CD that a source has been changed. Consider setting `trigger_on_definition_changed` to 'false' when setting `trigger_on_definition_changed_hook` to 'true'</summary>
    public bool TriggerOnSourceChangedNotification { get; set; }

    /// <summary>Setting this to true will periodically trigger an Apply job to check for drift in the deployed infrastructure. Setting this will override any default value set on the Module's parent Namespace.</summary>
    public bool? DriftCheckEnabled { get; set; }
    /// <summary>The number of minutes between drift checks. If not set, the system default (24 hours) is used. Note that irrespective of what is set here, these those will not be fired more regularly than the minimum internal as defined by your subscription tier. Setting this will override any default value set on the Module's parent Namespace.</summary>
    public int? DriftCheckIntervalMinutes { get; set; }
}
