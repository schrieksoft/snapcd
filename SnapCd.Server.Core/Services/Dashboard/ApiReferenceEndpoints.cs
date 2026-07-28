// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;

namespace SnapCd.Server.Core.Services.Dashboard;

public static class ApiReferenceEndpoints
{
    private static readonly string CacheBust = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// The standalone Scalar page framed by /ApiReference. Scalar assumes it owns the
    /// document (window scrolling, document-coordinate popovers, body theme classes),
    /// so it gets a document of its own; the dashboard page embeds it in an iframe.
    /// </summary>
    public static IEndpointConventionBuilder MapApiReferenceStandalone(this IEndpointRouteBuilder endpoints)
    {
        var configuration = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        var serverHost = (configuration["Server:Host"] ?? "").TrimEnd('/');

        return endpoints.MapGet("/ApiReference/Standalone", (HttpContext context) =>
        {
            var scalarConfig = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["url"] = "/openapi/v1.json",
                ["withDefaultFonts"] = false,
                // Label sidebar and search entries by path. The alternative ("summary")
                // falls back to the path only when a summary is absent, so a partially
                // documented API gets an inconsistent mix of paths and prose sentences.
                ["operationTitleSource"] = "path",
                ["hideClientButton"] = true,
                ["showDeveloperTools"] = "never",
                ["agent"] = new Dictionary<string, object> { ["disabled"] = true },
                ["mcp"] = new Dictionary<string, object> { ["disabled"] = true },
                ["authentication"] = new Dictionary<string, object>
                {
                    ["preferredSecurityScheme"] = "snapcd",
                    ["securitySchemes"] = new Dictionary<string, object>
                    {
                        ["snapcd"] = new Dictionary<string, object>
                        {
                            ["flows"] = new Dictionary<string, object>
                            {
                                ["authorizationCode"] = new Dictionary<string, object>
                                {
                                    ["x-scalar-client-id"] = "ScalarClient",
                                    ["x-usePkce"] = "SHA-256",
                                    ["selectedScopes"] = new[] { "snapcd_scope" },
                                    ["x-scalar-redirect-uri"] = $"{serverHost}/ApiReference/Standalone"
                                }
                            }
                        }
                    }
                }
            });

            var html = $$"""
                <!DOCTYPE html>
                <html lang="en" class="{{ThemeCookie.HtmlClass(context)}}">
                <head>
                    <meta charset="utf-8"/>
                    <meta name="color-scheme" content="{{ThemeCookie.ColorMode(context)}}"/>
                    <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
                    <base href="/"/>
                    <title>API Reference - Snap CD</title>
                    <link href="_content/SnapCd.Server.Core/snapcd-theme.css" rel="stylesheet"/>
                </head>
                <body>
                <div id="scalar-api-reference"></div>
                <script type="module">
                    import { init } from '/_content/SnapCd.Server.Core/scalar/scalar-interop.js?v={{CacheBust}}';
                    init('scalar-api-reference', {{scalarConfig}});
                </script>
                </body>
                </html>
                """;

            return Results.Content(html, "text/html");
        }).ExcludeFromDescription();
    }
}
