// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings.DataSeeder;

/// <summary>
/// Production-time data seeding configuration. Drives what the Server inserts on first start so
/// that a fresh deployment comes up with a working Organization, an admin User, a default Runner
/// + Service Principal, and (on Enterprise) a default Agent + Mission set. Operators rarely
/// change these from the defaults; the typical override is bumping the admin User's password.
/// </summary>
public class ProductionDataSeederSettings
{
    /// <summary>The single nested block that controls preseeding behaviour and identities.</summary>
    public PreseededSettings Preseeded { get; set; } = new();
}

/// <summary>
/// Identities (IDs, names, credentials) of the entities the Server preseeds on first start. The
/// fixed GUIDs (<see cref="DefaultId"/> / <see cref="DefaultAgentId"/>) make it safe to reference
/// the preseeded rows from Terraform without first calling the Web API for the IDs.
/// </summary>
public class PreseededSettings
{
    /// <summary>
    /// Shared default ID used for the preseeded Organization, User, Runner, Runner SP, Stack and
    /// sample Secret. Fixed (not configurable) so external IaC can reference these IDs by literal.
    /// </summary>
    public static readonly Guid DefaultId = new("10000000-0000-0000-0000-000000000000");

    // Agent + its ServicePrincipal use a distinct id because the agent's SP and the runner's SP
    // live in the same ServicePrincipals table and cannot share the DefaultId primary key.
    /// <summary>
    /// Default ID used for the preseeded Agent and its Service Principal. Distinct from
    /// <see cref="DefaultId"/> because the Agent SP and Runner SP share the ServicePrincipals
    /// table and can't both use the same primary key.
    /// </summary>
    public static readonly Guid DefaultAgentId = new("20000000-0000-0000-0000-000000000000");

    /// <summary>
    /// When true, the preseed runs on first start. Defaults to false so the seeder is opt-in
    /// — typical for Self-Hosted appsettings.json, which enables it for first-time setup and
    /// then leaves it enabled because the seed is idempotent.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Default Organization identity and name.</summary>
    public OrganizationPreseed Organization { get; set; } = new();

    /// <summary>Default admin User identity, email, and initial password.</summary>
    public UserPreseed User { get; set; } = new();

    /// <summary>Default Runner identity and bound Service Principal credentials.</summary>
    public RunnerPreseed Runner { get; set; } = new();

    /// <summary>Default Stack identity and sample Secret seeded under it.</summary>
    public StackPreseed Stack { get; set; } = new();

    /// <summary>
    /// Default Agent identity, bound Service Principal credentials, and the set of Mission types
    /// to seed as organization-wide Missions targeting that Agent.
    /// </summary>
    public AgentPreseed Agent { get; set; } = new();
}

/// <summary>
/// Identity for the preseeded default Organization.
/// </summary>
public class OrganizationPreseed
{
    /// <summary>
    /// Organization ID. Defaults to <see cref="PreseededSettings.DefaultId"/> so external IaC can
    /// reference it by literal.
    /// </summary>
    public Guid? Id { get; set; } = PreseededSettings.DefaultId;

    /// <summary>Organization display name. Defaults to "default".</summary>
    public string Name { get; set; } = "default";
}

/// <summary>
/// Identity and credentials for the preseeded admin User.
/// </summary>
public class UserPreseed
{
    /// <summary>User ID. Defaults to <see cref="PreseededSettings.DefaultId"/>.</summary>
    public Guid? Id { get; set; } = PreseededSettings.DefaultId;

    /// <summary>
    /// Admin email — doubles as the sign-in identifier. Defaults to <c>admin@preseeded.io</c>;
    /// operators typically change this to a real address so password resets and notifications
    /// work end-to-end.
    /// </summary>
    public string Email { get; set; } = "admin@preseeded.io";

    /// <summary>
    /// Initial admin password. Defaults to a publicly-known placeholder; operators MUST change
    /// this before first start of any deployment exposed to traffic. Sensitive — source via the
    /// External Settings provider in production.
    /// </summary>
    public string Password { get; set; } = "Admin#123";

    /// <summary>
    /// When true (default), the preseeded User is granted system-administrator role. Required
    /// for the Dashboard's first-login flow to succeed.
    /// </summary>
    public bool IsSystemAdministrator { get; set; } = true;
}

/// <summary>
/// Identity and bound Service Principal credentials for the preseeded default Runner.
/// </summary>
public class RunnerPreseed
{
    /// <summary>Runner ID. Defaults to <see cref="PreseededSettings.DefaultId"/>.</summary>
    public Guid? Id { get; set; } = PreseededSettings.DefaultId;

    /// <summary>Runner display name. Defaults to "default".</summary>
    public string Name { get; set; } = "default";

    /// <summary>Bound Service Principal ID. Defaults to <see cref="PreseededSettings.DefaultId"/>.</summary>
    public Guid? ServicePrincipalId { get; set; } = PreseededSettings.DefaultId;

    /// <summary>Service Principal client ID the Runner authenticates with. Defaults to "default".</summary>
    public string ServicePrincipalClientId { get; set; } = "default";

    /// <summary>
    /// Service Principal client secret. Defaults to "default" — replace before exposing the
    /// Server to traffic. Sensitive — source via the External Settings provider in production.
    /// </summary>
    public string ServicePrincipalClientSecret { get; set; } = "default";
}

/// <summary>
/// Identity, bound Service Principal credentials, and seeded Missions for the preseeded default
/// Agent. Only honoured on Enterprise tier; otherwise the Agent block is silently ignored.
/// </summary>
public class AgentPreseed
{
    /// <summary>Agent ID. Defaults to <see cref="PreseededSettings.DefaultAgentId"/>.</summary>
    public Guid? Id { get; set; } = PreseededSettings.DefaultAgentId;

    /// <summary>Agent display name. Defaults to "default".</summary>
    public string Name { get; set; } = "default";

    /// <summary>Bound Service Principal ID. Defaults to <see cref="PreseededSettings.DefaultAgentId"/>.</summary>
    public Guid? ServicePrincipalId { get; set; } = PreseededSettings.DefaultAgentId;

    /// <summary>Service Principal client ID the Agent authenticates with. Defaults to "defaultAgent".</summary>
    public string ServicePrincipalClientId { get; set; } = "defaultAgent";

    /// <summary>
    /// Service Principal client secret. Defaults to "defaultAgent" — replace before exposing.
    /// Sensitive — source via the External Settings provider in production.
    /// </summary>
    public string ServicePrincipalClientSecret { get; set; } = "defaultAgent";

    /// <summary>MissionType values seeded as organization-wide missions for the default Agent.</summary>
    public List<string> Missions { get; set; } = new() { "AutoDiagnose", "ApprovalRecommend", "SummarizeJob" };
}

/// <summary>
/// Identity for the preseeded default Stack, plus a sample Secret seeded under it (handy for
/// first-time-user demos that show the Inputs-from-Secret flow without setting one up manually).
/// </summary>
public class StackPreseed
{
    /// <summary>Stack ID. Defaults to <see cref="PreseededSettings.DefaultId"/>.</summary>
    public Guid? Id { get; set; } = PreseededSettings.DefaultId;

    /// <summary>Stack display name. Defaults to "default".</summary>
    public string Name { get; set; } = "default";

    /// <summary>Sample Stack Secret ID. Defaults to <see cref="PreseededSettings.DefaultId"/>.</summary>
    public Guid? SampleSecretId { get; set; } = PreseededSettings.DefaultId;

    /// <summary>Sample Stack Secret name. Defaults to "sample".</summary>
    public string SampleSecretName { get; set; } = "sample";

    /// <summary>
    /// Sample Stack Secret value. Defaults to the placeholder "sample" — replace for any
    /// deployment that will see real traffic. Sensitive — source via the External Settings
    /// provider in production.
    /// </summary>
    public string SampleSecretValue { get; set; } = "sample";
}

/// <summary>
/// Legacy User-seed shape kept for backwards compatibility with older preseed schemas. Use
/// <see cref="UserPreseed"/> for new deployments.
/// </summary>
public class UserToPreseed
{
    /// <summary>Optional fixed ID. When null, a fresh GUID is generated.</summary>
    public Guid? Id { get; set; }

    /// <summary>User name.</summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Initial password. Sensitive — source via the External Settings provider in production.
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>Organization the User belongs to.</summary>
    public Guid OrganizationId { get; set; }
}
