// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Host.Database;
using SnapCd.Server.Host.Installations;
using SnapCd.Server.Core.Telemetry;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Host.Licensing.Services;
using SnapCd.Server.Host.Telemetry;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

/// <summary>The beacon from the self-hosted side: what it sends, what it keeps, and that it never matters when the other end is gone.</summary>
[Collection("NewRoleBasedSharedFixture")]
public class TelemetryReportJobTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;

    public TelemetryReportJobTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SelfHostedSnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddSingleton<IDbContextFactory<SnapCdDbContext>>(sp => new SelfHostedFactory(sp.GetRequiredService<IDbContextFactory<SelfHostedSnapCdDbContext>>()));
        _provider = services.BuildServiceProvider(true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Snapshot_IsSeedVersionAndTwoCounts_NothingElse()
    {
        var snapshot = await Snapshots("1.12.11").BuildAsync();

        await using var db = _fixture.CreateDbContext();
        Assert.Equal((await Installation().GetAsync()).SeedGuid, snapshot.SeedGuid);
        Assert.Equal("1.12.11", snapshot.Version);
        Assert.Equal(await db.Modules.CountAsync(), snapshot.ModuleCount);
        Assert.Equal(await db.ModuleJobs.LongCountAsync(), snapshot.JobsTotal);
        Assert.Equal(4, typeof(TelemetryReportRequest).GetProperties().Length);
    }

    [Fact]
    public async Task Disabled_SendsNothing()
    {
        var handler = new CapturingHandler(_ => Ok("""{"whatsNew":[]}"""));

        await Job(handler, enabled: false).ExecuteJob();

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Enabled_PostsExactlyTheFourFields_ToTheLicensingHost()
    {
        var handler = new CapturingHandler(_ => Ok("""{"whatsNew":[]}"""));

        await Job(handler, enabled: true, version: "1.12.11").ExecuteJob();

        var (uri, body) = Assert.Single(handler.Requests);
        Assert.Equal("https://license.test/api/telemetry", uri.ToString());
        using var json = JsonDocument.Parse(body);
        var names = json.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();
        Assert.Equal(["jobsTotal", "moduleCount", "seedGuid", "version"], names);
        Assert.Equal("1.12.11", json.RootElement.GetProperty("version").GetString());
        Assert.Equal((await Installation().GetAsync()).SeedGuid, json.RootElement.GetProperty("seedGuid").GetGuid());
    }

    [Fact]
    public async Task Response_ReplacesStoredNotices_NewestFirst()
    {
        var older = Guid.NewGuid();
        var newer = Guid.NewGuid();
        await Installation().UpdateAsync(row =>
        {
            row.WhatsNewJson = JsonSerializer.Serialize(new List<WhatsNewEntryDto>
            {
                new(older, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Notice", "Older", "old"),
            });
        });
        var handler = new CapturingHandler(_ => Ok($$"""
            {"whatsNew":[
              {"id":"{{newer}}","publishedUtc":"2026-02-01T00:00:00Z","kind":"Alert","title":"Newer","markdown":"new"},
              {"id":"{{older}}","publishedUtc":"2026-01-01T00:00:00Z","kind":"Notice","title":"Older, edited","markdown":"edited"}
            ]}
            """));

        await Job(handler, enabled: true).ExecuteJob();

        var row = await Installation().GetAsync();
        Assert.NotNull(row.LastTelemetryReportAtUtc);
        var stored = TelemetrySnapshotProvider.ReadStored(row.WhatsNewJson);
        Assert.Equal(["Newer", "Older, edited"], stored.Select(e => e.Title).ToArray());
        Assert.Equal(["Alert", "Notice"], stored.Select(e => e.Kind).ToArray());
    }

    [Fact]
    public async Task Unreachable_CompletesQuietly_AndLeavesTheRowAlone()
    {
        var before = await Installation().GetAsync();
        var handler = new CapturingHandler(_ => throw new HttpRequestException("connection refused"));
        var clientLog = new CollectingLogger<TelemetryClient>();
        var jobLog = new CollectingLogger<TelemetryReportJob>();

        await Job(handler, enabled: true, clientLog: clientLog, jobLog: jobLog).ExecuteJob();

        var after = await Installation().GetAsync();
        Assert.Equal(before.LastTelemetryReportAtUtc, after.LastTelemetryReportAtUtc);
        Assert.Equal(before.LatestKnownVersion, after.LatestKnownVersion);
        Assert.All(clientLog.Entries.Concat(jobLog.Entries), e => Assert.True(e.Level <= LogLevel.Debug, $"{e.Level}: {e.Message}"));
    }

    [Fact]
    public async Task ServerError_IsRetriedThreeTimes_ThenQuiet()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var clientLog = new CollectingLogger<TelemetryClient>();

        await Job(handler, enabled: true, clientLog: clientLog).ExecuteJob();

        Assert.Equal(3, handler.Requests.Count);
        Assert.All(clientLog.Entries, e => Assert.True(e.Level <= LogLevel.Debug));
    }

    [Fact]
    public async Task TransientFailure_ThenSuccess_StopsRetrying()
    {
        var calls = 0;
        var handler = new CapturingHandler(_ => ++calls < 2
            ? throw new HttpRequestException("blip")
            : Ok("""{"whatsNew":[]}"""));

        await Job(handler, enabled: true).ExecuteJob();

        Assert.Equal(2, handler.Requests.Count);
        Assert.NotNull((await Installation().GetAsync()).LastTelemetryReportAtUtc);
    }

    [Fact]
    public async Task RejectedPayload_IsNotRetried()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));

        await Job(handler, enabled: true).ExecuteJob();

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task LicenceCalls_CarryTheInstallationSeed()
    {
        var handler = new CapturingHandler(_ => Ok("""{"token":"t","expiresAtUtc":"2027-01-01T00:00:00Z","licensePeriodEndUtc":"2027-01-01T00:00:00Z"}"""));
        var client = new RemoteLicenseClient(Factory(handler), LicenseOptions(), Installation(), new CollectingLogger<RemoteLicenseClient>());
        var seed = (await Installation().GetAsync()).SeedGuid;

        await client.IssueAsync("shsk_key");
        await client.RefreshAsync("shsk_key", "old-token");

        Assert.Equal(2, handler.Requests.Count);
        foreach (var (_, body) in handler.Requests)
        {
            Assert.Equal(seed, JsonDocument.Parse(body).RootElement.GetProperty("seedGuid").GetGuid());
        }
    }

    private InstallationService Installation() =>
        new(_provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>());

    private TelemetrySnapshotProvider Snapshots(string version) =>
        new(_provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>(), Installation(), new FixedVersion(version));

    private TelemetryReportJob Job(CapturingHandler handler, bool enabled, string version = "1.0.0",
        CollectingLogger<TelemetryClient>? clientLog = null, CollectingLogger<TelemetryReportJob>? jobLog = null)
    {
        var client = new TelemetryClient(Factory(handler), LicenseOptions(), clientLog ?? new CollectingLogger<TelemetryClient>()) { Backoff = [TimeSpan.Zero, TimeSpan.Zero] };
        var settings = Options.Create(new TelemetrySettings { Enabled = enabled });
        var releases = new GitHubReleasesClient(Factory(new CapturingHandler(_ => throw new HttpRequestException("no github in tests"))),
            settings, new FixedVersion(version), Installation(), new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), new CollectingLogger<GitHubReleasesClient>());
        return new TelemetryReportJob(Snapshots(version), client, releases, Installation(), settings, jobLog ?? new CollectingLogger<TelemetryReportJob>());
    }

    private static IOptions<LicenseSettings> LicenseOptions() =>
        Options.Create(new LicenseSettings { LicenseServerBaseUrl = "https://license.test/" });

    private static IHttpClientFactory Factory(HttpMessageHandler handler) => new SingleHandlerFactory(handler);

    private static HttpResponseMessage Ok(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(Uri Uri, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Requests.Add((request.RequestUri!, body));
            return respond(request);
        }
    }

    private sealed class FixedVersion(string version) : IVersionService
    {
        public string Version => version + "+sha.test";
        public string ShortVersion => version;
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    private sealed class SelfHostedFactory(IDbContextFactory<SelfHostedSnapCdDbContext> inner) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => inner.CreateDbContext();
    }
}
