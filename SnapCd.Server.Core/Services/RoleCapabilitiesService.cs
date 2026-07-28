// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using SnapCd.Contracts;
using SnapCd.Server.Core.Startup;

namespace SnapCd.Server.Core.Services;

public sealed record RoleCapabilityOperation(
    string Method,
    string Path,
    string Controller,
    string? Verb,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> ReverseInheritedRoles,
    string? Notes)
{
    public bool Grants(string qualifiedRole)
    {
        if (Roles.Contains(qualifiedRole)) return true;
        var dimension = qualifiedRole[..qualifiedRole.IndexOf('.')];
        return ReverseInheritedRoles.Contains($"{dimension}.*");
    }
}

/// <summary>
/// The in-process view of the permission catalog for /RoleCapabilities: the same
/// records the OpenAPI transformer emits as x-snapcd-permissions, pivoted by role.
/// Reads the extractor once — permission maps are static data, so the catalog is
/// process-lifetime stable.
/// </summary>
public class RoleCapabilitiesService(IApiDescriptionGroupCollectionProvider apiDescriptionProvider)
{
    private readonly Lazy<IReadOnlyList<RoleCapabilityOperation>> _operations = new(() => Build(apiDescriptionProvider));

    /// <summary>Every documented operation (prose-only skips excluded), sorted by path then method.</summary>
    public IReadOnlyList<RoleCapabilityOperation> Operations => _operations.Value;

    /// <summary>Every role in the system as a qualified "Dimension.Role" name, in dimension order.</summary>
    public IReadOnlyList<string> AllRoles { get; } =
    [
        .. Enum.GetValues<OrganizationRole>().Select(r => $"Organization.{r}"),
        .. Enum.GetValues<StackRole>().Select(r => $"Stack.{r}"),
        .. Enum.GetValues<NamespaceRole>().Select(r => $"Namespace.{r}"),
        .. Enum.GetValues<ModuleRole>().Select(r => $"Module.{r}"),
        .. Enum.GetValues<AgentRole>().Select(r => $"Agent.{r}"),
        .. Enum.GetValues<RunnerRole>().Select(r => $"Runner.{r}"),
        .. Enum.GetValues<IntegrationRole>().Select(r => $"Integration.{r}"),
        .. Enum.GetValues<StateStoreRole>().Select(r => $"StateStore.{r}")
    ];

    public IReadOnlyList<RoleCapabilityOperation> OperationsGrantedTo(string qualifiedRole)
    {
        return Operations.Where(o => o.Grants(qualifiedRole)).ToList();
    }

    private static List<RoleCapabilityOperation> Build(IApiDescriptionGroupCollectionProvider provider)
    {
        var operations = new List<RoleCapabilityOperation>();

        foreach (var group in provider.ApiDescriptionGroups.Items)
        foreach (var description in group.Items)
        {
            if (description.ActionDescriptor is not ControllerActionDescriptor action)
                continue;

            var doc = PermissionDocExtractor.Extract(action);
            if (doc is null || doc.RolesByDimension.Count == 0)
                continue;

            operations.Add(new RoleCapabilityOperation(
                description.HttpMethod ?? "GET",
                $"/{description.RelativePath}",
                action.ControllerName,
                doc.Verb?.ToString(),
                doc.RolesByDimension
                    .SelectMany(kv => kv.Value.Select(role => $"{Capitalize(kv.Key)}.{role}"))
                    .ToList(),
                doc.ReverseInheritedDimensions.Select(d => $"{Capitalize(d)}.*").ToList(),
                doc.Notes));
        }

        return operations
            .OrderBy(o => o.Path, StringComparer.Ordinal)
            .ThenBy(o => o.Method, StringComparer.Ordinal)
            .ToList();
    }

    private static string Capitalize(string dimension)
    {
        return char.ToUpperInvariant(dimension[0]) + dimension[1..];
    }
}
