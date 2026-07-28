// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Controllers;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Misc.Attributes;
using SnapCd.Server.Core.Misc.Helpers;

namespace SnapCd.Server.Core.Startup;

public sealed record PermissionDoc(
    PermissionVerb? Verb,
    IReadOnlyDictionary<string, IReadOnlyList<string>> RolesByDimension,
    IReadOnlyList<string> ReverseInheritedDimensions,
    string? Notes,
    string Source);

public enum PermissionCoverage
{
    /// <summary>Permissions resolved and documented.</summary>
    Documented,

    /// <summary>Deliberately undocumented via [PermissionSource(Skip = true)].</summary>
    Skipped,

    /// <summary>No resolution and no explicit skip — a gap the coverage report surfaces.</summary>
    Unresolved
}

/// <summary>
/// Resolves the permission documentation for a controller action from the secured
/// repository that enforces it. Resolution is convention/generics/inheritance by
/// default with <see cref="PermissionSourceAttribute"/> as the override:
/// repo = GenericCrudController type argument → {X}Controller → {X}SecuredRepository
/// name match → attribute; verb = base-method identity → action-name prefix →
/// attribute. Pure reflection, no DI, no database — the permission maps are literal
/// data readable from an uninitialized instance, which is what lets this run
/// identically in the live server and the headless OpenAPI generator.
/// </summary>
public static class PermissionDocExtractor
{
    // Fixed dimension order: emission must be deterministic (the artifact is diffed).
    private static readonly (string Name, Func<PermissionMap, IEnumerable<Enum>> Roles)[] Dimensions =
    [
        ("organization", m => m.OrganizationRoles.Cast<Enum>()),
        ("stack", m => m.StackRoles.Cast<Enum>()),
        ("namespace", m => m.NamespaceRoles.Cast<Enum>()),
        ("module", m => m.ModuleRoles.Cast<Enum>()),
        ("agent", m => m.AgentRoles.Cast<Enum>()),
        ("runner", m => m.RunnerRoles.Cast<Enum>()),
        ("integration", m => m.IntegrationRoles.Cast<Enum>()),
        ("stateStore", m => m.StateStoreRoles.Cast<Enum>())
    ];

    private static readonly ConcurrentDictionary<(Type Controller, string Action), (PermissionDoc? Doc, PermissionCoverage Coverage)> Cache = new();

    public static PermissionDoc? Extract(ControllerActionDescriptor action)
    {
        return ExtractWithCoverage(action).Doc;
    }

    public static (PermissionDoc? Doc, PermissionCoverage Coverage) ExtractWithCoverage(ControllerActionDescriptor action)
    {
        return Cache.GetOrAdd((action.ControllerTypeInfo.AsType(), action.MethodInfo.Name),
            _ => ExtractCore(action.ControllerTypeInfo.AsType(), action.MethodInfo));
    }

    private static (PermissionDoc? Doc, PermissionCoverage Coverage) ExtractCore(Type controllerType, MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<PermissionSourceAttribute>()
                        ?? controllerType.GetCustomAttribute<PermissionSourceAttribute>();
        if (attribute?.Skip == true)
        {
            // Skip + Notes documents authorization as prose only — for endpoints whose
            // auth model is not role-map-shaped.
            var proseDoc = attribute.Notes is null
                ? null
                : new PermissionDoc(null, new Dictionary<string, IReadOnlyList<string>>(), [],
                    attribute.Notes, controllerType.Name);
            return (proseDoc, PermissionCoverage.Skipped);
        }

        var verb = ResolveVerb(method, attribute);
        if (verb is null) return (null, PermissionCoverage.Unresolved);

        var repoTypes = ResolveRepositories(controllerType, attribute);
        if (repoTypes.Count == 0) return (null, PermissionCoverage.Unresolved);

        // All resolutions of the repo (open-generic closings included) must agree on
        // the map — permissions varying by a request-body discriminator could not be
        // documented as one truth and would be an API-design smell.
        var docs = repoTypes.Select(t => ReadMap(t, verb.Value)).ToList();
        // Repos without the four-map shape (per-user resources like UserColor /
        // UserFavorite, where permission is row ownership rather than roles) are
        // not documentable from maps — leave their operations untouched.
        if (docs.Any(d => d is null)) return (null, PermissionCoverage.Unresolved);
        var first = docs[0]!.Value;
        foreach (var other in docs.Skip(1))
        {
            if (!RolesEqual(first.Roles, other!.Value.Roles)
                || !first.ReverseDimensions.SequenceEqual(other.Value.ReverseDimensions))
                throw new InvalidOperationException(
                    $"Permission maps disagree between closings of the secured repository for " +
                    $"{controllerType.Name}.{method.Name} ({verb}): {repoTypes[0].Name} vs {other.Value.Source}.");
        }

        if (first.Roles.Count == 0) return (null, PermissionCoverage.Unresolved);

        return (new PermissionDoc(verb.Value, first.Roles, first.ReverseDimensions,
                attribute?.Notes ?? first.Notes, first.Source),
            PermissionCoverage.Documented);
    }

    private static List<Type> ResolveRepositories(Type controllerType, PermissionSourceAttribute? attribute)
    {
        var fromGenerics = ResolveFromGenericController(controllerType);
        var fromName = ResolveByName(controllerType);

        if (fromGenerics is not null && fromName is not null)
        {
            var genericsDef = fromGenerics.IsGenericType ? fromGenerics.GetGenericTypeDefinition() : fromGenerics;
            if (genericsDef != fromName && fromGenerics != fromName)
                throw new InvalidOperationException(
                    $"Permission source mismatch for {controllerType.Name}: generic argument {fromGenerics.Name} " +
                    $"vs name convention {fromName.Name}.");
        }

        if (attribute?.Repository is { } declared)
        {
            if (fromGenerics is not null && declared != fromGenerics && !attribute.OverridesInheritance)
                throw new InvalidOperationException(
                    $"[PermissionSource] on {controllerType.Name} contradicts the controller's generic argument " +
                    $"({fromGenerics.Name}); set OverridesInheritance = true if that is intended.");
            return Close(declared);
        }

        if (fromGenerics is not null) return [fromGenerics];
        if (fromName is not null) return Close(fromName);
        return [];
    }

    private static Type? ResolveFromGenericController(Type controllerType)
    {
        for (var t = controllerType; t is not null; t = t.BaseType!)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(GenericCrudController<,,,,,,,,,,>))
                return t.GetGenericArguments()[4]; // TSecuredRepository
        }

        return null;
    }

    private static Type? ResolveByName(Type controllerType)
    {
        if (!controllerType.Name.EndsWith("Controller")) return null;
        var wanted = controllerType.Name[..^"Controller".Length] + "SecuredRepository";

        var matches = typeof(PermissionDocExtractor).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                        && (t.Name == wanted || t.Name == wanted + "`1"))
            .ToList();

        return matches.Count switch
        {
            0 => null,
            1 => matches[0],
            _ => throw new InvalidOperationException(
                $"Name convention for {controllerType.Name} is ambiguous: " +
                string.Join(", ", matches.Select(m => m.FullName)))
        };
    }

    private static List<Type> Close(Type repoType)
    {
        if (!repoType.IsGenericTypeDefinition) return [repoType];

        var parameters = repoType.GetGenericArguments();
        if (parameters.Length != 1)
            throw new InvalidOperationException(
                $"{repoType.Name} has {parameters.Length} type parameters; the extractor only closes single-parameter repositories.");

        var constraints = parameters[0].GetGenericParameterConstraints();
        var closed = repoType.Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && constraints.All(c => c.IsAssignableFrom(t)))
            .Select(entity => repoType.MakeGenericType(entity))
            .ToList();

        if (closed.Count == 0)
            throw new InvalidOperationException($"No entity type satisfies the constraints of {repoType.Name}.");

        return closed;
    }

    private static PermissionVerb? ResolveVerb(MethodInfo method, PermissionSourceAttribute? attribute)
    {
        if (attribute?.VerbOrNull is { } declared) return declared;

        var baseDefinition = method.GetBaseDefinition();
        if (baseDefinition.DeclaringType is { IsGenericType: true } declaring
            && declaring.GetGenericTypeDefinition() == typeof(GenericCrudController<,,,,,,,,,,>))
        {
            return baseDefinition.Name switch
            {
                "Create" => PermissionVerb.Create,
                "Update" => PermissionVerb.Update,
                "Delete" => PermissionVerb.Delete,
                "Get" or "List" or "Count" => PermissionVerb.Read,
                _ => null
            };
        }

        var name = method.Name;
        if (name.StartsWith("Get") || name.StartsWith("List") || name.StartsWith("Count") || name.EndsWith("ByName"))
            return PermissionVerb.Read;
        if (name.StartsWith("Create") || name.StartsWith("Add"))
            return PermissionVerb.Create;
        if (name.StartsWith("Update") || name.StartsWith("Set"))
            return PermissionVerb.Update;
        if (name.StartsWith("Delete") || name.StartsWith("Remove"))
            return PermissionVerb.Delete;

        return null;
    }

    private static (IReadOnlyDictionary<string, IReadOnlyList<string>> Roles, IReadOnlyList<string> ReverseDimensions,
        string? Notes, string Source)?
        ReadMap(Type repoType, PermissionVerb verb)
    {
        var property = repoType.GetProperty($"{verb}PermissionMap");
        if (property is null) return null;

        // The maps are literal data (enforced from Phase 2 by a purity check), so an
        // uninitialized instance suffices — no constructor, no DbContext.
        var instance = RuntimeHelpers.GetUninitializedObject(repoType);
        var map = (PermissionMap)property.GetValue(instance)!;

        var roles = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var (dimensionName, select) in Dimensions)
        {
            var names = select(map).Select(r => r.ToString()).OrderBy(r => r, StringComparer.Ordinal).ToList();
            if (names.Count > 0) roles[dimensionName] = names;
        }

        // Reverse inheritance applies to reads only: any role on a contained resource
        // grants read, so only the dimensions matter, not the role lists.
        var reverseDimensions = new List<string>();
        if (verb == PermissionVerb.Read
            && repoType.GetProperty("ReverseInheritedReadPermissionMap")?.GetValue(instance) is PermissionMap reverse)
            foreach (var (dimensionName, select) in Dimensions)
                if (select(reverse).Any())
                    reverseDimensions.Add(dimensionName);

        var notes = repoType.GetProperty("PermissionNotes")?.GetValue(instance) as string;
        return (roles, reverseDimensions, notes, repoType.Name);
    }

    private static bool RolesEqual(
        IReadOnlyDictionary<string, IReadOnlyList<string>> a,
        IReadOnlyDictionary<string, IReadOnlyList<string>> b)
    {
        return a.Count == b.Count
               && a.All(kv => b.TryGetValue(kv.Key, out var other) && kv.Value.SequenceEqual(other));
    }
}
