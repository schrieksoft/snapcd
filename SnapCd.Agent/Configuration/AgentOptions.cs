// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Contracts.Validation;

namespace SnapCd.Agent.Configuration;

/// <summary>
/// Orchestrator configuration, bound from the "Agent" section of appsettings.json
/// (overridable by appsettings.{Environment}.json and Agent__* environment variables).
/// </summary>
public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>
    /// Identifier of the Organization this Agent belongs to. Must match the Organization the
    /// Agent record below was created in.
    /// </summary>
    [NonEmptyGuid]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Identifier of the Agent record on the Server this process binds to. The Service Principal
    /// referenced via ClientId / ClientSecret must be the one bound to this Agent record.
    /// </summary>
    [NonEmptyGuid]
    public Guid AgentId { get; set; }

    /// <summary>
    /// Name this Agent reports when it connects, used to distinguish replicas when
    /// allow_multiple_instances is set on the Agent record. Visible in the Dashboard's Agents
    /// page next to the parent record. When blank, defaults server-side to the Agent record name.
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// The Agent's Service Principal client identifier. The Agent prefixes this with the
    /// Organization ID when calling the token endpoint, so supply only the raw client ID here.
    /// </summary>
    [Required]
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// The Agent's Service Principal client secret. Sensitive — production deployments should
    /// source this via the External Settings provider rather than committing it to
    /// appsettings.json.
    /// </summary>
    [Required]
    public string ClientSecret { get; set; } = null!;

    /// <summary>
    /// Registered Sidecar processes the Agent supervises and dispatches Missions to. Each entry
    /// binds a sidecar name (used as the value of a Mission's sidecar_name field) to the base URL
    /// the Agent posts /invoke calls against. An Agent with no Sidecars connects to the Server
    /// but receives no Mission dispatches.
    /// </summary>
    public List<SidecarOptions> Sidecars { get; set; } = new();
}

/// <summary>
/// A single registered Sidecar. The Agent supervises the Sidecar process out-of-band (Docker /
/// Kubernetes / systemd) and communicates with it over localhost HTTP at the URL given here.
/// </summary>
public sealed class SidecarOptions
{
    /// <summary>
    /// Unique name for this Sidecar within the Agent. Matched against the sidecar_name field on
    /// Mission resources to route each Mission to the right Sidecar. When sidecar_name is unset
    /// on a Mission, the Agent's single configured Sidecar handles it.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Base URL of the Sidecar's HTTP endpoint, e.g. http://claude-sidecar:8080. The Agent posts
    /// per-Mission dispatches to {BaseUrl}/invoke and polls {BaseUrl}/health.
    /// </summary>
    public string BaseUrl { get; set; } = null!;
}
