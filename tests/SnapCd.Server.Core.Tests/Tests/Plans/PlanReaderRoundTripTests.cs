// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.IO.Compression;
using System.Text;
using SnapCd.Server.Core.Services.Nonsecured.Plans;

namespace SnapCd.Server.Core.Tests.Tests.Plans;

/// <summary>
/// The runner gzips the plan and the server reads it back. These check the join between them,
/// which no other test covers because the two halves are in different projects.
/// </summary>
public class PlanReaderRoundTripTests
{
    [Fact]
    public void A_gzipped_plan_reads_back()
    {
        const string json =
            "{\"format_version\": \"1.2\", \"terraform_version\": \"1.9.0\", " +
            "\"resource_changes\": [{\"address\": \"test.one\", \"type\": \"test\", " +
            "\"provider_name\": \"test\", \"change\": {\"actions\": [\"update\"], " +
            "\"before\": {\"a\": 1}, \"after\": {\"a\": 2}}}]}";

        var plan = PlanReader.Read(Gzip(json));

        Assert.NotNull(plan);
        Assert.Equal("1.9.0", plan.EngineVersion);
        Assert.Equal(1, plan.Resources.Single().ChangedLeaves);
    }

    [Fact]
    public void No_payload_reads_as_no_plan()
    {
        Assert.Null(PlanReader.Read(null));
        Assert.Null(PlanReader.Read([]));
    }

    /// <summary>A replacement carries two verbs, in the order the engine will perform them.</summary>
    [Fact]
    public void A_replacement_is_recognised()
    {
        const string json =
            "{\"format_version\": \"1.2\", \"terraform_version\": \"1.9.0\", " +
            "\"resource_changes\": [{\"address\": \"test.one\", \"type\": \"test\", " +
            "\"provider_name\": \"test\", \"action_reason\": \"replace_because_cannot_update\", " +
            "\"change\": {\"actions\": [\"delete\", \"create\"], " +
            "\"before\": {\"a\": 1}, \"after\": {\"a\": 2}}}]}";

        var resource = PlanReader.Read(Gzip(json))!.Resources.Single();

        Assert.Equal(ResourceAction.Replace, resource.Action);
        Assert.Equal("replace_because_cannot_update", resource.ActionReason);
    }

    [Fact]
    public void A_resource_that_does_not_change_reports_no_changes()
    {
        const string json =
            "{\"format_version\": \"1.2\", \"terraform_version\": \"1.9.0\", " +
            "\"resource_changes\": [{\"address\": \"test.one\", \"type\": \"test\", " +
            "\"provider_name\": \"test\", \"change\": {\"actions\": [\"no-op\"], " +
            "\"before\": {\"a\": 1}, \"after\": {\"a\": 1}}}]}";

        var plan = PlanReader.Read(Gzip(json))!;

        Assert.Equal(ResourceAction.NoOp, plan.Resources.Single().Action);
        Assert.Equal(0, plan.Resources.Single().ChangedLeaves);
    }

    private static byte[] Gzip(string text)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(Encoding.UTF8.GetBytes(text));

        return buffer.ToArray();
    }
}
