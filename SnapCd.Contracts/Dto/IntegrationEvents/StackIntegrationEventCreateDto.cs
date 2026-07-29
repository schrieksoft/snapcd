// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.IntegrationEvents;

/// <summary>DTO for creating a new StackIntegrationEvent (POST operations).</summary>
public class StackIntegrationEventCreateDto
{
    /// <summary>ID of the target integration.</summary>
    public Guid IntegrationId { get; set; }

    /// <summary>ID of the Stack this event is scoped to.</summary>
    public Guid StackId { get; set; }

    /// <summary>Trigger this subscription fires on.</summary>
    public IntegrationTrigger Trigger { get; set; }

    /// <summary>Optional message template ({{token}} substitution). Omit to use the default for the trigger.</summary>
    public string? Template { get; set; }

    /// <summary>Optional filter expression.</summary>
    public string? Filter { get; set; }

    /// <summary>Whether the subscription is disabled.</summary>
    public bool IsDisabled { get; set; }
}
