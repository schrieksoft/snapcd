// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using SnapCd.Contracts.Endpoints;
using SnapCd.Runner.Hub;
using Xunit;

namespace SnapCd.Runner.Tests;

/// <summary>
/// A step the server can dispatch has to be one the runner answers. SignalR drops an invocation
/// with no registered handler and reports nothing to either side, so the job waits on a reply that
/// never comes and the only symptom is a step that stays open.
/// </summary>
public class RunnerEndpointCoverageTests
{
    private static readonly Assembly Contracts = typeof(ITransferEndpoints).Assembly;

    /// <summary>
    /// Every endpoint interface in contracts, found rather than listed, so a new family is covered
    /// the moment it is declared.
    /// </summary>
    public static TheoryData<Type> EndpointInterfaces
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in EndpointInterfaceTypes())
                data.Add(type);
            return data;
        }
    }

    private static IEnumerable<Type> EndpointInterfaceTypes() =>
        Contracts.GetTypes()
            .Where(t => t.IsInterface
                        && t.Namespace == typeof(ITransferEndpoints).Namespace
                        && t.Name.EndsWith("Endpoints"))
            .OrderBy(t => t.Name);

    [Theory]
    [MemberData(nameof(EndpointInterfaces))]
    public void RunnerImplementsEveryEndpointInterface(Type endpointInterface) =>
        Assert.True(
            endpointInterface.IsAssignableFrom(typeof(RunnerHubConnection)),
            $"RunnerHubConnection does not implement {endpointInterface.Name}, so the steps it "
            + "declares would be dispatched to a runner with no handler for them.");

    /// <summary>
    /// The runner registers each handler under the interface member's own name, so a member with no
    /// registration is a step that is dispatched and silently dropped. The registration names are
    /// read from the source, because what a handler is registered under is not visible on the type.
    /// </summary>
    [Fact]
    public void EveryEndpointMemberIsRegisteredOnTheConnection()
    {
        var source = ReadRunnerHubConnectionSource();

        var unregistered = EndpointInterfaceTypes()
            .SelectMany(i => i.GetMethods().Select(m => $"{i.Name}.{m.Name}"))
            .Where(member => !source.Contains($"nameof({member})"))
            .ToList();

        Assert.True(
            unregistered.Count == 0,
            "These endpoints have no _connection.On registration, so the server can dispatch them "
            + "and the runner will drop them: " + string.Join(", ", unregistered));
    }

    /// <summary>
    /// A registration naming something that is not an endpoint member would compile - nameof binds
    /// to any member - but would not be dispatched to, so it is dead wiring.
    /// </summary>
    [Fact]
    public void EveryRegistrationNamesAnEndpointMember()
    {
        var source = ReadRunnerHubConnectionSource();

        var declared = EndpointInterfaceTypes()
            .SelectMany(i => i.GetMethods().Select(m => $"{i.Name}.{m.Name}"))
            .ToHashSet();

        var registered = System.Text.RegularExpressions.Regex
            .Matches(source, @"nameof\((I\w+Endpoints\.\w+)\)")
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        var orphans = registered.Except(declared).OrderBy(x => x).ToList();

        Assert.True(
            orphans.Count == 0,
            "These registrations do not name an endpoint interface member: "
            + string.Join(", ", orphans));
    }

    private static string ReadRunnerHubConnectionSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null
               && !File.Exists(Path.Combine(directory.FullName, "SnapCd.sln")))
            directory = directory.Parent;

        Assert.NotNull(directory);

        var path = Path.Combine(
            directory!.FullName, "SnapCd.Runner", "Hub", "RunnerHubConnection.cs");

        Assert.True(File.Exists(path), $"Could not find the runner hub connection at {path}");

        return File.ReadAllText(path);
    }
}
