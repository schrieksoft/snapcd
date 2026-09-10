// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Host.Database;
using SnapCd.Server.Host.Installations;
using SnapCd.Server.Host.Telemetry;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class GitHubReleasesClientTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;

    public GitHubReleasesClientTests(Fixture fixture)
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

    private InstallationService Installation() => new(_provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>());

    [Theory]
    [InlineData("# Maintenance mode\n\nQuiesce the server.", "Maintenance mode")]
    [InlineData("Intro line.\n\n## Policy as Code ##\n", "Policy as Code")]
    [InlineData("Quiesce the Server before a change. New work waits at a gate.", "Quiesce the Server before a change.")]
    [InlineData("Adds **bold** and a [link](https://x) to `code`", "Adds bold and a link to code")]
    [InlineData("<!-- release-notes -->\nFirst real line! Second.", "First real line!")]
    [InlineData("", "v1.2.3 name")]
    [InlineData(null, "v1.2.3 name")]
    public void Heading_IsFirstHeading_ElseFirstSentence_ElseName(string? body, string expected)
    {
        Assert.Equal(expected, GitHubReleasesClient.Heading(body, "v1.2.3 name", "1.2.3"));
    }

    [Fact]
    public void Heading_FallsBackToTag_WhenNothingElse()
    {
        Assert.Equal("1.2.3", GitHubReleasesClient.Heading("   ", " ", "1.2.3"));
    }

    [Fact]
    public async Task ListsOnlyReleasesNewerThanRunning_NewestFirst_SkippingDrafts()
    {
        const string json = """
            [
              {"tag_name":"2.0.0","name":"2.0.0","body":"# Two","draft":false,"prerelease":false},
              {"tag_name":"1.13.0","name":"1.13.0","body":"# Thirteen","html_url":"https://github.com/schrieksoft/snapcd/releases/tag/1.13.0","draft":false,"prerelease":false},
              {"tag_name":"1.12.12","name":"1.12.12","body":"Patch.","draft":false,"prerelease":false},
              {"tag_name":"1.14.0","name":"1.14.0","body":"# Fourteen","draft":true,"prerelease":false},
              {"tag_name":"1.12.11","name":"1.12.11","body":"# Current","draft":false,"prerelease":false},
              {"tag_name":"1.12.10","name":"1.12.10","body":"# Older","draft":false,"prerelease":false}
            ]
            """;
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        var client = Client(handler, "1.12.11");

        var releases = await client.NewerThanRunningAsync();

        Assert.Equal(["2.0.0", "1.13.0", "1.12.12"], releases.Select(r => r.Tag).ToArray());
        Assert.Equal(["major", "minor", "patch"], releases.Select(r => r.Bump).ToArray());
        Assert.Equal("Thirteen", releases[1].Heading);
        Assert.Equal("https://github.com/schrieksoft/snapcd/releases/tag/1.13.0", releases[1].Url);
        Assert.Equal("https://github.com/schrieksoft/snapcd/releases/tag/1.12.12", releases[2].Url);
        Assert.True(client.LastFetchSucceeded);
        Assert.Equal("https://api.github.com/repos/schrieksoft/snapcd/releases?per_page=50", handler.LastUri!.ToString());
        Assert.Equal("2.0.0", (await Installation().GetAsync()).LatestKnownVersion);
    }

    [Fact]
    public async Task Unreachable_IsEmptyAndFlagged_NotAnError()
    {
        var client = Client(new StubHandler(_ => throw new HttpRequestException("offline")), "1.12.11");

        var releases = await client.NewerThanRunningAsync();

        Assert.Empty(releases);
        Assert.False(client.LastFetchSucceeded);
    }

    private GitHubReleasesClient Client(HttpMessageHandler handler, string running) => new(
        new SingleHandlerFactory(handler),
        Options.Create(new TelemetrySettings()),
        new FixedVersion(running),
        Installation(),
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
        NullLogger<GitHubReleasesClient>.Instance);

    private sealed class SelfHostedFactory(IDbContextFactory<SelfHostedSnapCdDbContext> inner) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => inner.CreateDbContext();
    }

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(respond(request));
        }
    }

    private sealed class FixedVersion(string version) : IVersionService
    {
        public string Version => version + "+sha.test";
        public string ShortVersion => version;
    }
}
