// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using SnapCd.Server.Core.Health;

namespace SnapCd.Server.Core.Tests.Tests.Health;

public class HealthEndpointsTests
{
    private static Mock<HealthCheckService> Checks(HealthStatus status)
    {
        var checks = new Mock<HealthCheckService>();
        checks.Setup(c => c.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthReport(new Dictionary<string, HealthReportEntry>(), status, TimeSpan.Zero));
        return checks;
    }

    [Fact]
    public async Task Answers_starting_without_running_the_checks_until_the_bus_has_started()
    {
        var checks = Checks(HealthStatus.Unhealthy);

        var result = (ContentHttpResult)await HealthEndpoints.Ready(new BusStartupState(), checks.Object, CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("Starting", result.ResponseContent);
        checks.Verify(c => c.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(HealthStatus.Healthy, StatusCodes.Status200OK)]
    [InlineData(HealthStatus.Degraded, StatusCodes.Status200OK)]
    [InlineData(HealthStatus.Unhealthy, StatusCodes.Status503ServiceUnavailable)]
    public async Task Runs_the_checks_once_the_bus_has_started(HealthStatus status, int expected)
    {
        var started = new BusStartupState();
        started.MarkStarted();

        var result = (ContentHttpResult)await HealthEndpoints.Ready(started, Checks(status).Object, CancellationToken.None);

        Assert.Equal(expected, result.StatusCode);
        Assert.Equal(status.ToString(), result.ResponseContent);
    }

    [Fact]
    public async Task The_observer_marks_the_bus_started_when_it_reports_ready()
    {
        var state = new BusStartupState();
        var ready = new TaskCompletionSource<BusReady>();

        var observer = new BusStartupObserver(state, Microsoft.Extensions.Logging.Abstractions.NullLogger<BusStartupObserver>.Instance);
        await observer.PreStart(Mock.Of<IBus>());
        await observer.PostStart(Mock.Of<IBus>(), ready.Task);
        Assert.False(state.HasStarted);

        ready.SetResult(Mock.Of<BusReady>());
        await ready.Task;
        await Task.Yield();
        for (var i = 0; i < 50 && !state.HasStarted; i++)
            await Task.Delay(10);

        Assert.True(state.HasStarted);
    }

    [Fact]
    public async Task The_observer_leaves_the_bus_not_started_when_starting_faults()
    {
        var state = new BusStartupState();
        var ready = new TaskCompletionSource<BusReady>();

        var observer = new BusStartupObserver(state, Microsoft.Extensions.Logging.Abstractions.NullLogger<BusStartupObserver>.Instance);
        await observer.PreStart(Mock.Of<IBus>());
        await observer.PostStart(Mock.Of<IBus>(), ready.Task);
        ready.SetException(new InvalidOperationException("transport unavailable"));
        await Task.Delay(50);

        Assert.False(state.HasStarted);
    }
}
