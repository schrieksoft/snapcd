// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings.DataSeeder.ToSeed;

/// <summary>
/// One User to materialise via the debug data seeder. Only honoured in Development; never used
/// in production-flow seeds.
/// </summary>
public class UserToSeed
{
    /// <summary>Optional fixed ID. When null, a fresh GUID is generated.</summary>
    public Guid? Id { get; set; }

    /// <summary>Email address — doubles as the sign-in identifier.</summary>
    public required string Email { get; set; }

    /// <summary>
    /// Initial password. Sensitive — even in Development, prefer setting via the External Settings
    /// provider so the value doesn't end up in checked-in appsettings files.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// When true, the seeded User is granted system-administrator role on creation. Useful for
    /// developer workstations that need an admin without going through the Dashboard's first-User
    /// promotion flow.
    /// </summary>
    public bool IsSystemAdministrator { get; set; }
}
