// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Events.System;

/// <summary>
/// A participant's consent is wanted. The fan-out consumer that drives live manual-job updates
/// turns this into a notification on the receiving Module's page: that page is the inbox.
/// </summary>
public class ConsentRequested
{
    public Guid TransferId { get; set; }
    public Guid ModuleId { get; set; }
    public Guid OrganizationId { get; set; }
}

/// <summary>A participant answered. Carries the answer so a listener need not re-read the row.</summary>
public class ConsentDecided
{
    public Guid TransferId { get; set; }
    public Guid ModuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public bool Granted { get; set; }
    public Guid PrincipalId { get; set; }
}
