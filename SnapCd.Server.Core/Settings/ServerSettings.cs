// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Settings describing the Server's own runtime identity — its public URL and the unique
/// per-process instance ID used to disambiguate replicas on the message bus.
/// </summary>
public class ServerSettings
{
    /// <summary>
    /// Public base URL of the Server (scheme + host + optional port), used for token issuance
    /// (set as the OpenIddict issuer claim), email link generation, and self-referential redirects.
    /// Required. Must be reachable from Runners, Agents and the Dashboard.
    /// </summary>
    [Required]
    public string Host { get; set; } = null!;

    /// <summary>
    /// Unique identifier for this Server process. Generated automatically at startup (a fresh GUID
    /// per process) by the predefined configuration provider — operators should not set this in
    /// appsettings.json. Used to distinguish per-process MassTransit consumer endpoints when
    /// multiple Server replicas share one bus.
    /// </summary>
    public Guid InstanceId { get; set; }
}