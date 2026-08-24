// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SnapCd.Server.Core.Middleware;
using SnapCd.Server.Core.Services.Resilience;

namespace SnapCd.Server.Core.Tests.Tests.Resilience;

public class TransientFaultRetryMiddlewareTests
{
    /// <summary>Scoped, like SnapCdDbContext — a replayed attempt must not see the previous one's.</summary>
    private sealed class ScopedMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class Recorder
    {
        public int Attempts;
        public readonly List<Guid> ScopeIds = [];
    }

    [Fact]
    public async Task TransientFault_IsRetried_AndSucceeds()
    {
        var recorder = new Recorder();
        using var host = await StartHostAsync(recorder, failuresBeforeSuccess: 1, fault: Deadlock);

        var response = await host.GetTestClient().PostAsync("/anything", new StringContent(""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());
        Assert.Equal(2, recorder.Attempts);
    }

    [Fact]
    public async Task EachAttempt_GetsItsOwnScope()
    {
        var recorder = new Recorder();
        using var host = await StartHostAsync(recorder, failuresBeforeSuccess: 1, fault: Deadlock);

        await host.GetTestClient().PostAsync("/anything", new StringContent(""));

        // The whole point of retrying at this layer: a fresh scope means a fresh DbContext,
        // so the replay cannot inherit the change tracker of the attempt that failed.
        Assert.Equal(2, recorder.ScopeIds.Count);
        Assert.Distinct(recorder.ScopeIds);
    }

    [Fact]
    public async Task FailedAttempt_WritesNothingToTheClient()
    {
        var recorder = new Recorder();
        using var host = await StartHostAsync(recorder, failuresBeforeSuccess: 1, fault: Deadlock,
            writeBeforeThrowing: "PARTIAL");

        var response = await host.GetTestClient().PostAsync("/anything", new StringContent(""));

        // The first attempt wrote before it threw; none of that may reach the client.
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NonTransientFault_IsNotRetried()
    {
        var recorder = new Recorder();
        using var host = await StartHostAsync(recorder, failuresBeforeSuccess: 1,
            fault: new InvalidOperationException("permanent"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.GetTestClient().PostAsync("/anything", new StringContent("")));

        Assert.Equal(1, recorder.Attempts);
    }

    [Fact]
    public async Task GetRequests_AreNotRetried()
    {
        var recorder = new Recorder();
        using var host = await StartHostAsync(recorder, failuresBeforeSuccess: 1, fault: Deadlock);

        await Assert.ThrowsAsync<SqlException>(() => host.GetTestClient().GetAsync("/anything"));

        Assert.Equal(1, recorder.Attempts);
    }

    [Fact]
    public async Task ExhaustedRetries_SurfaceTheFault()
    {
        var recorder = new Recorder();
        using var host = await StartHostAsync(recorder, failuresBeforeSuccess: 99, fault: Deadlock);

        await Assert.ThrowsAsync<SqlException>(
            () => host.GetTestClient().PostAsync("/anything", new StringContent("")));

        Assert.Equal(3, recorder.Attempts);
    }

    private static SqlException Deadlock => SqlError(1205);

    private static async Task<IHost> StartHostAsync(
        Recorder recorder, int failuresBeforeSuccess, Exception fault, string? writeBeforeThrowing = null)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddScoped<ScopedMarker>();
                    services.AddSingleton<ITransientFaultClassifier, TransientFaultClassifier>();
                    services.AddLogging();
                })
                .Configure(app =>
                {
                    app.UseMiddleware<TransientFaultRetryMiddleware>();
                    app.Run(async context =>
                    {
                        recorder.Attempts++;
                        recorder.ScopeIds.Add(context.RequestServices.GetRequiredService<ScopedMarker>().Id);

                        if (recorder.Attempts <= failuresBeforeSuccess)
                        {
                            if (writeBeforeThrowing is not null)
                                await context.Response.WriteAsync(writeBeforeThrowing);
                            throw fault;
                        }

                        await context.Response.WriteAsync("ok");
                    });
                }))
            .StartAsync();

        return host;
    }

    private static SqlException SqlError(int number, string procedure = "")
    {
        var error = (SqlError)Activator.CreateInstance(
            typeof(SqlError), BindingFlags.Instance | BindingFlags.NonPublic, null,
            [number, (byte)0, (byte)0, "server", "message", procedure, 0, 0, null!], null)!;

        var collection = (SqlErrorCollection)Activator.CreateInstance(
            typeof(SqlErrorCollection), BindingFlags.Instance | BindingFlags.NonPublic, null, [], null)!;

        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(collection, [error]);

        return (SqlException)typeof(SqlException)
            .GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic,
                null, [typeof(SqlErrorCollection), typeof(string)], null)!
            .Invoke(null, [collection, "6.0.0"])!;
    }
}
