// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
namespace SnapCd.Server.Core.Services.Interfaces.Edition;

/// <summary>
/// Reports whether an organization is currently activated for billable use. A downstream host decides
/// what that means; the default implementation in Core treats every organization as activated.
/// </summary>
public interface IOrganizationActivationService
{
    Task<bool> IsActivatedAsync(Guid organizationId, CancellationToken ct = default);
}
