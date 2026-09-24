// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Runner.Services;
using Xunit;

namespace SnapCd.Runner.Tests;

/// <summary>
/// A filtered list reports only the addresses it was asked about. A Module's full state is not
/// Snap CD's to hold, log or display, so it never leaves the runner.
/// </summary>
public class StateListFilteredTests
{
    [Fact]
    public void It_Splits_The_Filter_Into_Present_And_Absent()
    {
        var (present, absent) = BaseEngine.Compare(
            ["random_pet.dns_zone", "random_uuid.dns_zone_id", "random_id.backup_suffix"],
            new HashSet<string> { "random_pet.dns_zone", "random_uuid.dns_zone_id", "random_id.other" });

        Assert.Equal(["random_pet.dns_zone", "random_uuid.dns_zone_id"], present);
        Assert.Equal(["random_id.backup_suffix"], absent);
    }

    /// <summary>The verdict covers the filter, not the state: what else is in there is not reported.</summary>
    [Fact]
    public void It_Reports_Nothing_About_Addresses_It_Was_Not_Asked_About()
    {
        var (present, absent) = BaseEngine.Compare(
            ["a.one"], new HashSet<string> { "a.one", "a.two", "a.three", "a.four" });

        Assert.Equal(["a.one"], present);
        Assert.Empty(absent);
    }

    /// <summary>A Module that has never been applied has no state, which is not a failure.</summary>
    [Fact]
    public void An_Empty_State_Reports_Everything_Absent()
    {
        var (present, absent) = BaseEngine.Compare(["a.one", "a.two"], new HashSet<string>());

        Assert.Empty(present);
        Assert.Equal(["a.one", "a.two"], absent);
    }

    [Fact]
    public void An_Empty_Filter_Asks_Nothing()
    {
        var (present, absent) = BaseEngine.Compare([], new HashSet<string> { "a.one" });

        Assert.Empty(present);
        Assert.Empty(absent);
    }
}
