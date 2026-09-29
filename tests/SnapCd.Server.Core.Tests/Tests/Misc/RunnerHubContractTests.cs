// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Hubs;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Misc;

/// <summary>
/// The runner reaches the server by method name over SignalR, so a wrapper whose endpoint has no
/// matching hub method compiles, ships, and fails only when that step runs. Two such breaks have
/// reached main already: the cancel wrappers after the family split, and StateMoveCompleted after
/// the family was renamed. The job harness cannot catch either, because its fake runner calls the
/// hub directly rather than going through the client.
/// </summary>
public class RunnerHubContractTests
{
    [Fact]
    public void Every_Client_Wrapper_Reaches_A_Hub_Method()
    {
        var hubMethods = typeof(RunnerHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToHashSet();

        var endpoints = typeof(ServerEndpoints)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!);

        var unreachable = endpoints
            .Where(e => !hubMethods.Contains(e.Value))
            .Select(e => $"ServerEndpoints.{e.Key} = \"{e.Value}\"")
            .OrderBy(x => x)
            .ToList();

        Assert.True(unreachable.Count == 0,
            "These server endpoints name no method on RunnerHub, so a runner calling them fails at "
            + "run time:\n  " + string.Join("\n  ", unreachable));
    }
}
