// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SnapCd.Server.Core.Controllers.Crud;
using SnapCd.Server.Core.Startup;

namespace SnapCd.Server.Core.Tests.Tests.Routing;

/// <summary>
/// A Guid route segment without a guid constraint matches any value, including the literal segment of a
/// sibling route, so an incomplete path such as ".../Metadata/ByName/" reaches the id route and is answered
/// with a validation error about an id the caller never sent.
/// </summary>
public class GuidRouteConstraintTests
{
    private static readonly Regex RouteParameter = new(@"\{\*{0,2}(?<name>\w+)(?<rest>[^}]*)\}");

    /// <summary>
    /// The server's controller list. Controllers are found through application parts, which default to the
    /// entry assembly's dependencies; under the test runner that is not the server, so its assembly is added.
    /// </summary>
    private static void AddServedControllers(IServiceCollection services)
    {
        services.AddSnapCdControllers();
        services.AddControllers().AddApplicationPart(typeof(SnapCd.Server.Core.Controllers.CustomControllerFeatureProvider).Assembly);
    }

    private static List<Type> ServedControllers()
    {
        var services = new ServiceCollection();
        AddServedControllers(services);
        var partManager = (ApplicationPartManager)services
            .Single(s => s.ServiceType == typeof(ApplicationPartManager)).ImplementationInstance!;
        var feature = new ControllerFeature();
        partManager.PopulateFeature(feature);
        return feature.Controllers.Select(c => c.AsType()).ToList();
    }

    private static bool IsGuid(Type type) => type == typeof(Guid) || type == typeof(Guid?);

    [Fact]
    public void Every_route_segment_bound_to_a_guid_is_constrained_to_one()
    {
        var problems = new List<string>();
        var controllers = ServedControllers();
        Assert.NotEmpty(controllers);

        foreach (var controller in controllers)
        {
            var controllerRoutes = controller.GetCustomAttributes<RouteAttribute>(true).Select(r => r.Template).DefaultIfEmpty("").ToList();

            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var actionRoutes = action.GetCustomAttributes(true).OfType<IRouteTemplateProvider>()
                    .Where(r => r.Template != null).Select(r => r.Template!).ToList();
                if (actionRoutes.Count == 0)
                {
                    if (!action.GetCustomAttributes(true).OfType<HttpMethodAttribute>().Any()) continue;
                    actionRoutes.Add("");
                }

                foreach (var template in controllerRoutes.SelectMany(c => actionRoutes.Select(a => a.StartsWith('/') || a.StartsWith('~') ? a : $"{c}/{a}")))
                foreach (Match segment in RouteParameter.Matches(template))
                {
                    var name = segment.Groups["name"].Value;
                    var constrained = segment.Groups["rest"].Value.Contains(":guid");
                    var parameter = action.GetParameters().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                    if (parameter is not null && IsGuid(parameter.ParameterType) && !constrained)
                        problems.Add($"{controller.Name}.{action.Name}: {{{name}}} is a Guid without :guid in {template}");
                    if (parameter is not null && !IsGuid(parameter.ParameterType) && constrained)
                        problems.Add($"{controller.Name}.{action.Name}: {{{name}}} has :guid but binds to {parameter.ParameterType.Name} in {template}");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems.Distinct()));
    }

    private static async Task<IHost> StartHostAsync() =>
        await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    AddServedControllers(services);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    // Reports the route that matched without running the action, so no controller dependencies are needed.
                    app.Use(async (HttpContext context, RequestDelegate next) =>
                    {
                        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
                        if (action is null)
                        {
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            return;
                        }

                        var sourceUrl = context.GetRouteValue("sourceUrl");
                        await context.Response.WriteAsync(sourceUrl is null
                            ? action.AttributeRouteInfo?.Template ?? ""
                            : $"{action.AttributeRouteInfo?.Template} => {sourceUrl}");
                    });
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .StartAsync();

    private static readonly string Organization = Guid.NewGuid().ToString();

    [Theory]
    [InlineData("Runner/Metadata/ByName/")]
    [InlineData("Runner/ByName/")]
    [InlineData("Namespace/Metadata/ByName/stack/")]
    [InlineData("Namespace/Metadata/ByName/")]
    [InlineData("Module/Metadata/ByName/stack/namespace/")]
    [InlineData("Runner/not-a-guid")]
    public async Task An_incomplete_or_malformed_path_matches_no_route(string path)
    {
        using var host = await StartHostAsync();

        var response = await host.GetTestClient().GetAsync($"/api/{Organization}/{path}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_organization_that_is_not_a_guid_matches_no_route()
    {
        using var host = await StartHostAsync();

        var response = await host.GetTestClient().GetAsync("/api/not-a-guid/Runner");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Runner/Metadata/ByName/my-runner", "Runner/Metadata/ByName/{name}")]
    [InlineData("Runner/ByName/my-runner", "Runner/ByName/{name}")]
    [InlineData("Runner/Metadata/{guid}", "Runner/Metadata/{id:guid}")]
    [InlineData("Runner/{guid}", "Runner/{id:guid}")]
    [InlineData("Namespace/Metadata/{guid}/my-namespace", "Namespace/Metadata/{stackId:guid}/{name}")]
    [InlineData("Namespace/Metadata/ByName/my-stack/my-namespace", "Namespace/Metadata/ByName/{stackName}/{name}")]
    public async Task A_complete_path_still_reaches_its_route(string path, string expectedRoute)
    {
        using var host = await StartHostAsync();

        var response = await host.GetTestClient().GetAsync($"/api/{Organization}/{path.Replace("{guid}", Guid.NewGuid().ToString())}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"api/{{organizationId:guid}}/{expectedRoute}", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("SourceRefresherPreselection/BySourceUrl/", "SourceRefresherPreselection/BySourceUrl/{**sourceUrl}")]
    [InlineData("SourceRefresherPreselection/Metadata/BySourceUrl/", "SourceRefresherPreselection/Metadata/BySourceUrl/{**sourceUrl}")]
    public async Task A_source_url_reaches_the_lookup_whole(string prefix, string expectedRoute)
    {
        using var host = await StartHostAsync();
        const string sourceUrl = "https://github.com/org/repo.git";

        var raw = await host.GetTestClient().GetAsync($"/api/{Organization}/{prefix}{sourceUrl}");
        var escaped = await host.GetTestClient().GetAsync($"/api/{Organization}/{prefix}{Uri.EscapeDataString(sourceUrl)}");

        var route = $"api/{{organizationId:guid}}/{expectedRoute} => ";
        var rawValue = (await raw.Content.ReadAsStringAsync())[route.Length..];
        var escapedValue = (await escaped.Content.ReadAsStringAsync())[route.Length..];

        Assert.Equal(sourceUrl, SourceRefresherPreselectionCustomEndpointNames.SourceUrlFromRoute(rawValue));
        Assert.Equal(sourceUrl, SourceRefresherPreselectionCustomEndpointNames.SourceUrlFromRoute(escapedValue));
    }

    [Theory]
    [InlineData("https:%2F%2Fgithub.com%2Forg%2Frepo.git", "https://github.com/org/repo.git")]
    [InlineData("https://github.com/org/repo.git", "https://github.com/org/repo.git")]
    [InlineData("git@github.com:org/repo.git", "git@github.com:org/repo.git")]
    public void A_source_url_route_value_is_read_back_as_sent(string routeValue, string expected) =>
        Assert.Equal(expected, SourceRefresherPreselectionCustomEndpointNames.SourceUrlFromRoute(routeValue));
}
