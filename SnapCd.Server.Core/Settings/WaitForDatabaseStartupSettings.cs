// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// How long the Server waits for its database server to accept connections before giving up at startup.
/// </summary>
public class WaitForDatabaseStartupSettings
{
    /// <summary>
    /// Whether to wait at all. Turned off, the Server attempts its first database operation
    /// immediately and fails if the database is not there.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Total time to keep retrying. Set to zero to fail on the first attempt.
    /// </summary>
    public TimeSpan WaitTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Delay between attempts.
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Per-attempt connection timeout, which overrides whatever the connection string asks for so a
    /// long one cannot stall the loop.
    /// </summary>
    public int ConnectTimeoutSeconds { get; set; } = 5;
}
