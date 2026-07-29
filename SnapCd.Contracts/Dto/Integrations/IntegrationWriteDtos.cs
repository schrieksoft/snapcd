// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;

namespace SnapCd.Contracts.Dto.Integrations;

/// <summary>
/// Create payload. <see cref="Connection"/> is a type-specific JSON object the server's codec (selected by
/// <see cref="IntegrationType"/>) parses into the typed connection — no polymorphic model binding.
/// </summary>
public class IntegrationCreateDto
{
    /// <summary>Name of the integration.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Integration type (e.g. Slack).</summary>
    public IntegrationType IntegrationType { get; set; }
    /// <summary>Whether the integration is enabled.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Whether the integration is supplied org-wide.</summary>
    public bool IsSuppliedToAllModules { get; set; }
    public JsonElement Connection { get; set; }
}

/// <summary>
/// Update payload. <see cref="Connection"/> may carry masked secret fields unchanged — the codec keeps the
/// stored value for any field still holding the mask sentinel.
/// </summary>
public class IntegrationUpdateDto
{
    /// <summary>Name of the integration.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Whether the integration is enabled.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Whether the integration is supplied org-wide.</summary>
    public bool IsSuppliedToAllModules { get; set; }
    public JsonElement Connection { get; set; }
}
