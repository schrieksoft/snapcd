// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;

namespace SnapCd.Contracts;


public enum InputType
{
    String,
    NotString,
}

public enum InputKind
{
    Param,
    EnvVar
}

public enum DesiredStateHeadline
{
    Applied,
    Destroyed
}

public enum SourceType
{
    Git,
    Registry,
    S3,
    Http,
    Https,
    Gcs,
    Mercurial,
    Unknown
}

public enum SourceRevisionType
{
    Default,
    SemanticVersionRange
}

public enum WaitForApplyDependencies
{
    Always,
    Never,
    OnFirstApply
}

public enum WaitForDestroyDependencies
{
    Always,
    Never
}

public enum PrincipalDiscriminator
{
    User,
    ServicePrincipal
}

public enum DefinitionInputType
{
    StackId,
    StackName,
    NamespaceId,
    NamespaceName,
    ModuleId,
    ModuleName,
    SourceRevision,
    SourceUrl,
    SourceSubdirectory
}

public enum StateManagementEngine
{
    OpenTofu,
    Terraform,
    Pulumi
}

public enum PulumiCommandTask
{
    Init,
    Plan,
    Apply,
    Destroy,
    Output
}

public enum PulumiFlag
{
    // Init — Login
    CloudUrl,
    LoginLocal,
    LoginCloud,
    DefaultOrg,
    Insecure,
    OidcExpiration,
    OidcOrg,
    OidcTeam,
    OidcToken,
    OidcUser,

    // Init — Stack Select
    StackName,
    SecretsProvider,
    CreateStack,

    // Plan/Apply/Destroy shared
    ConfigFile,
    Debug,
    Diff,
    ExpectNoChanges,
    Json,
    Message,
    Parallel,
    Refresh,
    RunProgram,
    ShowConfig,
    ShowFullOutput,
    ShowReads,
    ShowReplacementSteps,
    ShowSames,
    ShowSecrets,
    SuppressOutputs,
    SuppressProgress,
    TargetDependents,
    ExcludeDependents,
    Neo,

    // Plan only
    ImportFile,

    // Apply only
    ContinueOnError,
    SkipPreview,
    Strict,

    // Destroy only
    ExcludeProtected,
    Remove,

    // Output
    Shell,

    // Global
    Color,
    Verbose,
    Emoji
}

public enum PulumiArrayFlag
{
    PolicyPack,
    PolicyPackConfig,
    AttachDebugger,
    Target,
    Replace,
    Exclude,
    TargetReplace,
    Config
}

public enum TerraformCommandTask
{
    Init,
    Plan,
    Apply,
    Destroy,
    Output
}

public enum TerraformFlag
{
    // Init
    ForceCopy,
    FromModule,
    GetPlugins,
    LockTimeout,
    Lockfile,
    MigrateState,
    Plugin,
    Reconfigure,
    TestDirectory,
    Upgrade,

    // Plan/Apply/Destroy shared
    CompactWarnings,
    Concurrency,
    Lock,
    NoColor,
    Parallelism,
    Refresh,
    RefreshOnly,

    // Plan only
    DetailedExitcode,
    GenerateConfigOut,

    // Apply only
    CreateBeforeDestroy,

    // Output only
    Raw,
}

public enum TerraformArrayFlag
{
    Target,
    Replace,
    Exclude,
    Var,
    BackendConfig,
}

public enum HookTask
{
    Init,
    Plan,
    PlanDestroy,
    Apply,
    Destroy,
    Output,
    Validate
}

public enum HookPhase
{
    Before,
    After
}

public enum StackTriggerBehaviour
{
    DoNotTrigger,
    TriggerAllImmediately
}

public enum OrganizationRole
{
    Owner,
    Contributor,
    Reader,
    StackCreator,
    IdentityAccessManager,
    JobManager,
    SourceChangeNotifier,
    SubscriptionManager,
    StackContributor,
    StackReader,
    RunnerCreator,
    RunnerContributor,
    RunnerReader,
    AgentCreator,
    AgentContributor,
    AgentReader,
    SourceRefresherPreselectionCreator,
    SourceRefresherPreselectionContributor,
    SourceRefresherPreselectionReader,
    IntegrationCreator,
    IntegrationContributor,
    IntegrationReader,
}

public enum StackRole
{
    Owner,
    Contributor,
    Reader,
    NamespaceCreator,
    IdentityAccessManager,
    JobManager,
    SourceChangeNotifier
}

public enum NamespaceRole
{
    Owner,
    Contributor,
    Reader,
    ModuleCreator,
    IdentityAccessManager,
    JobManager,
    SourceChangeNotifier
}

public enum ModuleRole
{
    Owner,
    Contributor,
    Reader,
    IdentityAccessManager,
    JobManager,
    SourceChangeNotifier
}

public enum RunnerRole
{
    Owner,
    Contributor,
    Reader,
    IdentityAccessManager
}

public enum AgentRole
{
    Owner,
    Contributor,
    Reader,
    IdentityAccessManager
}

public enum IntegrationRole
{
    Owner,
    Contributor,
    Reader,
    IdentityAccessManager
}

public enum MissionType
{
    AutoDiagnose,
    ApprovalRecommend,
    SummarizeJob,
    AutoFix
}

/// <summary>
/// What kind of thing AutoDiagnose / AutoFix concluded about an unsuccessful run. Wire-stable string
/// values (enum is stored via HasConversion&lt;string&gt; and round-tripped from the sidecar's
/// <c>report_diagnosis_category</c> MCP tool); add values at the end, never rename. The agent picks
/// exactly one before terminating; <see cref="Unknown"/> is the fallback when nothing else fits.
/// </summary>
public enum DiagnosisCategory
{
    Unknown,
    ProviderTransient,
    ProviderAuth,
    ModuleCode,
    Configuration,
    StateDrift,
    Dependency,
    Quota,
    DeclinedApproval,
    ExternalMutation
}

public enum MissionStatus
{
    Pending,
    /// <summary>Mission-level only: no live agent matched at dispatch time; parked until an agent
    /// covering the mission's scope reconnects (mirrors the runner's <c>XWaitingForRunner</c>).</summary>
    WaitingForAgent,
    /// <summary>Mission-level only: the referenced Agent has a live connection but is not assigned
    /// to the target scope (no covering <c>Agent{Stack,Namespace,Module}Assignment</c> row and
    /// <c>Agent.IsSuppliedToAllModules</c> is false). Parked until the Agent owner adds an assignment;
    /// a status distinct from <see cref="WaitingForAgent"/> so the operator can see "this mission is
    /// blocked on configuration, not on the agent being offline". The wake path that picks this up is
    /// <c>AgentSupplyCreatedMissionWakeConsumer</c> — it consumes the assignment-created events and
    /// the <c>Agent</c> update event for <c>IsSuppliedToAllModules</c> flips, and re-attempts dispatch.</summary>
    BlockedAgentNotAssigned,
    Running,
    AwaitingReconnect,
    Succeeded,
    Failed,
    Cancelled,
    TimedOut
}


public enum GroupMemberDiscriminator
{
    Base,
    User,
    ServicePrincipal,
    Group
}

public enum NamespaceTriggerBehaviour
{
    DoNotTrigger,
    TriggerAllImmediately
}

public enum NamespaceInputUsageMode
{
    UseIfSelected,
    UseByDefault
}

public enum RoleAssignmentPrincipalDiscriminator
{
    Base,
    User,
    ServicePrincipal,
    Group
}

public enum SecretDiscriminator
{
    StackSecret,
    NamespaceSecret,
    ModuleSecret,
    SecretOutput
}

public enum CancellationType
{
    AfterCurrent,
    ImmediateGraceful,
    ImmediateKill
}

public enum NamespaceInputSource
{
    Literal,
    Definition,
    NamespaceSecret
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModuleInputSource
{
    Literal,
    ModuleOutput,
    NamespaceParam,
    Definition,
    ModuleOutputSet,
    ModuleSecret
}

public enum PlanAction
{
    Noop,
    Create,
    Update,
    Delete,
    Replace
}

public enum LogSource
{
    // 0 is deliberate: pre-existing persisted log entries have no "source" field,
    // so default deserialization maps them to Runner — which matches the historical truth.
    Runner = 0,
    Server = 1
}
