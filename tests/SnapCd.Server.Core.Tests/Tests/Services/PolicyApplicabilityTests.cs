// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

public class PolicyApplicabilityTests
{
    private static ResolvedPolicy Policy(PolicyEngine engine, PolicyEvaluateOn evaluateOn)
    {
        return new ResolvedPolicy { Name = "p", Engine = engine, EvaluateOn = evaluateOn, Kind = PolicySourceKind.Inline, Scope = PolicyScope.Module };
    }

    private static ResolvedModule Declared(params ResolvedPolicy[] policies)
    {
        return new ResolvedModule
        {
            ModuleName = "m", NamespaceName = "n", StackName = "s", RunnerName = "r",
            SourceRevision = "main", SourceUrl = "u", SourceSubdirectory = "", Engine = "tofu",
            Policies = policies.ToList()
        };
    }

    [Theory]
    [InlineData(PolicyEvaluateOn.ApplyAndDestroy, false, true)]
    [InlineData(PolicyEvaluateOn.ApplyAndDestroy, true, true)]
    [InlineData(PolicyEvaluateOn.ApplyOnly, false, true)]
    [InlineData(PolicyEvaluateOn.ApplyOnly, true, false)]
    [InlineData(PolicyEvaluateOn.DestroyOnly, false, false)]
    [InlineData(PolicyEvaluateOn.DestroyOnly, true, true)]
    public void ValidateStep_Matches_Terraform_By_EvaluateOn(PolicyEvaluateOn evaluateOn, bool isDestroy, bool expected)
    {
        Assert.Equal(expected, PolicyApplicability.Matches(Policy(PolicyEngine.Terraform, evaluateOn), isDestroy));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateStep_Never_Matches_Pulumi(bool isDestroy)
    {
        Assert.False(PolicyApplicability.Matches(Policy(PolicyEngine.Pulumi, PolicyEvaluateOn.ApplyAndDestroy), isDestroy));
    }

    [Fact]
    public void PlanStep_Selects_Pulumi_Only_With_EvaluateOn_Filtering()
    {
        var declared = Declared(
            Policy(PolicyEngine.Terraform, PolicyEvaluateOn.ApplyAndDestroy),
            Policy(PolicyEngine.Pulumi, PolicyEvaluateOn.ApplyAndDestroy),
            Policy(PolicyEngine.Pulumi, PolicyEvaluateOn.ApplyOnly),
            Policy(PolicyEngine.Pulumi, PolicyEvaluateOn.DestroyOnly));

        var apply = PolicyApplicability.ForPlanStep(declared, isDestroyJob: false);
        Assert.Equal(2, apply.Count);
        Assert.All(apply, p => Assert.Equal(PolicyEngine.Pulumi, p.Engine));
        Assert.DoesNotContain(apply, p => p.EvaluateOn == PolicyEvaluateOn.DestroyOnly);

        // The pulumi CLI has no policy support on destroy: destroy jobs get no Pulumi policies.
        var destroy = PolicyApplicability.ForPlanStep(declared, isDestroyJob: true);
        Assert.Empty(destroy);
    }

    [Fact]
    public void ValidateStep_For_Selects_Terraform_Only()
    {
        var declared = Declared(
            Policy(PolicyEngine.Terraform, PolicyEvaluateOn.ApplyAndDestroy),
            Policy(PolicyEngine.Pulumi, PolicyEvaluateOn.ApplyAndDestroy));

        var selected = PolicyApplicability.For(declared, isDestroyJob: false);
        var policy = Assert.Single(selected);
        Assert.Equal(PolicyEngine.Terraform, policy.Engine);
    }

    [Fact]
    public void Any_Is_False_For_Pulumi_Only_Policies()
    {
        var declared = Declared(Policy(PolicyEngine.Pulumi, PolicyEvaluateOn.ApplyAndDestroy));
        var json = System.Text.Json.JsonSerializer.Serialize(declared);

        Assert.False(PolicyApplicability.Any(json, isDestroyJob: false));
        Assert.False(PolicyApplicability.Any(json, isDestroyJob: true));
    }
}
