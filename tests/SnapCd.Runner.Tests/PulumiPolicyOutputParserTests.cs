// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Runner.Services.PolicyEvaluation;
using Xunit;

namespace SnapCd.Runner.Tests;

public class PulumiPolicyOutputParserTests
{
    // Captured from a live `pulumi preview --policy-pack` run.
    private const string MandatoryAndAdvisoryOutput = """
        Policies:
            ❌ snapcd-inline-demo@v0.0.1 (local: ../inline-pack)
                - [mandatory]  pet-prefix-required  (random:index/randomPet:RandomPet: web-server)
                  All RandomPets must carry the org prefix.
                  RandomPet must set a prefix (org naming convention)
                - [advisory]  pet-min-length  (random:index/randomPet:RandomPet: web-server)
                  Pet names must be at least two words.
                  RandomPet length must be >= 2 words

        Diagnostics:
          pulumi:pulumi:Stack (policy-demo-dev):
            error: preview failed
        """;

    private const string AdvisoryOnlyOutput = """
        Policies:
            ⚠️ snapcd-inline-demo@v0.0.1 (local: ../inline-pack)
                - [advisory]  pet-min-length  (random:index/randomPet:RandomPet: web-server)
                  Pet names must be at least two words.

        Resources:
            + 2 to create
        """;

    private const string CleanOutput = """
        Resources:
            + 2 to create
        """;

    [Fact]
    public void Mandatory_Violation_Is_HardDenied()
    {
        Assert.Equal(PolicyOutcome.HardDenied, PulumiPolicyOutputParser.Classify(MandatoryAndAdvisoryOutput));
    }

    [Fact]
    public void Advisory_Only_Is_SoftWarned()
    {
        Assert.Equal(PolicyOutcome.SoftWarned, PulumiPolicyOutputParser.Classify(AdvisoryOnlyOutput));
    }

    [Fact]
    public void No_Violations_Is_Passed()
    {
        Assert.Equal(PolicyOutcome.Passed, PulumiPolicyOutputParser.Classify(CleanOutput));
    }
}
