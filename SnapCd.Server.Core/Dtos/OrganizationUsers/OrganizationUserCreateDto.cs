// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Dtos.OrganizationUsers;

public class OrganizationUserCreateDto
{
    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public bool IsDeactivated { get; set; }

    public string? InvitationToken { get; set; }

    public DateTime? InvitationSentDateTime { get; set; }

    public DateTime? InvitationExpirationDateTime { get; set; }

    public bool InvitationCompleted { get; set; }

    public DateTime? InvitationCompletedDateTime { get; set; }
}
