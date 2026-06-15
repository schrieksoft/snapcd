// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Serilog.Events;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Misc;

namespace SnapCd.Server.Core.UI.Dashboard.Pages.Organization;

/// <summary>
/// Canned <see cref="ModuleJob"/> log content used by the dev harness, in the production shape — a
/// <see cref="List{LogEntryDto}"/> serialised to <c>ModuleJob.Logs</c>. Each scenario is a sequence of
/// per-phase snippets (one <see cref="PhaseSnippet"/> per <c>TaskName</c>) so the synthesised job
/// reads like a real runner trace and the agent's <c>TaskName</c>-based filtering works the way the
/// skills expect.
/// </summary>
public static class MissionTestBenchScenarios
{
    /// <summary>Title is shown in the dropdown; Header is the one-line summary surfaced to the agent
    /// (apply/error header). ErrorText is only set on Failed scenarios — used for
    /// <c>ModuleJob.ServerSideError</c>.</summary>
    public sealed record Scenario(string Title, string Header, string? ErrorText, IReadOnlyList<PhaseSnippet> Phases);

    /// <summary>One runner phase: a <c>TaskName</c> and the ordered <c>Message</c>s for that phase.</summary>
    public sealed record PhaseSnippet(string TaskName, IReadOnlyList<string> Messages);

    // ---- Common prelude. Real runner traces include hooks + env-var loading; we keep the shape but
    // trim the boilerplate. The plan phase is per-scenario; ApplyFromPlan only where relevant.

    private const string Sha = "a460ff686a0e16d8de06b20327844fd7bb124239";

    private static readonly PhaseSnippet GetDefinitiveRevision = new("GetDefinitiveRevision", new[]
    {
        "Now cloning repo",
        $"Found remote sha: {Sha}",
        "Completed GetDefinitiveRevision",
    });

    private static readonly PhaseSnippet GetModule = new("GetModule", new[]
    {
        "Now cloning repo",
        $"Found local sha: {Sha}",
        $"Found remote sha: {Sha}",
        "Local SHA equals latest remote SHA, continuing with current local revision.",
        "Completed GetModule",
    });

    private static readonly PhaseSnippet Init = new("Init", new[]
    {
        "Now initializing",
        "Initializing the backend...",
        "Initializing provider plugins...",
        "- Reusing previous version of hashicorp/aws from the dependency lock file",
        "- Using previously-installed hashicorp/aws v5.31.0",
        "OpenTofu has been successfully initialized!",
        "Completed Init",
    });

    private static readonly PhaseSnippet Validate = new("Validate", new[]
    {
        "Now validating",
        "Success! The configuration is valid.",
        "Completed Validate",
    });

    private static readonly PhaseSnippet Variables = new("Variables", new[]
    {
        "Now discovering variables",
        "Completed Variables",
    });

    /// <summary>Prelude common to every scenario: clone → resolve sha → init → validate → variables.</summary>
    private static readonly PhaseSnippet[] Prelude = { GetDefinitiveRevision, GetModule, Init, Validate, Variables };

    // ---- Plan snippets reused across scenarios. SnapCd emits its own "Plan summary:" entry — the
    // skill prefers that over parsing terraform's table, so we always include it.

    private static PhaseSnippet PlanPhase(string planBody, string countsSummary, string outputsSummary) => new("Plan", new[]
    {
        "Now planning",
        planBody,
        $"Plan summary:\n{countsSummary}",
        $"Plan summary:\n{outputsSummary}",
        "Completed Plan",
    });

    // ---- Scenarios ----

    public static readonly Scenario[] Failed = new[]
    {
        new Scenario(
            Title: "Apply: S3 bucket already exists",
            Header: "Apply failed with exception",
            ErrorText: "BucketAlreadyOwnedByYou: Your previous request to create the named bucket succeeded and you already own it.",
            Phases: Prelude.Concat(new[]
            {
                PlanPhase(
                    "OpenTofu will perform the following actions:\n\n  # aws_s3_bucket.logs will be created\n  + resource \"aws_s3_bucket\" \"logs\" {\n      + bucket = \"acme-prod-logs\"\n    }\n\nPlan: 1 to add, 0 to change, 0 to destroy.",
                    "- Unchanged: 0\n- Create:    1\n- Modify:    0\n- Destroy:   0\n- Recreate:  0\n- Count Before Apply:  0\n- Count After Apply:   1",
                    "- Unchanged Outputs: 0\n- Create Outputs:    0\n- Modify Outputs:    0\n- Destroyed Outputs: 0\n- Recreate Outputs:  0"),
                new PhaseSnippet("ApplyFromPlan", new[]
                {
                    "Now applying from plan",
                    "aws_s3_bucket.logs: Creating...",
                    "Apply failed with exception: Process in /home/.../acme-prod-logs failed.\n│ Error: creating S3 Bucket (acme-prod-logs): BucketAlreadyOwnedByYou: Your previous request to create the named bucket succeeded and you already own it.\n│\n│   with aws_s3_bucket.logs,\n│   on s3.tf line 12, in resource \"aws_s3_bucket\" \"logs\":\n│   12: resource \"aws_s3_bucket\" \"logs\" {",
                }),
            }).ToArray()),

        new Scenario(
            Title: "Plan: missing required variable",
            Header: "Error: No value for required variable",
            ErrorText: "The root module input variable \"environment\" is not set, and has no default value.",
            Phases: Prelude.Concat(new[]
            {
                new PhaseSnippet("Plan", new[]
                {
                    "Now planning",
                    "│ Error: No value for required variable\n│\n│   on variables.tf line 1:\n│    1: variable \"environment\" {\n│\n│ The root module input variable \"environment\" is not set, and has no default value. Use a -var or -var-file command line argument to provide a value for this variable.",
                    "Plan failed with exception: missing required variable \"environment\".",
                }),
            }).ToArray()),

        new Scenario(
            Title: "Plan: IAM permission denied",
            Header: "Error: reading IAM Role",
            ErrorText: "AccessDenied: not authorized to perform: iam:GetRole.",
            Phases: Prelude.Concat(new[]
            {
                new PhaseSnippet("Plan", new[]
                {
                    "Now planning",
                    "data.aws_iam_role.deploy: Reading...",
                    "│ Error: reading IAM Role (acme-deploy)\n│\n│   with data.aws_iam_role.deploy,\n│   on iam.tf line 4, in data \"aws_iam_role\" \"deploy\":\n│    4: data \"aws_iam_role\" \"deploy\" {\n│\n│ AccessDenied: User: arn:aws:sts::123456789012:assumed-role/snapcd-runner/i-0a1b2c3d is not authorized to perform: iam:GetRole on resource: role acme-deploy because no identity-based policy allows the iam:GetRole action",
                    "Plan failed with exception: provider data source read failed.",
                }),
            }).ToArray()),

        new Scenario(
            Title: "Plan: resource not found during refresh",
            Header: "Error: reading EC2 Instance",
            ErrorText: "InvalidInstanceID.NotFound: The instance ID 'i-0deadbeef' does not exist.",
            Phases: Prelude.Concat(new[]
            {
                new PhaseSnippet("Plan", new[]
                {
                    "Now planning",
                    "aws_instance.app: Refreshing state... [id=i-0deadbeef]",
                    "│ Error: reading EC2 Instance (i-0deadbeef): InvalidInstanceID.NotFound: The instance ID 'i-0deadbeef' does not exist\n│\n│   with aws_instance.app,\n│   on app.tf line 22, in resource \"aws_instance\" \"app\":\n│   22: resource \"aws_instance\" \"app\" {",
                    "Plan failed with exception: state refresh failed.",
                }),
            }).ToArray()),
    };

    public static readonly Scenario[] Succeeded = new[]
    {
        new Scenario(
            Title: "Apply: created VPC + 3 subnets",
            Header: "Apply complete! Resources: 4 added, 0 changed, 0 destroyed.",
            ErrorText: null,
            Phases: Prelude.Concat(new[]
            {
                PlanPhase(
                    "OpenTofu will perform the following actions:\n\n  + aws_vpc.main\n  + aws_subnet.public[0]\n  + aws_subnet.public[1]\n  + aws_subnet.public[2]\n\nPlan: 4 to add, 0 to change, 0 to destroy.",
                    "- Unchanged: 0\n- Create:    4\n- Modify:    0\n- Destroy:   0\n- Recreate:  0\n- Count Before Apply:  0\n- Count After Apply:   4",
                    "- Unchanged Outputs: 0\n- Create Outputs:    2\n- Modify Outputs:    0\n- Destroyed Outputs: 0\n- Recreate Outputs:  0"),
                new PhaseSnippet("ApplyFromPlan", new[]
                {
                    "Now applying from plan",
                    "aws_vpc.main: Creating...",
                    "aws_vpc.main: Creation complete after 2s [id=vpc-0a1b2c3d4e]",
                    "aws_subnet.public[0]: Creating...",
                    "aws_subnet.public[1]: Creating...",
                    "aws_subnet.public[2]: Creating...",
                    "aws_subnet.public[0]: Creation complete after 1s [id=subnet-aaa]",
                    "aws_subnet.public[1]: Creation complete after 1s [id=subnet-bbb]",
                    "aws_subnet.public[2]: Creation complete after 1s [id=subnet-ccc]",
                    "Apply complete! Resources: 4 added, 0 changed, 0 destroyed.",
                    "Completed ApplyFromPlan",
                }),
            }).ToArray()),

        new Scenario(
            Title: "Apply: updated IAM role policy",
            Header: "Apply complete! Resources: 0 added, 1 changed, 0 destroyed.",
            ErrorText: null,
            Phases: Prelude.Concat(new[]
            {
                PlanPhase(
                    "OpenTofu will perform the following actions:\n\n  ~ aws_iam_role_policy.deploy { ~ policy = (sensitive) }\n\nPlan: 0 to add, 1 to change, 0 to destroy.",
                    "- Unchanged: 0\n- Create:    0\n- Modify:    1\n- Destroy:   0\n- Recreate:  0\n- Count Before Apply:  1\n- Count After Apply:   1",
                    "- Unchanged Outputs: 0\n- Create Outputs:    0\n- Modify Outputs:    0\n- Destroyed Outputs: 0\n- Recreate Outputs:  0"),
                new PhaseSnippet("ApplyFromPlan", new[]
                {
                    "Now applying from plan",
                    "aws_iam_role_policy.deploy: Refreshing state... [id=acme-deploy:DeployPolicy]",
                    "aws_iam_role_policy.deploy: Modifying... [id=acme-deploy:DeployPolicy]",
                    "aws_iam_role_policy.deploy: Modifications complete after 1s [id=acme-deploy:DeployPolicy]",
                    "Apply complete! Resources: 0 added, 1 changed, 0 destroyed.",
                    "Completed ApplyFromPlan",
                }),
            }).ToArray()),

        new Scenario(
            Title: "Destroy: removed staging stack",
            Header: "Destroy complete! Resources: 7 destroyed.",
            ErrorText: null,
            Phases: Prelude.Concat(new[]
            {
                new PhaseSnippet("PlanDestroy", new[]
                {
                    "Now planning destroy",
                    "OpenTofu will perform the following actions:\n\n  - aws_lb_listener.https\n  - aws_lb.app\n  - aws_security_group.app\n  - aws_ecs_service.app\n  - aws_ecs_task_definition.app\n  - aws_iam_role.task\n  - aws_cloudwatch_log_group.app\n\nPlan: 0 to add, 0 to change, 7 to destroy.",
                    "Plan summary:\n- Unchanged: 0\n- Create:    0\n- Modify:    0\n- Destroy:   7\n- Recreate:  0\n- Count Before Apply:  7\n- Count After Apply:   0",
                    "Completed PlanDestroy",
                }),
                new PhaseSnippet("DestroyFromPlan", new[]
                {
                    "Now destroying from plan",
                    "aws_lb_listener.https: Destroying...",
                    "aws_lb.app: Destroying...",
                    "aws_security_group.app: Destroying...",
                    "aws_ecs_service.app: Destroying...",
                    "aws_ecs_task_definition.app: Destroying...",
                    "aws_iam_role.task: Destroying...",
                    "aws_cloudwatch_log_group.app: Destroying...",
                    "Destroy complete! Resources: 7 destroyed.",
                    "Completed DestroyFromPlan",
                }),
            }).ToArray()),
    };

    public static readonly Scenario[] AwaitingApproval = new[]
    {
        new Scenario(
            Title: "Plan: add EC2 instance + security group",
            Header: "Plan: 2 to add, 0 to change, 0 to destroy.",
            ErrorText: null,
            Phases: Prelude.Concat(new[]
            {
                PlanPhase(
                    "OpenTofu will perform the following actions:\n\n  # aws_instance.app will be created\n  + resource \"aws_instance\" \"app\" {\n      + ami           = \"ami-0abcdef1234567890\"\n      + instance_type = \"t3.medium\"\n      + tags          = { \"Name\" = \"acme-app\" }\n    }\n\n  # aws_security_group.app will be created\n  + resource \"aws_security_group\" \"app\" {\n      + name        = \"acme-app-sg\"\n      + description = \"App tier\"\n    }\n\nPlan: 2 to add, 0 to change, 0 to destroy.",
                    "- Unchanged: 0\n- Create:    2\n- Modify:    0\n- Destroy:   0\n- Recreate:  0\n- Count Before Apply:  0\n- Count After Apply:   2",
                    "- Unchanged Outputs: 0\n- Create Outputs:    1\n- Modify Outputs:    0\n- Destroyed Outputs: 0\n- Recreate Outputs:  0"),
            }).ToArray()),

        new Scenario(
            Title: "Plan: replace RDS database (destructive)",
            Header: "Plan: 1 to add, 0 to change, 1 to destroy.",
            ErrorText: null,
            Phases: Prelude.Concat(new[]
            {
                PlanPhase(
                    "OpenTofu will perform the following actions:\n\n  # aws_db_instance.main must be replaced\n-/+ resource \"aws_db_instance\" \"main\" {\n      ~ engine_version = \"13.11\" -> \"15.4\" # forces replacement\n      ~ id             = \"acme-prod-db\" -> (known after apply)\n      ~ arn            = \"arn:aws:rds:…\" -> (known after apply)\n    }\n\nPlan: 1 to add, 0 to change, 1 to destroy.\n\nWARNING: This will REPLACE the production database. Snapshot strongly recommended.",
                    "- Unchanged: 0\n- Create:    1\n- Modify:    0\n- Destroy:   1\n- Recreate:  1\n- Count Before Apply:  1\n- Count After Apply:   1",
                    "- Unchanged Outputs: 2\n- Create Outputs:    0\n- Modify Outputs:    0\n- Destroyed Outputs: 0\n- Recreate Outputs:  0"),
            }).ToArray()),

        new Scenario(
            Title: "Plan: bulk IAM policy update",
            Header: "Plan: 0 to add, 5 to change, 0 to destroy.",
            ErrorText: null,
            Phases: Prelude.Concat(new[]
            {
                PlanPhase(
                    "OpenTofu will perform the following actions:\n\n  ~ aws_iam_role_policy.deploy  { ~ policy = (sensitive) }\n  ~ aws_iam_role_policy.read    { ~ policy = (sensitive) }\n  ~ aws_iam_role_policy.audit   { ~ policy = (sensitive) }\n  ~ aws_iam_role_policy.backup  { ~ policy = (sensitive) }\n  ~ aws_iam_role_policy.metrics { ~ policy = (sensitive) }\n\nPlan: 0 to add, 5 to change, 0 to destroy.",
                    "- Unchanged: 0\n- Create:    0\n- Modify:    5\n- Destroy:   0\n- Recreate:  0\n- Count Before Apply:  5\n- Count After Apply:   5",
                    "- Unchanged Outputs: 0\n- Create Outputs:    0\n- Modify Outputs:    0\n- Destroyed Outputs: 0\n- Recreate Outputs:  0"),
            }).ToArray()),
    };

    public static readonly Scenario[] Declined = new[]
    {
        new Scenario(
            Title: "Declined: replace RDS too risky",
            Header: "Plan: 1 to add, 0 to change, 1 to destroy.",
            ErrorText: null,
            Phases: AwaitingApproval[1].Phases), // same plan as "Plan: replace RDS database (destructive)"

        new Scenario(
            Title: "Declined: bulk IAM policy change",
            Header: "Plan: 0 to add, 5 to change, 0 to destroy.",
            ErrorText: null,
            Phases: AwaitingApproval[2].Phases),
    };

    /// <summary>
    /// AutoFix scenarios run against the seeded <c>mock-module-vpc</c> module (real repo
    /// <c>snapcd-samples/mock-module-vpc</c>, branch <c>autofixtest</c>), so the dispatched AutoFix
    /// mission clones it, fixes the defect, and opens a PR. The error here matches the actual bugs on
    /// that branch (a typo'd <c>creat_duration</c> in main.tf and an undeclared <c>random_uui</c> in
    /// outputs.tf).
    /// </summary>
    public static readonly Scenario[] AutoFix = new[]
    {
        new Scenario(
            Title: "Plan: typo'd argument + undeclared resource (mock-module-vpc)",
            Header: "Error: Unsupported argument",
            ErrorText: "An argument named \"creat_duration\" is not expected here (did you mean \"create_duration\"?), and managed resource \"random_uui\" \"private_subnet_id\" has not been declared.",
            Phases: Prelude.Concat(new[]
            {
                new PhaseSnippet("Plan", new[]
                {
                    "Now planning",
                    "╷\n│ Error: Unsupported argument\n│ \n│   on main.tf line 2, in resource \"time_sleep\" \"wait_10s\":\n│    2:   creat_duration  = \"10s\"\n│ \n│ An argument named \"creat_duration\" is not expected here. Did you mean \"create_duration\"?\n╵\n╷\n│ Error: Reference to undeclared resource\n│ \n│   on outputs.tf line 13, in output \"private_subnet_id\":\n│   13:   value       = random_uui.private_subnet_id.result\n│ \n│ A managed resource \"random_uui\" \"private_subnet_id\" has not been declared in the root module.\n╵",
                    "Plan failed with exception: configuration is invalid.",
                }),
            }).ToArray()),
    };

    /// <summary>Render the scenario as a flat <see cref="List{LogEntryDto}"/> suitable for
    /// <c>JsonSerializer.Serialize</c> into <c>ModuleJob.Logs</c>. Timestamps run forward; entries
    /// within a phase are 100 ms apart, phases 1 s apart — close enough to realistic that
    /// <see cref="LogService.GetLogStrings"/>'s ordering-by-Timestamp works correctly.</summary>
    public static List<LogEntryDto> BuildEntries(
        Scenario scenario,
        Guid jobId,
        Guid moduleId,
        string moduleName,
        DateTimeOffset startTime)
    {
        var entries = new List<LogEntryDto>();
        var batchStamp = startTime;
        var time = startTime;
        foreach (var phase in scenario.Phases)
        {
            foreach (var msg in phase.Messages)
            {
                entries.Add(new LogEntryDto
                {
                    JobId = jobId,
                    Timestamp = time,
                    BatchTimeStamp = batchStamp,
                    StackId = Guid.Empty,
                    NamespaceId = Guid.Empty,
                    ModuleId = moduleId,
                    StackName = "debug",
                    NamespaceName = "debug",
                    ModuleName = moduleName,
                    Level = LogEventLevel.Verbose,
                    Message = msg,
                    TaskName = phase.TaskName,
                    Tags = null,
                    Source = LogSource.Runner,
                });
                time = time.AddMilliseconds(100);
            }
            time = time.AddSeconds(1);
            batchStamp = time;
        }
        return entries;
    }

    /// <summary>Compact preview rendering for the harness UI — `[TaskName] message` per line.</summary>
    public static string Preview(Scenario scenario)
    {
        var lines = new List<string>();
        foreach (var phase in scenario.Phases)
            foreach (var msg in phase.Messages)
                lines.Add($"[{phase.TaskName}] {msg}");
        return string.Join("\n", lines);
    }
}
