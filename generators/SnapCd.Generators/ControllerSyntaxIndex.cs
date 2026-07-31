// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SnapCd.Generators;

/// <summary>
/// Syntax-only index over the controller source files, used by <see cref="McpSurfaceEmitter"/> to mine method
/// bodies for the literal <c>Service.Foo(args)</c> invocation — the one piece of information reflection cannot
/// provide (controller and service signatures don't always agree on argument order). Parsing is raw
/// <see cref="CSharpSyntaxTree"/> work: no compilation, no semantic model, milliseconds for the whole folder.
/// </summary>
public sealed class ControllerSyntaxIndex
{
    private readonly Dictionary<string, List<MethodDeclarationSyntax>> _methodsByClassAndName;

    private ControllerSyntaxIndex(Dictionary<string, List<MethodDeclarationSyntax>> methodsByClassAndName)
    {
        _methodsByClassAndName = methodsByClassAndName;
    }

    public static ControllerSyntaxIndex Build(string controllersDirectory)
    {
        var methods = new Dictionary<string, List<MethodDeclarationSyntax>>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(controllersDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
            foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = classDeclaration.Identifier.Text;
                foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
                {
                    var key = Key(className, method.Identifier.Text);
                    if (!methods.TryGetValue(key, out var list))
                        methods[key] = list = new List<MethodDeclarationSyntax>();
                    list.Add(method);
                }
            }
        }

        return new ControllerSyntaxIndex(methods);
    }

    /// <summary>
    /// Finds the literal <c>Service.Foo(arg1, arg2, ...)</c> invocation inside the named method's body and
    /// returns it as <c>Foo(arg1, arg2)</c>; null when the class/method has no source here or the body contains
    /// no <c>Service.*</c> call. Overloads are disambiguated by parameter count.
    /// </summary>
    public string? TryExtractServiceCall(string className, string methodName, int parameterCount)
    {
        if (!_methodsByClassAndName.TryGetValue(Key(className, methodName), out var candidates))
            return null;

        foreach (var method in candidates.Where(m => m.ParameterList.Parameters.Count == parameterCount))
        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Expression is IdentifierNameSyntax identifier
                && identifier.Identifier.Text == "Service")
            {
                var calledName = memberAccess.Name.Identifier.Text;
                var args = invocation.ArgumentList.Arguments
                    .Select(a => a.ToFullString().Trim());
                return $"{calledName}({string.Join(", ", args)})";
            }
        }

        return null;
    }

    private static string Key(string className, string methodName) => $"{className}::{methodName}";
}
