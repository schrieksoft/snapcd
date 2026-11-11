// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net;
using System.Text;
using SnapCd.Cli.Auth;
using SnapCd.Cli.Commands;
using SnapCd.Cli.Configuration;

namespace SnapCd.Cli.Tests;

public class PkceTests
{
    [Fact]
    public void Challenge_matches_the_RFC_7636_example()
    {
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
            Pkce.ChallengeFor("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
    }

    [Fact]
    public void Verifier_is_unpadded_base64url_of_allowed_length()
    {
        var pkce = Pkce.Create();
        Assert.InRange(pkce.Verifier.Length, 43, 128);
        Assert.DoesNotContain('=', pkce.Verifier);
        Assert.DoesNotContain('+', pkce.Verifier);
        Assert.DoesNotContain('/', pkce.Verifier);
    }
}

public class CliConfigStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "snapcd-cli-tests", Guid.NewGuid().ToString());

    [Fact]
    public void Round_trips_contexts_and_the_current_one()
    {
        var store = new CliConfigStore(_directory);
        var orgId = Guid.NewGuid();
        store.Save(new CliConfig
        {
            CurrentContext = "prod",
            Contexts = [new CliContext { Name = "prod", Url = "https://snapcd.example", OrganizationId = orgId, OrganizationName = "acme" }]
        });

        var loaded = store.Load();
        Assert.Equal("prod", loaded.CurrentContext);
        Assert.Equal(orgId, loaded.Resolve(null).OrganizationId);
    }

    [Fact]
    public void Resolve_without_contexts_says_how_to_add_one()
    {
        var error = Assert.Throws<CliException>(() => new CliConfigStore(_directory).Load().Resolve(null));
        Assert.Contains("snapcd context add", error.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}

public class OrganizationPickerTests
{
    private static readonly Organization Acme = new(Guid.NewGuid(), "acme");
    private static readonly Organization Globex = new(Guid.NewGuid(), "globex");

    [Fact]
    public void Picks_the_only_organization_without_asking()
    {
        Assert.Equal(Acme, OrganizationPicker.Pick([Acme], null, TextWriter.Null, new StringReader("")));
    }

    [Fact]
    public void Picks_by_name_or_id()
    {
        Assert.Equal(Globex, OrganizationPicker.Pick([Acme, Globex], "globex", TextWriter.Null, new StringReader("")));
        Assert.Equal(Globex, OrganizationPicker.Pick([Acme, Globex], Globex.Id.ToString(), TextWriter.Null, new StringReader("")));
    }

    [Fact]
    public void Asks_when_there_are_several_and_skips_invalid_answers()
    {
        Assert.Equal(Globex, OrganizationPicker.Pick([Acme, Globex], null, TextWriter.Null, new StringReader("x\n3\n2\n")));
    }

    [Fact]
    public void Refuses_an_organization_the_user_is_not_in()
    {
        Assert.Throws<CliException>(() => OrganizationPicker.Pick([Acme], "initech", TextWriter.Null, new StringReader("")));
    }
}

public class AccessTokenProviderTests
{
    private static readonly CliContext Context = new() { Name = "prod", Url = "https://snapcd.example" };
    private readonly FakeTime _time = new(new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Returns_a_cached_token_that_is_still_valid_without_calling_the_Server()
    {
        var store = new MemoryStore();
        store.Set("prod", new StoredTokens { AccessToken = "cached", AccessTokenExpiresOn = _time.GetUtcNow().AddMinutes(30), RefreshToken = "r1" });
        var handler = new FakeHandler(_ => throw new InvalidOperationException("no call expected"));

        var tokens = await Provider(store, handler).GetAsync(Context, CancellationToken.None);

        Assert.Equal("cached", tokens.AccessToken);
    }

    [Fact]
    public async Task Refreshes_a_token_about_to_expire_and_keeps_the_rotated_refresh_token()
    {
        var store = new MemoryStore();
        store.Set("prod", new StoredTokens { AccessToken = "old", AccessTokenExpiresOn = _time.GetUtcNow().AddMinutes(2), RefreshToken = "r1" });
        string? sentRefreshToken = null;
        var handler = new FakeHandler(request =>
        {
            var form = request.Content!.ReadAsStringAsync().Result;
            sentRefreshToken = System.Web.HttpUtility.ParseQueryString(form)["refresh_token"];
            return Json(HttpStatusCode.OK, """{"access_token":"new","expires_in":3600,"refresh_token":"r2","token_type":"Bearer"}""");
        });

        var tokens = await Provider(store, handler).GetAsync(Context, CancellationToken.None);

        Assert.Equal("r1", sentRefreshToken);
        Assert.Equal("new", tokens.AccessToken);
        Assert.Equal("r2", store.Get("prod")!.RefreshToken);
        Assert.Equal(_time.GetUtcNow().AddHours(1), store.Get("prod")!.AccessTokenExpiresOn);
    }

    [Fact]
    public async Task A_refused_refresh_token_forgets_the_session_and_asks_for_a_login()
    {
        var store = new MemoryStore();
        store.Set("prod", new StoredTokens { AccessToken = "old", AccessTokenExpiresOn = _time.GetUtcNow().AddMinutes(-1), RefreshToken = "r1" });
        var handler = new FakeHandler(_ => Json(HttpStatusCode.BadRequest, """{"error":"invalid_grant","error_description":"The token is no longer valid."}"""));

        var error = await Assert.ThrowsAsync<CliException>(() => Provider(store, handler).GetAsync(Context, CancellationToken.None));

        Assert.Contains("snapcd login", error.Message);
        Assert.Null(store.Get("prod"));
    }

    [Fact]
    public async Task Without_a_session_asks_for_a_login()
    {
        var error = await Assert.ThrowsAsync<CliException>(() =>
            Provider(new MemoryStore(), new FakeHandler(_ => throw new InvalidOperationException())).GetAsync(Context, CancellationToken.None));
        Assert.Contains("snapcd login", error.Message);
    }

    private AccessTokenProvider Provider(ITokenStore store, FakeHandler handler) =>
        new(store, context => new OAuthClient(new HttpClient(handler), context.Url, _time), _time);

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class MemoryStore : ITokenStore
    {
        private readonly Dictionary<string, StoredTokens> _tokens = new();
        public bool IsUnprotected => false;
        public StoredTokens? Get(string context) => _tokens.GetValueOrDefault(context);
        public void Set(string context, StoredTokens tokens) => _tokens[context] = tokens;
        public void Remove(string context) => _tokens.Remove(context);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class FakeTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
