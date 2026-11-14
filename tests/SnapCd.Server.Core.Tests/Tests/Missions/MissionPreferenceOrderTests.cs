// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Contracts;
using SnapCd.Server.Core.Services.Nonsecured.Ai.Missions;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Missions;

/// <summary>
/// The order missions are tried in when more than one matches a job. The nearest declaration wins,
/// so a Namespace's mission overrides the ones above it; a live Agent only separates missions that
/// are equally near.
/// </summary>
public class MissionPreferenceOrderTests
{
    private static readonly Guid LiveAgent = Guid.NewGuid();
    private static readonly Guid DeadAgent = Guid.NewGuid();

    private static MissionMatcher.MissionMatch Match(int distance, Guid agentId, Guid? missionId = null) =>
        new(agentId, missionId ?? Guid.NewGuid(), MissionType.SummarizeJob, null, distance);

    private static List<MissionMatcher.MissionMatch> Order(params MissionMatcher.MissionMatch[] matches) =>
        MissionMatcher.InPreferenceOrder(matches, new HashSet<Guid> { LiveAgent });

    [Fact]
    public void The_module_is_tried_before_its_namespace()
    {
        var onModule = Match(0, LiveAgent);
        var onNamespace = Match(1, LiveAgent);

        var ordered = Order(onNamespace, onModule);

        Assert.Equal(onModule.MissionId, ordered[0].MissionId);
    }

    [Fact]
    public void A_nearer_namespace_is_tried_before_one_further_up()
    {
        var onOwnNamespace = Match(1, LiveAgent);
        var onGrandparent = Match(3, LiveAgent);

        var ordered = Order(onGrandparent, onOwnNamespace);

        Assert.Equal(onOwnNamespace.MissionId, ordered[0].MissionId);
    }

    /// <summary>
    /// The decision this rule exists for: an override is honoured even when it cannot run, rather
    /// than silently falling back to what it was meant to replace.
    /// </summary>
    [Fact]
    public void The_nearest_wins_even_when_its_agent_is_down()
    {
        var nearButDead = Match(1, DeadAgent);
        var farButLive = Match(3, LiveAgent);

        var ordered = Order(farButLive, nearButDead);

        Assert.Equal(nearButDead.MissionId, ordered[0].MissionId);
    }

    [Fact]
    public void A_live_agent_separates_two_missions_at_the_same_distance()
    {
        var dead = Match(1, DeadAgent);
        var live = Match(1, LiveAgent);

        var ordered = Order(dead, live);

        Assert.Equal(live.MissionId, ordered[0].MissionId);
    }

    [Fact]
    public void An_organization_mission_is_tried_last()
    {
        var onOrganization = Match(int.MaxValue, LiveAgent);
        var onNamespace = Match(2, LiveAgent);

        var ordered = Order(onOrganization, onNamespace);

        Assert.Equal(onNamespace.MissionId, ordered[0].MissionId);
    }

    [Fact]
    public void Two_otherwise_equal_missions_keep_a_stable_order()
    {
        var first = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var second = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var ordered = Order(Match(1, LiveAgent, second), Match(1, LiveAgent, first));

        Assert.Equal(first, ordered[0].MissionId);
    }
}
