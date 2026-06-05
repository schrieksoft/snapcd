// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SnapCd.Mcp.Generator;

/// <summary>
/// Roslyn incremental source generator that emits MCP tool / resource wrappers for every controller
/// action annotated with <c>[ExposeAsMcpTool]</c> or <c>[ExposeAsMcpResource]</c>.
///
/// Output: one <c>&lt;ControllerName&gt;McpSurface.g.cs</c> per controller, containing
/// <c>[McpServerToolType]</c> / <c>[McpServerResourceType]</c> static classes with <c>[McpServerTool]</c>
/// / <c>[McpServerResource]</c> wrapper methods that delegate to the controller's service.
///
/// Discovery:
///   - Walks every <see cref="INamedTypeSymbol"/> in the compilation that derives (directly or
///     transitively) from <c>ControllerBase</c>.
///   - For each, collects action methods marked with <c>[ExposeAsMcpTool]</c> / <c>[ExposeAsMcpResource]</c>
///     — including methods inherited from a generic base controller (e.g. <c>GenericCrudController</c>).
///   - The derived controller's <c>[McpEntity]</c> attribute supplies the entity name for template
///     substitution in inherited XML doc summaries.
///
/// Phase 1 (this file): GenericCrudController-derived controllers — emits tool wrappers for the five
/// inherited CRUD actions (List, Get, Create, Update, Delete) annotated on the base. Custom action
/// methods on the derived class are also picked up if annotated. Resources, non-generic controllers,
/// and the URI-template translation rule arrive in later phases.
/// </summary>
[Generator]
public sealed class McpSurfaceGenerator : IIncrementalGenerator
{
    private const string ExposeToolAttribute = "SnapCd.Contracts.Mcp.ExposeAsMcpToolAttribute";
    private const string ExposeResourceAttribute = "SnapCd.Contracts.Mcp.ExposeAsMcpResourceAttribute";
    private const string EntityAttribute = "SnapCd.Contracts.Mcp.McpEntityAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find every type derived (transitively) from ControllerBase. We can't filter via
        // ForAttributeWithMetadataName because the annotation may live on a base-class method,
        // not on the derived controller itself.
        var controllers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cd && cd.BaseList is not null,
                transform: static (ctx, _) =>
                {
                    var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
                    if (symbol is null) return null;
                    if (!InheritsFromControllerBase(symbol)) return null;
                    return symbol;
                })
            .Where(static s => s is not null)
            .Select(static (s, _) => s!);

        context.RegisterSourceOutput(controllers.Collect(), (spc, controllerSymbols) =>
        {
            foreach (var controller in controllerSymbols.Distinct(SymbolEqualityComparer.Default).OfType<INamedTypeSymbol>())
            {
                // Only emit wrappers for concrete, non-generic controllers. Generic-arity bases
                // (e.g. GenericCrudController<TEntity, ...>) declare the annotated actions; the
                // wrappers are bound against each derived concrete type instead.
                if (controller.IsAbstract) continue;
                if (controller.IsGenericType) continue;

                // Require [McpEntity] as the explicit opt-in. Without it, a derived controller that
                // inherits annotated actions from a generic base would still be auto-exposed —
                // bypassing the safety of opt-in.
                var entity = ResolveEntity(controller);
                if (entity is null) continue;

                var actions = CollectAnnotatedActions(controller).ToList();
                if (actions.Count == 0) continue;
                var source = EmitSurface(controller, actions, entity);
                spc.AddSource($"{controller.Name}McpSurface.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        });
    }

    private static bool InheritsFromControllerBase(INamedTypeSymbol type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.Name == "ControllerBase" && t.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Mvc")
                return true;
        }
        return false;
    }

    /// <summary>
    /// Walks the inheritance chain and collects every method annotated with
    /// <c>[ExposeAsMcpTool]</c> or <c>[ExposeAsMcpResource]</c>. Each result carries the original
    /// declaring type so we can read the XML doc from the base while emitting against the derived.
    /// </summary>
    private static IEnumerable<AnnotatedAction> CollectAnnotatedActions(INamedTypeSymbol controller)
    {
        var seen = new HashSet<string>();
        for (var t = controller; t is not null; t = t.BaseType)
        {
            foreach (var member in t.GetMembers().OfType<IMethodSymbol>())
            {
                if (member.MethodKind != MethodKind.Ordinary) continue;
                if (member.DeclaredAccessibility != Accessibility.Public) continue;
                if (!seen.Add(member.Name)) continue; // a derived override masks the base

                var toolAttr = member.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString() == ExposeToolAttribute);
                var resourceAttr = member.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString() == ExposeResourceAttribute);
                if (toolAttr is null && resourceAttr is null) continue;

                yield return new AnnotatedAction(member, toolAttr, resourceAttr);
            }
        }
    }

    private static McpEntityInfo? ResolveEntity(INamedTypeSymbol controller)
    {
        for (var t = controller; t is not null; t = t.BaseType)
        {
            var attr = t.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString() == EntityAttribute);
            if (attr is null) continue;
            var singular = attr.NamedArguments.FirstOrDefault(n => n.Key == "Singular").Value.Value as string;
            var plural = attr.NamedArguments.FirstOrDefault(n => n.Key == "Plural").Value.Value as string;
            if (singular is null || plural is null) continue;
            return new McpEntityInfo(singular, plural);
        }
        return null;
    }

    private static string EmitSurface(INamedTypeSymbol controller, List<AnnotatedAction> actions, McpEntityInfo? entity)
    {
        var ns = controller.ContainingNamespace.ToDisplayString();
        var className = $"{controller.Name}McpSurface";
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/> AUTO-GENERATED by SnapCd.Mcp.Generator — do not edit.");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.ComponentModel;");
        sb.AppendLine("using ModelContextProtocol.Server;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine($"[McpServerToolType]");
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");

        var serviceType = ResolveServiceType(controller);

        foreach (var action in actions.Where(a => a.ToolAttribute is not null))
        {
            EmitToolWrapper(sb, controller, action, serviceType, entity);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Best-effort discovery of the service the controller delegates to. For
    /// <c>GenericCrudController</c>-derived controllers the service is the 7th type argument
    /// (<c>TService</c>). Returns null when no obvious service is found — the caller falls back
    /// to a placeholder that the developer is expected to fill in.
    /// </summary>
    private static ITypeSymbol? ResolveServiceType(INamedTypeSymbol controller)
    {
        for (var t = controller; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.OriginalDefinition.Name == "GenericCrudController")
            {
                var args = t.TypeArguments;
                if (args.Length >= 7) return args[6]; // TService is the 7th type parameter
            }
        }
        return null;
    }

    private static void EmitToolWrapper(
        StringBuilder sb,
        INamedTypeSymbol controller,
        AnnotatedAction action,
        ITypeSymbol? serviceType,
        McpEntityInfo? entity)
    {
        var method = action.Method;
        var toolName = ResolveToolName(controller, method, action.ToolAttribute, entity);
        var description = ResolveDescription(method, action.ToolAttribute, entity);
        var returnTypeText = EmitReturnType(method.ReturnType);

        sb.AppendLine($"    [McpServerTool(Name = \"{toolName}\")]");
        sb.AppendLine($"    [Description(@\"{EscapeForVerbatim(description)}\")]");

        var serviceParam = serviceType is not null
            ? $"{serviceType.ToDisplayString()} service"
            : "/* service-not-resolved */ object service";

        var actionParams = method.Parameters
            .Select(p => $"[Description(@\"{EscapeForVerbatim(ApplyEntityTemplate(ResolveParamDoc(method, p.Name), entity))}\")] {p.Type.ToDisplayString()} {p.Name}")
            .ToList();

        var allParams = new List<string> { serviceParam };
        allParams.AddRange(actionParams);

        var paramList = string.Join(",\n        ", allParams);
        var argList = string.Join(", ", method.Parameters.Select(p => p.Name));

        // Parse the controller method body to find the actual `Service.X(args)` call. Controller
        // and service signatures don't always agree on argument order — e.g. controller's
        // `Update(orgId, dto, id)` calls `Service.Update(dto, id, orgId)`. We mirror the literal
        // call from the controller body rather than passing controller args through verbatim.
        var serviceCall = ExtractServiceCall(method) ?? $"{method.Name}({argList})";

        sb.AppendLine($"    public static {returnTypeText} {ToPascalCase(toolName)}(");
        sb.AppendLine($"        {paramList})");
        sb.AppendLine($"        => service.{serviceCall};");
        sb.AppendLine();
    }

    /// <summary>
    /// Walks the controller method's syntax body to find the literal `Service.Foo(arg1, arg2, ...)`
    /// or `await Service.Foo(...)` call, returning <c>Foo(arg1, arg2)</c> as a string. Returns null
    /// if no `Service.{Method}` invocation is found (fallback: assume same arg order as the controller).
    /// </summary>
    private static string? ExtractServiceCall(IMethodSymbol method)
    {
        foreach (var declRef in method.DeclaringSyntaxReferences)
        {
            if (declRef.GetSyntax() is not MethodDeclarationSyntax mds) continue;
            foreach (var invocation in mds.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is MemberAccessExpressionSyntax mae
                    && mae.Expression is IdentifierNameSyntax id
                    && id.Identifier.Text == "Service")
                {
                    var calledName = mae.Name.Identifier.Text;
                    var args = invocation.ArgumentList.Arguments
                        .Select(a => a.ToFullString().Trim());
                    return $"{calledName}({string.Join(", ", args)})";
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Turns the controller action's return type into the MCP wrapper's return type string.
    /// Patterns:
    ///   <c>Task&lt;ActionResult&lt;T&gt;&gt;</c> → <c>Task&lt;T&gt;</c>
    ///   <c>Task&lt;IActionResult&gt;</c>          → <c>Task</c>   (best-effort; agent gets no payload)
    ///   <c>Task&lt;T&gt;</c>                       → <c>Task&lt;T&gt;</c>  (unchanged)
    ///   <c>ActionResult&lt;T&gt;</c>               → <c>Task&lt;T&gt;</c>
    ///   <c>T</c>                                   → <c>Task&lt;T&gt;</c>  (wrap)
    /// </summary>
    private static string EmitReturnType(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol named)
        {
            if (named.IsGenericType && named.Name == "Task" && named.TypeArguments.Length == 1)
            {
                var inner = named.TypeArguments[0];
                if (inner is INamedTypeSymbol innerNamed)
                {
                    if (innerNamed.IsGenericType && innerNamed.Name == "ActionResult" && innerNamed.TypeArguments.Length == 1)
                        return $"System.Threading.Tasks.Task<{innerNamed.TypeArguments[0].ToDisplayString()}>";
                    if (innerNamed.Name == "IActionResult")
                        return "System.Threading.Tasks.Task";
                }
                return $"System.Threading.Tasks.Task<{inner.ToDisplayString()}>";
            }
            if (named.IsGenericType && named.Name == "ActionResult" && named.TypeArguments.Length == 1)
                return $"System.Threading.Tasks.Task<{named.TypeArguments[0].ToDisplayString()}>";
            if (named.Name == "IActionResult")
                return "System.Threading.Tasks.Task";
        }
        return $"System.Threading.Tasks.Task<{returnType.ToDisplayString()}>";
    }

    private static string ResolveToolName(INamedTypeSymbol controller, IMethodSymbol method, AttributeData? attr, McpEntityInfo? entity)
    {
        if (attr is not null)
        {
            var explicitName = attr.NamedArguments.FirstOrDefault(n => n.Key == "Name").Value.Value as string;
            if (!string.IsNullOrWhiteSpace(explicitName)) return explicitName!;
        }

        // Default convention: {Entity.Plural ?? controller-name-without-Controller}_{methodName} in snake_case
        var noun = entity?.Plural ?? StripControllerSuffix(controller.Name);
        return $"{ToSnakeCase(noun)}_{ToSnakeCase(method.Name)}";
    }

    private static string ResolveDescription(IMethodSymbol method, AttributeData? attr, McpEntityInfo? entity)
    {
        var summary = ApplyEntityTemplate(ExtractSummary(method.GetDocumentationCommentXml() ?? string.Empty), entity);

        string? agentNote = null;
        if (attr is not null)
        {
            agentNote = attr.NamedArguments.FirstOrDefault(n => n.Key == "AgentNote").Value.Value as string;
        }

        return string.IsNullOrWhiteSpace(agentNote)
            ? summary
            : $"{summary}\n\n{agentNote}";
    }

    private static string ApplyEntityTemplate(string text, McpEntityInfo? entity)
    {
        if (entity is null) return text;
        return text
            .Replace("{Entity}", entity.Singular)
            .Replace("{entities}", entity.Plural.ToLowerInvariant());
    }

    private static string ResolveParamDoc(IMethodSymbol method, string paramName)
    {
        var xml = method.GetDocumentationCommentXml() ?? string.Empty;
        var marker = $"<param name=\"{paramName}\">";
        var i = xml.IndexOf(marker, System.StringComparison.Ordinal);
        if (i < 0) return paramName;
        i += marker.Length;
        var end = xml.IndexOf("</param>", i, System.StringComparison.Ordinal);
        if (end < 0) return paramName;
        return xml.Substring(i, end - i).Trim();
    }

    /// <summary>
    /// Extracts the <c>&lt;summary&gt;</c> body from a raw XML doc comment string.
    /// </summary>
    private static string ExtractSummary(string docXml)
    {
        const string start = "<summary>";
        const string end = "</summary>";
        var i = docXml.IndexOf(start, System.StringComparison.Ordinal);
        if (i < 0) return string.Empty;
        i += start.Length;
        var j = docXml.IndexOf(end, i, System.StringComparison.Ordinal);
        if (j < 0) return string.Empty;
        return Whitespace(docXml.Substring(i, j - i));
    }

    private static string Whitespace(string s) =>
        string.Join(" ", s.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim()))
            .Trim();

    private static string StripControllerSuffix(string name) =>
        name.EndsWith("Controller", System.StringComparison.Ordinal)
            ? name.Substring(0, name.Length - "Controller".Length)
            : name;

    private static string ToSnakeCase(string s)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (i > 0 && char.IsUpper(c)) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static string ToPascalCase(string snakeCase)
    {
        var parts = snakeCase.Split('_');
        var sb = new StringBuilder();
        foreach (var p in parts)
        {
            if (p.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(p[0]));
            if (p.Length > 1) sb.Append(p.Substring(1));
        }
        return sb.ToString();
    }

    private static string EscapeForVerbatim(string s) => s.Replace("\"", "\"\"");

    private sealed record AnnotatedAction(IMethodSymbol Method, AttributeData? ToolAttribute, AttributeData? ResourceAttribute);
    private sealed record McpEntityInfo(string Singular, string Plural);
}
