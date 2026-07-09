// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Contracts.Validation;

namespace SnapCd.Runner.Settings;

/// <summary>
/// Identity, organisation and credentials that bind this Runner process to a Runner record on
/// the Server. All four fields are required for the Runner to authenticate and connect.
/// </summary>
public class RunnerSettings
{
    /// <summary>
    /// Identifier of the Organization this Runner belongs to. Must match the Organization the
    /// Runner record below was created in.
    /// </summary>
    [NonEmptyGuid]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Name this Runner reports when it connects, used to distinguish replicas when
    /// allow_multiple_instances is set on the Runner record. Visible in the Dashboard's
    /// Runners page next to the parent record.
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the Runner record on the Server this process binds to.
    /// </summary>
    [NonEmptyGuid]
    public Guid Id { get; set; }

    /// <summary>
    /// Service Principal credentials the Runner authenticates with. The Service Principal
    /// referenced here must be the one bound to the Runner record via
    /// service_principal_id.
    /// </summary>
    public Credentials Credentials { get; set; } = new() { ClientId = string.Empty, ClientSecret = string.Empty };
}

/// <summary>
/// OAuth2 client_credentials grant credentials for a Service Principal.
/// </summary>
public class Credentials
{
    /// <summary>
    /// The Service Principal's client identifier, prefixed with the Organization ID at the
    /// token endpoint (the prefix is added automatically by the Runner; supply only the raw
    /// client ID here).
    /// </summary>
    [Required]
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// The Service Principal's client secret. Sensitive — production deployments should source
    /// this via the External Settings provider rather than committing it to appsettings.json.
    /// </summary>
    [Required]
    public string ClientSecret { get; set; } = null!;
}