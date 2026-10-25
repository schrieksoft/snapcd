// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class Variable : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid VariableSetId { get; set; }
    [MaxLength(255)] public string Name { get; set; } = null!;
    [MaxLength(255)] public string? Type { get; set; }

    /// <summary>
    /// cty JSON encoding of a complex type, in the same form <c>tofu output -json</c> reports an
    /// output's type. Null for a primitive type, which <see cref="Type"/> fully describes.
    /// </summary>
    public string? TypeDefinition { get; set; }

    /// <summary>Declared default, serialised as JSON. Null when the variable is required.</summary>
    public string? Default { get; set; }

    public string? Description { get; set; }
    public bool Sensitive { get; set; } = false;
    public bool Nullable { get; set; } = false;
    public bool FromExtraFile { get; set; } = false;

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public VariableSet VariableSet { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return VariableSetId;
    }
}