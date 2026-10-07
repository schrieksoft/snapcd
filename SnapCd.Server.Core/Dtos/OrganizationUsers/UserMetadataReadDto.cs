// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Contracts.Interfaces;
using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.OrganizationUsers;

/// <summary>Id and username of a user in an organization, for callers who may discover them without reading them.</summary>
public class UserMetadataReadDto : IDto
{
    /// <summary>Unique ID of the User.</summary>
    public Guid Id { get; set; }

    /// <summary>Username of the User, which is also their email.</summary>
    public string UserName { get; set; } = null!;
}
