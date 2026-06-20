// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Integrations;

/// <summary>One supply assignment of an integration to a scope. Shared across all three scope tables —
/// <see cref="Scope"/> says which, <see cref="ScopeId"/> is the stack/namespace/module id.</summary>
public class IntegrationSupplyDto : IDto
{
    public Guid Id { get; set; }
    public IntegrationSupplyScope Scope { get; set; }
    public Guid ScopeId { get; set; }
}

/// <summary>Create payload: assign the integration to one scope target.</summary>
public class IntegrationSupplyCreateDto
{
    public IntegrationSupplyScope Scope { get; set; }
    public Guid ScopeId { get; set; }
}
