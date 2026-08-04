// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Configuration;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Startup;

// Minimal host that exists only to describe the API surface, so the build-time
// `dotnet-getdocument` tool (Microsoft.Extensions.ApiDescription.Server) can emit the
// OpenAPI document straight to a file — see scripts/check-openapi-document.sh.
//
// Registers only what document generation reads: the controllers (the API surface) and
// the OpenAPI configuration (security scheme + transformers). Nothing that talks to SQL,
// the bus or the network is registered, which is why the document generates anywhere
// without infrastructure — unlike SnapCd.Server.Host, whose Program migrates the
// database and starts Hangfire before any document could be read.

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

// The committed artifact must be gap-free: unresolved permission coverage fails
// generation here (and so pre-commit), while the live server only warns.
PermissionsCoverageDocumentTransformer.Strict = true;
SchemaDocsCoverageDocumentTransformer.Strict = true;

// Server:Host feeds the OAuth URLs in the security scheme. No request is ever made
// against it, but it lands in the emitted document, so it stays overridable.
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Server:Host"] = builder.Configuration["Server:Host"] ?? "https://localhost"
});

// The org-prefill transformer takes IHttpContextAccessor; with no request in flight it
// reads no cookie and stamps no example, which is what the committed document wants.
builder.Services.AddHttpContextAccessor();
// Stamps info.version. The assembly version is only the real release string in a CI build
// (GitVersion stamps it); a plain `dotnet build` leaves it at the 1.0.0 default, which would
// otherwise be baked into the committed document and every artifact derived from it. So the
// release pipeline passes the version explicitly via SNAPCD_VERSION.
var versionOverride = builder.Configuration["SNAPCD_VERSION"]
                      ?? Environment.GetEnvironmentVariable("SNAPCD_VERSION");
builder.Services.AddSingleton<IVersionService>(
    string.IsNullOrWhiteSpace(versionOverride)
        ? new VersionService()
        : new FixedVersionService(versionOverride));
builder.Services.AddSnapCdControllers();
builder.Services.AddSnapCdScalarConfiguration((ConfigurationManager)builder.Configuration);

var app = builder.Build();
app.MapOpenApi();
app.Run();

/// <summary>
/// Reports a version supplied by the caller rather than read from the assembly, so the emitted
/// document carries the release being built.
/// </summary>
file sealed class FixedVersionService(string version) : IVersionService
{
    public string Version { get; } = version;

    public string ShortVersion
    {
        get
        {
            var plusIndex = Version.IndexOf('+');
            return plusIndex > 0 ? Version[..plusIndex] : Version;
        }
    }
}
