// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Runner.Services.PolicyEvaluation;
using Xunit;

namespace SnapCd.Runner.Tests;

public class ConftestResultParserTests
{
    [Fact]
    public void Parses_Failures_Warnings_And_Attribution()
    {
        var json = """
        [
            {
                "filename": "plan.json",
                "namespace": "snapcd.policies.networking",
                "successes": 0,
                "failures": [
                    {
                        "msg": "aws_security_group.web opens port 22 to the world",
                        "metadata": {
                            "query": "data.snapcd.policies.networking.deny",
                            "details": {"port": 22, "severity": "high"}
                        }
                    }
                ]
            },
            {
                "filename": "plan.json",
                "namespace": "snapcd.policies.cost",
                "successes": 1,
                "warnings": [
                    { "msg": "aws_instance.web uses t3.large", "metadata": { "query": "data.snapcd.policies.cost.warn" } }
                ]
            },
            { "filename": "plan.json", "namespace": "lib.terraform", "successes": 0 }
        ]
        """;

        var results = ConftestResultParser.Parse(json);

        Assert.Equal(3, results.Count);
        var networking = results.Single(r => r.Namespace == "snapcd.policies.networking");
        var failure = Assert.Single(networking.Failures);
        Assert.Equal("aws_security_group.web opens port 22 to the world", failure.Message);
        Assert.Equal("data.snapcd.policies.networking.deny", failure.Query);
        Assert.Contains("\"severity\"", failure.DetailsJson);

        var cost = results.Single(r => r.Namespace == "snapcd.policies.cost");
        Assert.Equal(1, cost.Successes);
        Assert.Single(cost.Warnings);
        Assert.Empty(cost.Failures);
        Assert.Null(cost.Warnings[0].DetailsJson);

        Assert.False(ConftestResultParser.DefinesNoPolicyRules(results));
    }

    [Fact]
    public void Zero_Rules_Detected_When_All_Counts_Are_Zero()
    {
        // The shape conftest produces for a syntactically valid policy whose rule names are typo'd
        // (e.g. "denny"): exit 0, one namespace entry, all counts zero.
        var json = """[ { "filename": "plan.json", "namespace": "snapcd", "successes": 0 } ]""";

        var results = ConftestResultParser.Parse(json);

        Assert.True(ConftestResultParser.DefinesNoPolicyRules(results));
    }

    [Fact]
    public void Passing_Rule_Counts_As_Success_Not_Zero_Rules()
    {
        var json = """[ { "filename": "plan.json", "namespace": "snapcd", "successes": 1 } ]""";

        var results = ConftestResultParser.Parse(json);

        Assert.False(ConftestResultParser.DefinesNoPolicyRules(results));
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("")]
    [InlineData("""{"filename": "plan.json"}""")]
    public void Malformed_Output_Throws(string output)
    {
        Assert.Throws<ConftestParseException>(() => ConftestResultParser.Parse(output));
    }
}
