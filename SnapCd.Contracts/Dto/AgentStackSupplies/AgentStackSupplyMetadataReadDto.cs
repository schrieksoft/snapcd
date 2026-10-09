// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.AgentStackSupplies;

/// <summary>Name and ID of a Agent Stack Supply, for a principal who may identify it without reading it.</summary>
public class AgentStackSupplyMetadataReadDto : IDto
{
    /// <summary>Unique ID of the Agent Stack Supply.</summary>
    public Guid Id { get; set; }

    /// <summary>Agent Id of the Agent Stack Supply.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Stack Id of the Agent Stack Supply.</summary>
    public Guid StackId { get; set; }
}
